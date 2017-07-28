using Dapper;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace Dirtbag.Helpers
{
    public class ThingIDTVP : SqlMapper.IDynamicParameters {
        
            private readonly IEnumerable<string> _thingIDs;
            /// <summary>Initializes a new instance of the
            /// <see cref="T:System.Object" /> class.</summary>
            public ThingIDTVP( IEnumerable<string> thingIDs ) {
                _thingIDs = thingIDs;
            }
            /// <summary>
            /// Add all the parameters needed to the command just before it executes
            /// </summary>
            /// <param name="command">The raw command prior to execution</param>
            /// <param name="identity">Information about the query</param>
            public void AddParameters( IDbCommand command, SqlMapper.Identity identity ) {
                var sqlCommand = (SqlCommand) command;
                sqlCommand.CommandType = CommandType.Text;
                var items = new List<SqlDataRecord>();
                foreach(var thingID in _thingIDs) {
                    var rec = new SqlDataRecord(new SqlMetaData("ThingID",
                      SqlDbType.NVarChar, 20));
                    rec.SetString(0, thingID);
                    items.Add(rec);
                }
                var p = sqlCommand.Parameters.Add("@thingIDs", SqlDbType.Structured);
                p.Direction = ParameterDirection.Input;
                p.TypeName = "ThingID_TVP"; 
                p.Value = items;
            }
        
    }
}
