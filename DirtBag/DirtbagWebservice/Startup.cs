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

namespace DirtBagWebservice
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            SentinelConnectionString = Configuration.GetConnectionString( "Sentinel" );
            DirtbagConnectionString = Configuration.GetConnectionString( "Dirtbag" );
        }

        public IConfigurationRoot Configuration { get; }
        public string SentinelConnectionString { get; }
        public string DirtbagConnectionString { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
            services.AddSingleton<IConfigurationRoot>( Configuration );
            services.AddTransient<DAL.IUserPostingHistoryDAL>( (x)=> { return new DAL.UserPostingHistoryDAL( new NpgsqlConnection( SentinelConnectionString ) ); } );
            services.AddTransient<DAL.IProcessedItemDAL>( ( x ) => { return new DAL.ProcessedItemSQLDAL( new SqlConnection( DirtbagConnectionString ) ); } );
            //new DAL.SubredditSettingsWikiDAL()
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();
        }
    }
}
