using System.Globalization;
using GB28181.Cache;
using GB28181.Config;
using GB28181.Logger4Net;
using GB28181.Server.Main;
using GB28181.Server.Message;
using GB28181.Server.Settings;
using GB28181.Servers;
using GB28181.Servers.SIPMessage;
using GB28181.Servers.SIPMonitor;
using GB28181.Service.Protos.DeviceCatalog;
using GB28181.Service.Protos.DeviceFeature;
using GB28181.Service.Protos.Ptz;
using GB28181.Service.Protos.Video;
using GB28181.Service.Protos.VideoRecord;
using GB28181.Sys.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace GB28181.Service
{
    public class Startup
    {

        public IConfiguration Configuration { get; }
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Configuration);
            services.Configure<DbConfig>(Configuration.GetSection("DbConfig"));
            services.AddSingleton<IMainProcess, MainProcess>();
            services.AddHostedService<GBService>();
            services.AddGrpc();

            //添加grpchttpapi的引用
            services.AddGrpcHttpApi();


            services.AddSingleton<ILog, Logger>()
                            .AddSingleton<ISipStorage, SipStorage>()
                            .AddSingleton<MediaEventSource>()
                            .AddSingleton<MessageHub>()
                            .AddSingleton<CatalogEventsProc>()
                            .AddSingleton<AlarmEventsProc>()
                            .AddSingleton<DeviceEventsProc>()
                            .AddScoped<ISIPServiceDirector, SIPServiceDirector>()
                            .AddTransient<ISIPMonitorCore, SIPMonitorCore>()
                            .AddSingleton<ISipMessageCore, SIPMessageCore>()
                            .AddSingleton<ISIPTransport, SIPTransport>()
                            .AddTransient<ISIPTransactionEngine, SIPTransactionEngine>()
                            .AddSingleton<ISIPRegistrarCore, SIPRegistrarCore>()
                            .AddSingleton<IMemoCache<Camera>, DeviceObjectCache>()
                            .AddScoped<VideoSession.VideoSessionBase, SSMediaSessionImpl>()
                            .AddScoped<PtzControl.PtzControlBase, PtzControlImpl>()
                            .AddScoped<DeviceCatalog.DeviceCatalogBase, DeviceCatalogImpl>()
                            .AddScoped<DeviceFeature.DeviceFeatureBase, DeviceFeatureImpl>()
                            .AddScoped<VideoOnDemand.VideoOnDemandBase, VideoOnDemandImpl>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "gRPC HTTP API Example", Version = "v1" });
            });
            services.AddGrpcSwagger();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("zh-CN"),
                // Formatting numbers, dates, etc.
                SupportedCultures = new[] { new CultureInfo("en-US"), new CultureInfo("zh-CN") }
            });
            // app.UseHttpsRedirection();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "gRPC HTTP API Example V1");
                options.DisplayOperationId();
                options.DisplayRequestDuration();
                options.DocExpansion(DocExpansion.Full);
                options.EnableDeepLinking();
                options.EnableFilter();
                options.ShowExtensions();
                options.EnableValidator();
            });

            app.UseRouting();

            // app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<PtzControlImpl>();
                endpoints.MapGrpcService<DeviceFeatureImpl>();
                endpoints.MapGrpcService<DeviceCatalogImpl>();
                endpoints.MapGrpcService<VideoOnDemandImpl>();
                endpoints.MapGrpcService<SSMediaSessionImpl>();
                //endpoints.MapGrpcService<DeviceManageImpl>(); // 没有加载
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
