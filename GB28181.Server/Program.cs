using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GB28181.Service
{
    public class Program
    {
        public static async Task Main(string[] args)
        {

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            //CreateHostBuilder(args).Build().Run();

            var host = CreateHostBuilder(args).Build();

            //pre-working before app starting
            //using (var scope = host.Services.CreateScope())
            //{
            //    var builder = new ConfigurationBuilder();
            //    builder.SetBasePath(Directory.GetCurrentDirectory());
            //    builder.AddXmlFile("Config/gb28181.xml", false, reloadOnChange: true);
            //    var config = builder.Build();      
            //    Console.WriteLine(config["sipaccount:ID"]);
            //    var sect = config.GetSection("sipaccounts");

            //    // var myDbContext = scope.ServiceProvider.GetRequiredService<YourDbContext>();
            //    //await myDbContext.Database.MigrateAsync();
            //}

            await host.RunAsync();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject != null)
            {
                if (e.ExceptionObject is Exception exceptionObj)
                {
                    throw exceptionObj;
                }
            }
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                 .ConfigureLogging(logging =>
                 {
                     logging.ClearProviders();
                     logging.AddConsole();
                     // logging.SetMinimumLevel(LogLevel.Trace);  //configration used
                 })
                .ConfigureHostConfiguration(config =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    //config.AddJsonFile($"appsettings.json", true, reloadOnChange: true);
                    config.AddXmlFile("Config/gb28181.xml", false, reloadOnChange: true);
                })
                .ConfigureAppConfiguration(config =>
               {
                   config.AddEnvironmentVariables();
                   if (args != null)
                   {
                       config.AddCommandLine(args);
                   }
               })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
