using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
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

            //如果需要在服务启动前做一些事情,参考如下代码
            //下列代码用于在服务启动前，先进行数据库迁移工作
            //using (var scope = host.Services.CreateScope())
            //{
            //    var myDbContext = scope.ServiceProvider.GetRequiredService<YourDbContext>();
            //    await myDbContext.Database.MigrateAsync();
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
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
