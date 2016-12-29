using Microsoft.Practices.Unity;
using RedditSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Helpers {
    public static class UnityHelpers {
        #region Unity Container
        private static Lazy<IUnityContainer> container = new Lazy<IUnityContainer>( () => {
            var container = new UnityContainer();
            RegisterTypes( container );
            return container;
        } );

        public static IUnityContainer GetConfiguredContainer() {
            return container.Value;
        }
        #endregion

        public static void RegisterTypes( IUnityContainer container ) {
            string sqlConnString = ConfigurationManager.AppSettings["SQLConnString"];
            string uname = ConfigurationManager.AppSettings["BotUsername"];
            string pass = ConfigurationManager.AppSettings["BotPassword"];
            string clientId = ConfigurationManager.AppSettings["ClientID"];
            string clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
            string redirectUri = ConfigurationManager.AppSettings["RedirectURI"];
            if ( string.IsNullOrEmpty( uname ) ) throw new Exception( "Missing 'BotUsername' in config" );
            if ( string.IsNullOrEmpty( pass ) ) throw new Exception( "Missing 'BotPassword' in config" );
            if ( string.IsNullOrEmpty( clientId ) ) throw new Exception( "Missing 'ClientID' in config" );
            if ( string.IsNullOrEmpty( clientSecret ) ) throw new Exception( "Missing 'ClientSecret' in config" );
            if ( string.IsNullOrEmpty( redirectUri ) ) throw new Exception( "Missing 'RedirectURI' in config" );
            var agent = new BotWebAgent( uname, pass, clientId, clientSecret, redirectUri );
            var client = new Reddit( agent, false );
            container.RegisterType<IDbConnection, SqlConnection>( new InjectionConstructor( sqlConnString ) );
            container.RegisterType<DAL.IProcessedItemDAL, DAL.ProcessedItemSQLDAL>();
            container.RegisterType<DAL.ISubredditSettingsDAL, DAL.SubredditSettingsWikiDAL>( new InjectionConstructor( client ) );

        }
    }
}
