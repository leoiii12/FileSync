using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FileSync.Comparers;
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
            Initialize();

            Console.CancelKeyPress += (sender, eArgs) =>
            {
                eArgs.Cancel = true;
                QuitEvent.Set();
            };

            Task.Run(() =>
            {
                using (var scope = Container.BeginLifetimeScope())
                {
                    var fileSynchronizer = scope.Resolve<FileSynchronizer>();

                    fileSynchronizer.Sync();
                    fileSynchronizer.WatchAndSync();
                }
            });

            QuitEvent.WaitOne();
        }

        private static void Initialize()
        {
            var appConfig = new AppConfig().Initialize();

            var log = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File("logs/logs.txt")
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();

            var builder = new ContainerBuilder();

            builder.RegisterInstance(appConfig).As<AppConfig>();

            builder.RegisterInstance(log).As<ILogger>();

            builder.RegisterType<FileFilter>().As<IFileFilter>();

            builder.RegisterType<DirectoryStructureComparer>().As<IDirectoryStructureComparer>();

            if (appConfig.UseDeepFileComparer)
                builder.RegisterType<DeepFileComparer>().As<IFileComparer>();
            else
                builder.RegisterType<ShallowFileComparer>().As<IFileComparer>();

            builder.RegisterType<SimpleFileCopier>().As<IFileCopier>();

            builder.RegisterType<SimpleFileDeleter>().As<IFileDeleter>();

            builder.RegisterType<SimpleFileMerger>().As<IFileMerger>();

            builder.RegisterType<FileSynchronizer>();

            Container = builder.Build();
        }
    }
}