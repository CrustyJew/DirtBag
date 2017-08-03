using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.SqlClient;
using EasyNetQ;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using NLog.Extensions.Logging;
using NLog.Web;
using Dirtbag.DAL;
using Dirtbag.BLL;

namespace DirtbagWebservice {
    public class Startup {
        public static IAdvancedBus rabbit;
        private static RabbitListener rabbitListener;
        private static IEasyNetQLogger logger;
        public Startup( IHostingEnvironment env ) {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if(env.IsDevelopment()) {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
                //logger = new EasyNetQ.Loggers.ConsoleLogger();
            }
            Configuration = builder.Build();


            SentinelConnectionString = Configuration.GetConnectionString("SentinelDirtbag");
            DirtbagConnectionString = Configuration.GetConnectionString("Dirtbag");



        }

        public IConfigurationRoot Configuration { get; }
        public string SentinelConnectionString { get; }
        public string DirtbagConnectionString { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices( IServiceCollection services ) {
            // Add framework services.
            services.AddMemoryCache();
            services.AddSingleton<IConfigurationRoot>(Configuration);
            services.AddTransient<IUserPostingHistoryDAL>(( x ) => { return new UserPostingHistoryDAL(new NpgsqlConnection(SentinelConnectionString)); });
            services.AddTransient<IProcessedItemDAL>(( x ) => { return new ProcessedItemSQLDAL(new SqlConnection(DirtbagConnectionString)); });
            services.AddTransient<ISubredditSettingsDAL>(( x ) => { return new SubredditSettingsPostgresDAL(new NpgsqlConnection(SentinelConnectionString)); });
            services.AddTransient<ISentinelChannelBanDAL>(( x ) => { return new SentinelChannelBanDAL(new NpgsqlConnection(SentinelConnectionString)); });
            services.AddTransient<ISentinelChannelBanBLL, SentinelChannelBanBLL>();
            services.AddTransient<ISubredditSettingsBLL, SubredditSettingsBLL>();
            services.AddTransient<IAnalyzeMediaBLL, AnalyzeMediaBLL>();
            services.AddTransient<IProcessedItemBLL, ProcessedItemBLL>();

            services.AddSingleton(new RedditSharp.WebAgentPool<string, RedditSharp.BotWebAgent>());

            new DatabaseInitializationSQL(new SqlConnection(DirtbagConnectionString)).InitializeTablesAndData().Wait();


            services.AddMvc();

            services.AddAuthorization(options => {
                options.AddPolicy("DirtbagAdmin", policy => policy.Requirements.Add(new AdminAuthRequirement()));
            });

            services.AddSingleton<IAuthorizationHandler, AdminAuthHandler>();

            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new Info { Title = "Dirtbag", Version = "v1" });
                c.AddSecurityDefinition("oauth2", new OAuth2Scheme() {
                    Type = "oauth2",
                    Flow = "application",
                    TokenUrl = Configuration["OIDC_Authority"] + "/connect/token",
                    Scopes = new Dictionary<string, string> { { "dirtbag", "Dirtbag API" } }
                });
                c.OperationFilter<SecurityRequirementsOperationFilter>();
            }
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure( IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory ) {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            if(env.IsDevelopment()) {
                loggerFactory.AddDebug();
            }
            if(String.IsNullOrWhiteSpace(Configuration["DisableRabbitQueue"])) {
                rabbit = RabbitHutch.CreateBus(Configuration.GetConnectionString("Rabbit"),
                    x => {
                        x.Register<ISerializer, DirtbagRabbitSerializer>()
                         .Register<ITypeNameSerializer>(_ => new DirtbagTypeNameSerializer());
                        if(logger != null) x.Register<IEasyNetQLogger>(_ => logger);
                    }).Advanced;
                var exchange = rabbit.ExchangeDeclare(Configuration["RabbitExchange"], EasyNetQ.Topology.ExchangeType.Direct);
                var queue = rabbit.QueueDeclare(Configuration["RabbitQueue"]);
                rabbitListener = new DirtbagWebservice.RabbitListener(app.ApplicationServices, loggerFactory.CreateLogger<RabbitListener>());
                var binding = rabbit.Bind(exchange, queue, Configuration["RabbitRoutingKey"]);

                rabbit.Consume<Dirtbag.Models.RabbitAnalysisRequestMessage>(queue, rabbitListener.Subscribe, conf => { conf.WithPrefetchCount(25); });

            }

            app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions {
                Authority = Configuration["OIDC_Authority"],
                RequireHttpsMetadata = false,
                ApiName = "dirtbag"
            });

            app.UseMvc();

            app.UseSwagger(c => {
                c.RouteTemplate = "api-docs/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("../api-docs/v1/swagger.json", "Dirtbag API v1");
                //c.ConfigureOAuth2( "js", "", "swagger", "dirtbag" );
                //c.ConfigureOAuth2()

            });

            if(!env.IsDevelopment()) {

                loggerFactory.AddNLog();
                app.AddNLogWeb();

            }

        }
    }
}
