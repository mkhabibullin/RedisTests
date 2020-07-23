using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RedisFTS.Core;
using System;

namespace RedisFTS
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = CreateHostBuilder(args)
                .Build();

            host.RunAsync().Wait();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.Sources.Clear();

                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json",
                                     optional: true, reloadOnChange: true);

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging();
                //services.Configure<Application>(hostContext.Configuration.GetSection("application"));
                //services.AddHostedService<FizzBuzzHostedService>();
                services.AddSingleton<RedisClient>();

                services.AddSingleton<IHostedService, App>();

            })
            .ConfigureLogging((hostContext, configLogging) =>
            {
                //configLogging.AddConsole();

            })
            .UseConsoleLifetime();
    }
}
