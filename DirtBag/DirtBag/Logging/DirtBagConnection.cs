using System;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using Dapper;

namespace DirtBag.Logging {
    class DirtBagConnection {

        public string SQLConnString { get; set; }
        public string LocalDBFile { get; set; }

        private DbConnection dbConn;
        public void InitializeConnection( string[] subreddits ) {

            SQLConnString = ConfigurationManager.AppSettings["SQLConnString"];
            if ( string.IsNullOrEmpty( SQLConnString ) ) {
                throw new Exception( "No SQLConnString specified!" );
            }


            using ( var con = GetConn() ) {
                var initTables = @"
if not exists( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where s.name = SCHEMA_NAME() and t.name = 'Subreddits' ) 
CREATE TABLE
[SubReddits]( 
    [ID] INTEGER NOT NULL PRIMARY KEY IDENTITY, 
    [SubName] varchar(50) NOT NULL
);

if not exists( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where s.name = SCHEMA_NAME() and t.name = 'Actions' ) 
CREATE TABLE
[Actions]( 
    [ID] INTEGER NOT NULL PRIMARY KEY IDENTITY,
    [ActionName] varchar(50) NOT NULL
);

if not exists( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where s.name = SCHEMA_NAME() and t.name = 'ProcessedPosts' )
CREATE TABLE
[ProcessedPosts]( 
    [ID] INTEGER NOT NULL PRIMARY KEY IDENTITY, 
    [SubredditID] INTEGER NOT NULL, 
    [PostID] varchar(20) NOT NULL, 
    [ActionID] INTEGER, 
    [SeenByModules] INTEGER, 
    [AnalysisResults] VARBINARY(2000) --varbinary uses less space than base64 encoding and storing as varchar
); 

if not exists( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where s.name = SCHEMA_NAME() and t.name = 'BannedEntities' ) 
CREATE TABLE
[BannedEntities]( 
    [ID] INTEGER NOT NULL PRIMARY KEY IDENTITY, 
    [SubredditID] INTEGER, 
    [EntityString] varchar(100) NOT NULL,
    [EntityType] tinyint NOT NULL,
    [BannedBy] varchar(50) NOT NULL,
    [BanReason] varchar(255),
    [BanDate] DATETIME,
    [ThingID] varchar(20) 
);

if not exists( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where s.name = SCHEMA_NAME() and t.name = 'BannedEntities_History' ) 
CREATE TABLE
[BannedEntities_History]( 
    [ID] INTEGER NOT NULL PRIMARY KEY IDENTITY, 
    [HistTimestamp] DATETIME NOT NULL,
    [HistAction] varchar(1) NOT NULL,
    [HistUser] varchar(50) NOT NULL,
    [SubredditID] INTEGER, 
    [EntityString] varchar(100) NOT NULL,
    [EntityType] tinyint NOT NULL,
    [BannedBy] varchar(50) NOT NULL,
    [BanReason] varchar(255),
    [BanDate] DATETIME,
    [ThingID] varchar(20) 
);

if not exists ( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where s.name = SCHEMA_NAME() and t.name = 'AnalysisScores' ) 
CREATE TABLE
[AnalysisScores](
    [SubredditID] INTEGER NOT NULL,
    [ModuleID] INTEGER NOT NULL,
    [PostID] varchar(20) NOT NULL,
    [Score] FLOAT,
    [Reason] varchar(1000),
    [ReportReason] varchar(255),
    [FlairText] varchar(255),
    [FlairClass] varchar(255),
    [FlairPriority] smallint
);
";

                con.Execute( initTables );

                var subs = "('" + string.Join( "'),('", subreddits ) + "') ";
                var seedData = @"
MERGE Subreddits WITH (HOLDLOCK) AS s 
Using (VALUES "  + subs + @") AS ns (SubName) 
ON s.SubName = ns.SubName 
WHEN NOT MATCHED BY TARGET THEN 
INSERT (SubName) VALUES (ns.SubName); 

MERGE Actions WITH (HOLDLOCK) AS v 
Using (VALUES ('Report'),('Remove'),('None')) AS nv (Action) 
ON v.ActionName = nv.Action 
WHEN NOT MATCHED BY TARGET THEN 
INSERT (ActionName) VALUES (nv.Action); ";
                
                con.Execute( seedData );

            }
        }

        public static DbConnection GetConn() {
            string sqlConnString = ConfigurationManager.AppSettings["SQLConnString"];
            if ( string.IsNullOrEmpty( sqlConnString ) ) {
                throw new Exception( "No SQLConnString specified!" );
            }
            return new SqlConnection( sqlConnString );
        }

        public static DbConnection GetSentinelConn() {
            string postgresSqlConn = ConfigurationManager.AppSettings["SentinelDBConnString"];
            if ( string.IsNullOrEmpty( postgresSqlConn ) ) {
                throw new Exception( "No postgresSqlConn specified!" );
            }
            return new Npgsql.NpgsqlConnection( postgresSqlConn );
        }
    }
}
