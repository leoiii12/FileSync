using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FileSync.Comparers;
using FileSync.FileWatchers;
using FileSync.Filters;
using FileSync.Operations;
using Serilog;
using Serilog.Events;

namespace FileSync
{
    internal class Program
    {
        private static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);

        public static IContainer Container { get; set; }

        private static void Main(string[] args)
        {
            var appConfig = new AppConfig().Initialize();
            
            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(appConfig.Log)
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();

            Initialize(appConfig, logger);

            Console.CancelKeyPress += (sender, eArgs) =>
            {
                eArgs.Cancel = true;
                QuitEvent.Set();
            };

            Task.Run(() =>
            {
                try
                {
                    FileSynchronizer fileSynchronizer;

                    using (var scope = Container.BeginLifetimeScope())
                    {
                        fileSynchronizer = scope.Resolve<FileSynchronizer>();
                    }

                    fileSynchronizer.Sync();
                    fileSynchronizer.WatchAndSync();
                }
                catch (Exception e)
                {
                    logger.Fatal(e.GetBaseException().ToString());
                    
                    Environment.Exit(-1);
                }
            });

            QuitEvent.WaitOne();
        }

        private static void Initialize(IAppConfig appConfig, ILogger logger)
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(appConfig).As<IAppConfig>();

            builder.RegisterInstance(logger).As<ILogger>();

            #region FileWatchers

            builder.RegisterType<FileWatcher>().As<IFileWatcher>();

            #endregion

            #region Filters

            builder.RegisterType<GitignoreParser>();

            builder.RegisterType<GitignoreFileFilter>().As<IFileFilter>();

            #endregion

            #region Comparers

            builder.RegisterType<DirectoryStructureComparer>().As<IDirectoryStructureComparer>();

            if (appConfig.UseDeepFileComparer)
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

            Container = builder.Build();
        }
    }
}