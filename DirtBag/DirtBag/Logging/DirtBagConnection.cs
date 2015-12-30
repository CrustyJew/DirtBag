using System;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using Dapper;

namespace DirtBag.Logging {
    class DirtBagConnection {
        public static bool UseLocalDB { get; set; }
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
                var initTables = "";
                if ( UseLocalDB ) initTables += "CREATE TABLE IF NOT EXISTS ";
                else initTables += "if not exists( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where s.name = SCHEMA_NAME() and t.name = 'Subreddits' ) Create table ";
                initTables += "" +
                    "[SubReddits]( [ID] INTEGER NOT NULL PRIMARY KEY " + ( UseLocalDB ? "AUTOINCREMENT" : "IDENTITY" ) + ", " +
                    "[SubName] varchar(50) NOT NULL); ";
                if ( UseLocalDB ) initTables += "CREATE TABLE IF NOT EXISTS ";
                else initTables += "if not exists( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where s.name = SCHEMA_NAME() and t.name = 'Actions' ) Create table ";
                initTables += "" +
                    "[Actions]( " +
                    "[ID] INTEGER NOT NULL PRIMARY KEY " + (UseLocalDB?"AUTOINCREMENT":"IDENTITY") +", " +
                    "[ActionName] varchar(50) NOT NULL); ";
                if ( UseLocalDB ) initTables += "CREATE TABLE IF NOT EXISTS ";
                else initTables += "if not exists( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where t.name = 'ProcessedPosts' ) Create table ";
                initTables += "" +
                    "[ProcessedPosts]( " +
                    "[ID] INTEGER NOT NULL PRIMARY KEY " + ( UseLocalDB ? "AUTOINCREMENT" : "IDENTITY" ) + ", " +
                    "[SubredditID] INTEGER NOT NULL, " +
                    "[PostID] varchar(20) NOT NULL, " +
                    "[ActionID] INTEGER, "+
                    "[AnalysisResults] VARBINARY(max) ); " + //varbinary uses less space than base64 encoding and storing as varchar
                    "";
                con.Execute( initTables );

                var subs = "('" + string.Join( "'),('", subreddits ) + "') ";
                var seedData = "";
                if ( UseLocalDB ) {
                    seedData +=
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
                }
                else {
                    seedData +=
                    "MERGE Subreddits WITH (HOLDLOCK) AS s " +
                    "Using (VALUES " + subs + ") AS ns (SubName) " +
                    "ON s.SubName = ns.SubName " +
                    "WHEN NOT MATCHED BY TARGET THEN " +
                    "INSERT (SubName) VALUES (ns.SubName); " +
                    "" +
                    "MERGE Actions WITH (HOLDLOCK) AS v " +
                    "Using (VALUES ('Report'),('Remove')) AS nv (Action) " +
                    "ON v.ActionName = nv.Action " +
                    "WHEN NOT MATCHED BY TARGET THEN " +
                    "INSERT (ActionName) VALUES (nv.Action); ";
                }
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
