﻿using CustomConsoleLogger;
using FibraClickSocial.Configuration;
using FibraClickSocial.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FibraClickSocial
{
    class Program
    {
        /// <summary>
        /// Holds the <see cref="CancellationToken"/> used
        /// for shutting down the WebHost
        /// </summary>
        private static CancellationTokenSource shutdownTokenSource =
            new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            IHost host = new HostBuilder()
                    .ConfigureAppConfiguration(ConfigureApp)
                    .ConfigureServices(ConfigureServices)
                    .ConfigureLogging(ConfigureLogging)
                    .UseConsoleLifetime()
                    .Build();

            // Get the logger
            ILogger logger = host.Services.GetRequiredService<ILogger<Program>>();

            try
            {
                await host.RunAsync(shutdownTokenSource.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception");

                // Stop the WebHost, otherwise it might hang here
                shutdownTokenSource.Cancel();

                // Required to stop all the threads.
                // With "return 1", the process could actually stay online forever
                Environment.Exit(1);
            }
        }

        private static void ConfigureApp(HostBuilderContext hostContext, IConfigurationBuilder configApp)
        {
            // Load the application settings
            configApp.SetBasePath(Directory.GetCurrentDirectory());
            configApp.AddJsonFile("appsettings.json");
        }

        private static void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder logging)
        {
            logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
            logging.AddCustomConsole();
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            // TODO CORE 3.0: Change after https://github.com/aspnet/Hosting/issues/1346 is released
            services.Configure<ConsoleLifetimeOptions>(console => console.SuppressStatusMessages = true);

            services.Configure<WholesaleConfiguration>(hostContext.Configuration.GetSection("Wholesale"));
            services.Configure<TelegramConfiguration>(hostContext.Configuration.GetSection("Telegram"));
            services.Configure<TwitterConfiguration>(hostContext.Configuration.GetSection("Twitter"));
            services.Configure<FacebookConfiguration>(hostContext.Configuration.GetSection("Facebook"));

            // This also registers the service as a transient service
            //services.AddHttpClient<IWholesaleService, WholesaleService>();

            services.AddHostedService<ConfigurationValidationHostedService>();
            services.AddHostedService<SchedulerHostedService>();
        }
    }
}