using System;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using Dapper;

namespace DirtBag.Logging {
    class DirtBagConnection {
        public bool UseLocalDB { get; set; }
        public string SQLConnString { get; set; }
        public string LocalDBFile { get; set; }

        private DbConnection dbConn;
        public void InitializeConnection( string[] subreddits ) {
            UseLocalDB = bool.Parse( ConfigurationManager.AppSettings["UseLocalDB"] );
            if ( UseLocalDB ) {
                LocalDBFile = ConfigurationManager.AppSettings["LocalDBFile"];
                if ( string.IsNullOrEmpty( LocalDBFile ) ) {
                    throw new Exception( "UseLocalDB set to true but no LocalDBFile path specified." );
                }
                if ( !File.Exists( LocalDBFile ) ) {
                    SQLiteConnection.CreateFile( LocalDBFile );
                }
            }
            else {
                SQLConnString = ConfigurationManager.AppSettings["SQLConnString"];
                if ( string.IsNullOrEmpty( SQLConnString ) ) {
                    throw new Exception( "UseLocalDB set to false but no SQLConnString specified" );
                }
            }

            using ( var con = GetConn() ) {
                var initTables = "" +
                    "CREATE TABLE IF NOT EXISTS [SubReddits]( " +
                    "[ID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                    "[SubName] varchar(50) NOT NULL); " +
                    "" +
                    "CREATE TABLE IF NOT EXISTS [Actions]( " +
                    "[ID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                    "[ActionName] varchar(50) NOT NULL); " +
                    "" +
                    "CREATE TABLE IF NOT EXISTS [ProcessedPosts]( " +
                    "[ID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                    "[SubredditID] INTEGER NOT NULL, " +
                    "[PostID] varchar(20) NOT NULL, " +
                    "[ActionID] INTEGER ); " +
                    "";
                con.Execute( initTables );

                var subs = "('" + string.Join( "'),('", subreddits ) + "') ";
                var seedData = "" +
                    "INSERT INTO SubReddits (SubName) " +
                    "values " + subs +
                    "EXCEPT " +
                    "SELECT SubName from SubReddits" +
                    ";" +
                    "INSERT INTO Actions (ActionName) " +
                    "VALUES ('Report'),('Remove') " +
                    "EXCEPT " +
                    "SELECT ActionName from Actions" +
                    ";";

                con.Execute( seedData );

            }
        }

        public static DbConnection GetConn() {
            var useLocalDB = bool.Parse( ConfigurationManager.AppSettings["UseLocalDB"] );
            var localDBFile = "";
            var sqlConnString = "";
            if ( useLocalDB ) {
                localDBFile = ConfigurationManager.AppSettings["LocalDBFile"];
                if ( string.IsNullOrEmpty( localDBFile ) ) {
                    throw new Exception( "UseLocalDB set to true but no LocalDBFile path specified." );
                }
                if ( !File.Exists( localDBFile ) ) {
                    SQLiteConnection.CreateFile( localDBFile );
                }
                var sb = new SQLiteConnectionStringBuilder();
                sb.DataSource = localDBFile;
                sb.DateTimeKind = DateTimeKind.Utc; //currently doesn't do anything as there is a bug in Dapper
                return new SQLiteConnection( sb.ToString());
            }
            sqlConnString = ConfigurationManager.AppSettings["SQLConnString"];
            if ( string.IsNullOrEmpty( sqlConnString ) ) {
                throw new Exception( "UseLocalDB set to false but no SQLConnString specified" );
            }
            return new SqlConnection( sqlConnString );
        }
    }
}
