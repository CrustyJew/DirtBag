using Dapper;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DirtbagWebservice.DAL
{
    public class CitextParameter : SqlMapper.ICustomQueryParameter {
        readonly string _value;

        public CitextParameter( string value ) {
            _value = value;
        }

        public void AddParameter( IDbCommand command, string name ) {
            command.Parameters.Add(new NpgsqlParameter {
                ParameterName = name,
                NpgsqlDbType = NpgsqlDbType.Citext,
                Value = _value
            });
        }
    }
}
