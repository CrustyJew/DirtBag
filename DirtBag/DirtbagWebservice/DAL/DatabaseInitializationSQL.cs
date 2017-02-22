using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace DirtBagWebservice.DAL {
    public class DatabaseInitializationSQL {
        private IDbConnection conn;

        public DatabaseInitializationSQL( IDbConnection dbConn ) {
            conn = dbConn;
        }

        public async Task InitializeTablesAndData() {
            string initTables = @"
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

if not exists( select * from sys.tables t join sys.schemas s on ( t.schema_id = s.schema_id ) where s.name = SCHEMA_NAME() and t.name = 'ProcessedItems' )
CREATE TABLE
[ProcessedItems]( 
    [ID] INTEGER NOT NULL PRIMARY KEY IDENTITY, 
    [SubredditID] INTEGER NOT NULL, 
    [ThingID] varchar(20) NOT NULL, 
    [ThingType] tinyint NOT NULL,
    [ActionID] INTEGER, 
    [SeenByModules] INTEGER
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
    [ThingID] varchar(20) NOT NULL,
    [Score] float,
    [Reason] varchar(1000),
    [ReportReason] varchar(255),
    [FlairText] varchar(255),
    [FlairClass] varchar(255),
    [FlairPriority] smallint
);

MERGE Actions WITH (HOLDLOCK) AS v 
Using (VALUES ('Report'),('Remove'),('None')) AS nv (Action) 
ON v.ActionName = nv.Action 
WHEN NOT MATCHED BY TARGET THEN 
INSERT (ActionName) VALUES (nv.Action);
";
            await conn.ExecuteAsync( initTables );
        }
    }
}
