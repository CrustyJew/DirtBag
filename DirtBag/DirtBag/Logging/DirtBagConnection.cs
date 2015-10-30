using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using Dapper;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirtBag.Logging {
	class DirtBagConnection {
		public bool UseLocalDB { get; set; }
		public string SQLConnString { get; set; }
		public string LocalDBFile { get; set; }

		private DbConnection dbConn;
		public void InitializeConnection(string[] subreddits) {
			string masterTable = "";
			UseLocalDB = bool.Parse(ConfigurationManager.AppSettings["UseLocalDB"]);
			if ( UseLocalDB ) {
				LocalDBFile = ConfigurationManager.AppSettings["LocalDBFile"];
				if ( string.IsNullOrEmpty( LocalDBFile ) ) {
					throw new Exception( "UseLocalDB set to true but no LocalDBFile path specified." );
				}
				if ( !File.Exists( LocalDBFile ) ) {
					SQLiteConnection.CreateFile( LocalDBFile );
				}
				masterTable = "sqlite_master";
			}
			else {
				SQLConnString = ConfigurationManager.AppSettings["SQLConnString"];
				if ( string.IsNullOrEmpty( SQLConnString ) ) {
					throw new Exception( "UseLocalDB set to false but no SQLConnString specified" );
				}
				masterTable = "master";
			}

			using ( DbConnection con = GetConn() ) {
				string initTables = "" +
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

				string subs = "('" + string.Join( "'),('", subreddits ) + "') " ;
                string seedData = "" +
					"INSERT INTO SubReddits (SubName) " +
					"values " + subs +
					"EXCEPT " +
					"SELECT SubName from SubReddits" +
					";"+
					"INSERT INTO Actions (ActionName) " +
					"VALUES ('Report'),('Remove') " +
					"EXCEPT " +
					"SELECT ActionName from Actions" +
					";";

				con.Execute( seedData);

			}
        }

		private DbConnection GetConn() {
			if ( UseLocalDB ) {
				return new SQLiteConnection( "Data Source=" + LocalDBFile );
			}
			else {
				return new SqlConnection( SQLConnString );
			}
		}
	}
}
