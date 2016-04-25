using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace DirtBag.Logging {
    class BannedEntities {
        //TODO refactor to DAL
        public async Task LogNewBannedEntities(IEnumerable<Models.BannedEntity> entities ) {
            string query = @"
insert into BannedEntities (SubredditID,EntityString,BannedBy, BanReason, BanDate, ThingID)
select sub.ID,@EntityString,@BannedBy,@BanReason,@BanDate,@ThingID from Subreddits sub where sub.SubName like @SubName
;";
            using ( var conn = DirtBagConnection.GetConn() ) {
                await conn.ExecuteAsync( query, entities );
                return;
            }
        }

        public async Task<IEnumerable<Models.BannedEntity>> GetBannedEntities(string subredditName ) {
            string query = @"
select be.Id, sub.SubName, be.EntityString, be.BannedBy, be.BanReason, be.BanDate, be.ThingID
from BannedEntities be
inner join Subreddits sub on sub.ID = be.SubredditID
where sub.SubName like @subredditName
;";
            using ( var conn = DirtBagConnection.GetConn() ) {
                return await conn.QueryAsync<Models.BannedEntity>( query, new { subredditName } );
            }
        }

        public async Task<string> RemoveBannedEntity(int id, string subredditName, string modName ) {
            string query = @"
delete be
output GETUTCDATE() as 'DeletedTimestamp', @modName as 'DeletedBy', DELETED.SubredditID, DELETED.EntityString, DELETED.BannedBy, DELETED.BanReason, DELETED.BanDate, DELETED.ThingID INTO BannedEntities_History
output DELETED.EntityString
from BannedEntities be
inner join Subreddits sub on sub.ID = be.SubredditID
where sub.SubName like @subredditName
AND be.Id = @id
;";
            using ( var conn = DirtBagConnection.GetConn() ) {
                return (await conn.QueryAsync<string>( query, new { id, subredditName, @modName } )).FirstOrDefault();
            }
        }
    }
}
