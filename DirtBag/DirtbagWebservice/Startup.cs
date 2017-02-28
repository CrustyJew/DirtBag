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

namespace DirtBagWebservice {
    public class Startup {
        public static IAdvancedBus rabbit;
        private static RabbitListener rabbitListener;
        public Startup( IHostingEnvironment env ) {
            var builder = new ConfigurationBuilder()
                .SetBasePath( env.ContentRootPath )
                .AddJsonFile( "appsettings.json", optional: true, reloadOnChange: true )
                .AddJsonFile( $"appsettings.{env.EnvironmentName}.json", optional: true )
                .AddEnvironmentVariables();

            if ( env.IsDevelopment() ) {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }
            Configuration = builder.Build();


            SentinelConnectionString = Configuration.GetConnectionString( "Sentinel" );
            DirtbagConnectionString = Configuration.GetConnectionString( "Dirtbag" );



        }

        public IConfigurationRoot Configuration { get; }
        public string SentinelConnectionString { get; }
        public string DirtbagConnectionString { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices( IServiceCollection services ) {
            // Add framework services.

            services.AddSingleton<IConfigurationRoot>( Configuration );
            services.AddTransient<DAL.IUserPostingHistoryDAL>( ( x ) => { return new DAL.UserPostingHistoryDAL( new NpgsqlConnection( SentinelConnectionString ) ); } );
            services.AddTransient<DAL.IProcessedItemDAL>( ( x ) => { return new DAL.ProcessedItemSQLDAL( new SqlConnection( DirtbagConnectionString ) ); } );
            services.AddTransient<DAL.ISubredditSettingsDAL>( ( x ) => { return new DAL.SubredditSettingsPostgresDAL( new NpgsqlConnection( SentinelConnectionString ) ); } );
            services.AddTransient<BLL.ISubredditSettingsBLL, BLL.SubredditSettingsBLL>();
            services.AddTransient<BLL.IAnalyzePostBLL, BLL.AnalyzePostBLL>();

            services.AddMvc();

            var logger = new EasyNetQ.Loggers.ConsoleLogger(); 
            rabbit = RabbitHutch.CreateBus( Configuration.GetConnectionString( "Rabbit" ), x => x.Register< ISerializer, DirtbagRabbitSerializer>().Register<ITypeNameSerializer>(_ => new DirtbagTypeNameSerializer()).Register<IEasyNetQLogger>( _ => logger ) ).Advanced;

            var exchange = rabbit.ExchangeDeclare( Configuration["RabbitExchange"], EasyNetQ.Topology.ExchangeType.Direct);
            var queue = rabbit.QueueDeclare( Configuration["RabbitQueue"] );
            rabbitListener = new DirtBagWebservice.RabbitListener( services.BuildServiceProvider(), rabbit, exchange, Configuration["RabbitResultRoutingKey"], Boolean.Parse( Configuration["RabbitReturnItemsWithActionsOnly"] ) );
            rabbit.Bind( exchange, queue, Configuration["RabbitRoutingKey"] );
            rabbit.Consume<Models.RabbitAnalysisRequestMessage>( queue, rabbitListener.Subscribe );
            
            services.AddSwaggerGen( c => {
                c.SwaggerDoc("v1", new Info { Title = "Dirtbag", Version = "v1" } );
                c.AddSecurityDefinition( "oauth2", new OAuth2Scheme() {
                     Type = "oauth2", 
                     Flow= "implicit",
                     AuthorizationUrl = Configuration["OIDC_Authority"] + "/connect/authorize", 
                     Scopes = new Dictionary<string, string> { { "dirtbag","Dirtbag API"} }, 
                } );
                c.OperationFilter<SecurityRequirementsOperationFilter>();
            }
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure( IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory ) {
            loggerFactory.AddConsole( Configuration.GetSection( "Logging" ) );
            loggerFactory.AddDebug();
            app.UseIdentityServerAuthentication( new IdentityServerAuthenticationOptions {
                Authority = Configuration["OIDC_Authority"],
                RequireHttpsMetadata = !env.IsDevelopment(),
                ApiName = "dirtbag"
            } );

            app.UseMvc();

            app.UseSwagger( c =>
            {
                c.RouteTemplate = "api-docs/{documentName}/swagger.json";
            } );

            app.UseSwaggerUI(c=> {
                c.SwaggerEndpoint( "/api-docs/v1/swagger.json", "Dirtbag API v1" );
                c.ConfigureOAuth2( "js","", "swagger", "dirtbag" );
            } );

        }
    }
}
