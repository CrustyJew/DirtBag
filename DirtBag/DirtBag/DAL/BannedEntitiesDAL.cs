using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DirtBag.Logging;

namespace DirtBag.DAL {
    class BannedEntities {
        public async Task LogNewBannedEntities(IEnumerable<Models.BannedEntity> entities ) {
            string query = @"
insert into BannedEntities (SubredditID,EntityString,EntityType,BannedBy, BanReason, BanDate, ThingID)
select sub.ID,@EntityString,@Type,@BannedBy,@BanReason,@BanDate,@ThingID from Subreddits sub where sub.SubName like @SubName
;";
            using ( var conn = DirtBagConnection.GetConn() ) {
                await conn.ExecuteAsync( query, entities );
                return;
            }
        }

        public async Task<IEnumerable<Models.BannedEntity>> GetBannedEntities(string subredditName ) {
            string query = @"
select be.Id, sub.SubName, be.EntityString, be.EntityType as 'Type', be.BannedBy, be.BanReason, be.BanDate, be.ThingID
from BannedEntities be
inner join Subreddits sub on sub.ID = be.SubredditID
where sub.SubName like @subredditName
;";
            using ( var conn = DirtBagConnection.GetConn() ) {
                return await conn.QueryAsync<Models.BannedEntity>( query, new { subredditName } );
            }
        }

        public async Task<IEnumerable<Models.BannedEntity>> GetBannedUsers( string subredditName ) {
            string query = @"
select be.Id, sub.SubName, be.EntityString, be.EntityType as 'Type', be.BannedBy, be.BanReason, be.BanDate, be.ThingID
from BannedEntities be
inner join Subreddits sub on sub.ID = be.SubredditID
where sub.SubName like @subredditName AND be.EntityType = 2
;";
            using ( var conn = DirtBagConnection.GetConn() ) {
                return await conn.QueryAsync<Models.BannedEntity>( query, new { subredditName } );
            }
        }

        public async Task<IEnumerable<Models.BannedEntity>> GetBannedChannels( string subredditName ) {
            string query = @"
select be.Id, sub.SubName, be.EntityString, be.EntityType as 'Type', be.BannedBy, be.BanReason, be.BanDate, be.ThingID
from BannedEntities be
inner join Subreddits sub on sub.ID = be.SubredditID
where sub.SubName like @subredditName AND be.EntityType = 1
;";
            using ( var conn = DirtBagConnection.GetConn() ) {
                return await conn.QueryAsync<Models.BannedEntity>( query, new { subredditName } );
            }
        }
        public async Task<string> RemoveBannedEntity(int id, string subredditName, string modName ) {
            string query = @"
delete be
output GETUTCDATE() as 'HistTimestamp', 'D' as 'HistAction', @modName as 'HistUser', DELETED.SubredditID, DELETED.EntityString, DELETED.EntityType, DELETED.BannedBy, DELETED.BanReason, DELETED.BanDate, DELETED.ThingID INTO BannedEntities_History
output DELETED.EntityString, DELETED.EntityType
from BannedEntities be
inner join Subreddits sub on sub.ID = be.SubredditID
where sub.SubName like @subredditName
AND be.Id = @id
;";
            using ( var conn = DirtBagConnection.GetConn() ) {
                dynamic results = (await conn.QueryAsync<dynamic>( query, new { id, subredditName, @modName } )).FirstOrDefault();
                if ( results.EntityType == 2 ) return results.EntityString;
                return "";
            }
        }

        public async Task<bool> UpdateBanReason( int id, string subredditName, string modName, string banReason ) {
            string query = @"
update be
SET be.BanReason = @banReason
OUTPUT GETUTCDATE() as 'HistTimestamp', 'U' as 'HistAction', @modName as 'HistUser', INSERTED.SubredditID, INSERTED.EntityString, INSERTED.EntityType, INSERTED.BannedBy, INSERTED.BanReason, INSERTED.BanDate, INSERTED.ThingID INTO BannedEntities_History
FROM BannedEntities be
inner join Subreddits sub on sub.ID = be.SubredditID
where sub.SubName like @subredditName AND be.id = @id
";
            using ( var conn = DirtBagConnection.GetConn() ) {
                var results = await conn.ExecuteAsync( query, new { id, subredditName, modName, banReason } );
                if ( results == 1 ) return true;
                return false;
            }
        }
    }
}
