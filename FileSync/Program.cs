using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FileSync.Comparers;
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
            var log = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File("logs/logs.txt")
                .WriteTo.Console(LogEventLevel.Information)
                .CreateLogger();

            var appConfig = new AppConfig().Initialize();

            var builder = new ContainerBuilder();
            builder.RegisterInstance(log).As<ILogger>();
            builder.RegisterInstance(appConfig).As<AppConfig>();
            builder.RegisterType<FileFilter>().As<IFileFilter>();
            builder.RegisterType<DirectoryStructureComparer>().As<IDirectoryStructureComparer>();
            builder.RegisterType<DeepDeepFileComparer>().As<IDeepFileComparer>();
            builder.RegisterType<ShallowFileComparer>().As<IShallowFileComparer>();
            builder.RegisterType<FileSynchronizer>();
            Container = builder.Build();

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
    }
}