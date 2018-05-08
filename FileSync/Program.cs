using System;
using System.Linq;
using System.Threading;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FileSync.Comparers;
using FileSync.FileWatchers;
using FileSync.Filters;
using FileSync.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace FileSync
{
    internal class Program
    {
        private static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);

        private static IAppConfig AppConfig { get; set; }

        private static void Main(string[] args)
        {
            #region Configure Services

            AppConfig = new AppConfig().Initialize();

            var serviceCollection = new ServiceCollection();
            var serviceProvider = ConfigureServices(serviceCollection);

            #endregion

            Run(serviceProvider);

            Console.CancelKeyPress += (sender, eArgs) =>
            {
                eArgs.Cancel = true;
                QuitEvent.Set();
            };

            QuitEvent.WaitOne();
        }

        private static void Run(IServiceProvider serviceProvider)
        {
            try
            {
                var fileSynchronizer = serviceProvider.GetService<FileSynchronizer>();

                fileSynchronizer.Sync();
                fileSynchronizer.WatchAndSync();
            }
            catch (Exception exception)
            {
                var logger = serviceProvider.GetService<ILogger<Program>>();

                switch (exception)
                {
                    case AggregateException e:
                        var exceptionGroups = e.InnerExceptions.GroupBy(ie => ie.Message).ToArray();

                        foreach (var group in exceptionGroups)
                        {
                            var message = group.Key;
                            var baseExceptions = string.Join(", ", group.Select(a => a.GetBaseException().ToString()));

                            logger.LogDebug(message);
                            logger.LogDebug(baseExceptions);
                        }

                        logger.LogCritical(string.Join(", ", exceptionGroups.Select(a => a.Key).ToArray()));
                        break;
                    case Exception e:
                        logger.LogCritical(e.GetBaseException().ToString());
                        break;
                }
            }
        }

        private static IServiceProvider ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(AppConfig.Log)
                .WriteTo.Console(LogEventLevel.Information, theme: AnsiConsoleTheme.Grayscale)
                .CreateLogger();

            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            var builder = new ContainerBuilder();

            builder.Populate(services);

            // builder.RegisterModule(new LogRequestsModule());

            #region AppConfig

            builder.RegisterInstance(AppConfig).As<IAppConfig>();

            #endregion

            #region FileWatchers

            builder.RegisterType<FileWatcher>().As<IFileWatcher>();

            #endregion

            #region Filters

            builder.RegisterType<GitignoreParser>();

            builder.RegisterType<GitignoreFileFilter>().As<IFileFilter>();

            #endregion

            #region Comparers

            builder.RegisterType<DirectoryStructureComparer>().As<IDirectoryStructureComparer>();

            if (AppConfig.UseDeepFileComparer)
                builder.RegisterType<DeepFileComparer>().As<IFileComparer>();
            else
                builder.RegisterType<ShallowFileComparer>().As<IFileComparer>();

            #endregion

            #region Operations

            builder.RegisterType<SimpleFileCopier>().As<IFileCopier>();

            builder.RegisterType<SimpleFileDeleter>().As<IFileDeleter>();

            builder.RegisterType<SimpleFileMerger>().As<IFileMerger>();

            #endregion

            builder.RegisterType<FileSynchronizer>();

            var container = builder.Build();

            // Create the IServiceProvider based on the container.
            return new AutofacServiceProvider(container);
        }
    }
}