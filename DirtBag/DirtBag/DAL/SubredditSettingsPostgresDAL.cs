using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Dirtbag.Models;
using Newtonsoft.Json;

namespace Dirtbag.DAL {
    public class SubredditSettingsPostgresDAL : ISubredditSettingsDAL {
        private IDbConnection conn;
        public SubredditSettingsPostgresDAL( IDbConnection conn ) {
            this.conn = conn;
        }
        public async Task SetSubredditSettingsAsync( SubredditSettings settings ) {

            string subredditQuery = @"
INSERT INTO dirtbag.dirtbag_settings as dbsets (subreddit_id, report_score_threshold, removal_score_threshold, edited_utc, edited_by)
SELECT
s.id, @ReportScoreThreshold, @RemoveScoreThreshold, @LastModified, @ModifiedBy
FROM public.subreddit s
WHERE s.subreddit_name like @Subreddit
ON CONFLICT (subreddit_id) DO UPDATE SET
report_score_threshold = EXCLUDED.report_score_threshold,
removal_score_threshold = EXCLUDED.removal_score_threshold,
edited_utc = EXCLUDED.edited_utc,
edited_by = EXCLUDED.edited_by
WHERE
dbsets.subreddit_id = EXCLUDED.subreddit_id
";
            await conn.ExecuteAsync(subredditQuery, settings).ConfigureAwait(false);


        }

        public Task SetLicensingSmasherSettingsAsync( LicensingSmasherSettings settings, string subreddit ) {

            string licensingQuery = @"
INSERT INTO dirtbag.licensing_smasher as lsmash (subreddit_id, enabled, score_multiplier, edited_utc, edited_by, removal_flair_text, removal_flair_class, removal_flair_priority, removal_flair_enabled)
SELECT s.id, @Enabled, @ScoreMultiplier, @LastModified, @ModifiedBy, @FlairText, @FlairClass, @FlairPriority, @FlairEnabled
FROM public.subreddit s
WHERE s.subreddit_name like @subreddit
ON CONFLICT (subreddit_id) DO UPDATE SET
enabled = EXCLUDED.enabled,
score_multiplier = EXCLUDED.score_multiplier,
edited_utc = EXCLUDED.edited_utc,
edited_by = EXCLUDED.edited_by,
removal_flair_text = EXCLUDED.removal_flair_text,
removal_flair_class = EXCLUDED.removal_flair_class,
removal_flair_priority = EXCLUDED.removal_flair_priority,
removal_flair_enabled = EXCLUDED.removal_flair_enabled
WHERE 
lsmash.subreddit_id = EXCLUDED.subreddit_id
";
            return conn.ExecuteAsync(licensingQuery,
                new {
                    subreddit = new CitextParameter(subreddit),
                    settings.Enabled,
                    settings.LastModified,
                    settings.ModifiedBy,
                    flairclass = settings.RemovalFlair?.Class,
                    flairpriority = settings.RemovalFlair?.Priority,
                    flairtext = settings.RemovalFlair?.Text,
                    flairenabled = settings.RemovalFlair?.Enabled,
                    settings.ScoreMultiplier,
                });

        }

        public Task AddLicensingSmasherTermsAsync( IEnumerable<Models.DAL.LicensingSmasherTerm> terms ) {
            string licensingTermsAddQuery = @"
INSERT INTO dirtbag.licensing_smasher_terms (subreddit_id, match_term)
SELECT s.id, @Term
FROM public.subreddit s
WHERE s.subreddit_name like @Subreddit
";

            /*
             * 
             *  return conn.ExecuteAsync( licensingTermsQuery,
                    settings.LicensingSmasher.MatchTerms.Select( t =>
                     new Dirtbag.Models.DAL.LicensingSmasherTerm {
                         Subreddit = settings.Subreddit,
                         Term = t
                     } )
                    );

             * */

            return conn.ExecuteAsync(licensingTermsAddQuery, terms);
        }
        public Task DeleteLicensingSmasherTermsAsync( IEnumerable<Models.DAL.LicensingSmasherTerm> terms ) {
            string licensingTermsDeleteQuery = @"

DELETE FROM dirtbag.licensing_smasher_terms
WHERE subreddit_id = ( SELECT id from public.Subreddit where subreddit_name like @Subreddit)
AND match_term = @Term";

            return conn.ExecuteAsync(licensingTermsDeleteQuery, terms);
        }


        public Task UpsertLicensingSmasherLicensorsAsync( IEnumerable<Models.DAL.LicensingSmasherLicensor> licensors ) {



            string licensingCompaniesQuery = @"
INSERT INTO dirtbag.licensing_smasher_licensors as lsmash (subreddit_id, licensor_id, display_name)
SELECT s.id, @LicensorID, @DisplayName
FROM public.subreddit s
WHERE s.subreddit_name like @subreddit
ON CONFLICT (subreddit_id, licensor_id) DO UPDATE SET
display_name = EXCLUDED.display_name
WHERE 
lsmash.subreddit_id = EXCLUDED.subreddit_id
AND lsmash.licensor_id = EXCLUDED.licensor_id;
";

            return conn.ExecuteAsync(licensingCompaniesQuery, licensors);
        }

        public Task DeleteLicensingSmasherLicensorsAsync( IEnumerable<Models.DAL.LicensingSmasherLicensor> licensors ) {

            string licensingCompaniesDelete = @"
            DELETE FROM dirtbag.licensing_smasher_licensors
            WHERE subreddit_id = ( SELECT id from public.Subreddit where subreddit_name like @Subreddit)
AND licensor_id = @LicensorID;
";

            return conn.ExecuteAsync(licensingCompaniesDelete, licensors);
        }

        public Task SetSelfPromoSettingsAsync( SelfPromotionCombustorSettings settings, string subreddit ) {

            string selfPromoQuery = @"
INSERT INTO dirtbag.self_promotion_combustor as spromo (subreddit_id, enabled, score_multiplier, percentage_threshold, include_post, grace_period, edited_utc, edited_by, removal_flair_text, removal_flair_class, removal_flair_priority, removal_flair_enabled)
SELECT s.id, @Enabled, @ScoreMultiplier, @PercentageThreshold, @IncludePostInPercentage, @GracePeriod, @LastModified, @ModifiedBy, @FlairText, @FlairClass, @FlairPriority, @FlairEnabled
FROM public.subreddit s
WHERE s.subreddit_name like @subreddit
ON CONFLICT (subreddit_id) DO UPDATE SET
enabled = EXCLUDED.enabled,
score_multiplier = EXCLUDED.score_multiplier,
percentage_threshold = EXCLUDED.percentage_threshold,
include_post = EXCLUDED.include_post,
grace_period = EXCLUDED.grace_period,
edited_utc = EXCLUDED.edited_utc,
edited_by = EXCLUDED.edited_by,
removal_flair_text = EXCLUDED.removal_flair_text,
removal_flair_class = EXCLUDED.removal_flair_class,
removal_flair_priority = EXCLUDED.removal_flair_priority,
removal_flair_enabled = EXCLUDED.removal_flair_enabled
WHERE 
spromo.subreddit_id = EXCLUDED.subreddit_id
";
            return conn.ExecuteAsync(selfPromoQuery,
                    new {
                        subreddit = new CitextParameter(subreddit),
                        settings.Enabled,
                        settings.GracePeriod,
                        settings.IncludePostInPercentage,
                        settings.LastModified,
                        settings.ModifiedBy,
                        settings.PercentageThreshold,
                        flairclass = settings.RemovalFlair?.Class,
                        flairpriority = settings.RemovalFlair?.Priority,
                        flairtext = settings.RemovalFlair?.Text,
                        flairenabled = settings.RemovalFlair?.Enabled,
                        settings.ScoreMultiplier
                    });
        }

        public Task SetSpamDetectorSettingsAsync( YouTubeSpamDetectorSettings settings, string subreddit ) {


            string spamDetectorQuery = @"
INSERT INTO dirtbag.spam_detector as spamd (subreddit_id, enabled, score_multiplier, edited_utc, edited_by, removal_flair_text, removal_flair_class, removal_flair_priority, removal_flair_enabled)
SELECT s.id, @Enabled, @ScoreMultiplier, @LastModified, @ModifiedBy, @FlairText, @FlairClass, @FlairPriority, @FlairEnabled
FROM public.subreddit s
WHERE s.subreddit_name like @subreddit
ON CONFLICT (subreddit_id) DO UPDATE SET
enabled = EXCLUDED.enabled,
score_multiplier = EXCLUDED.score_multiplier,
edited_utc = EXCLUDED.edited_utc,
edited_by = EXCLUDED.edited_by,
removal_flair_text = EXCLUDED.removal_flair_text,
removal_flair_class = EXCLUDED.removal_flair_class,
removal_flair_priority = EXCLUDED.removal_flair_priority,
removal_flair_enabled = EXCLUDED.removal_flair_enabled
WHERE 
spamd.subreddit_id = EXCLUDED.subreddit_id
";

            return conn.ExecuteAsync(spamDetectorQuery,
                new {
                    subreddit = new CitextParameter(subreddit),
                    settings.Enabled,
                    settings.LastModified,
                    settings.ModifiedBy,
                    flairclass = settings.RemovalFlair?.Class,
                    flairpriority = settings.RemovalFlair?.Priority,
                    flairtext = settings.RemovalFlair?.Text,
                    flairenabled = settings.RemovalFlair?.Enabled,
                    settings.ScoreMultiplier
                });

        }

        public Task SetSpamDetectorModuleSettingsAsync( IEnumerable<Models.DAL.SpamDetectorModule> modules ) {

            string spamDetectorModulesQuery = @"
INSERT INTO dirtbag.spam_detector_modules as spamd (subreddit_id, module_name, enabled, threshold, weight)
SELECT s.id, @Name, @Enabled, @Value, @Weight
FROM public.subreddit s
WHERE s.subreddit_name like @subreddit
ON CONFLICT (subreddit_id, module_name) DO UPDATE SET
enabled = EXCLUDED.enabled,
threshold = EXCLUDED.threshold,
weight = EXCLUDED.weight
WHERE
spamd.subreddit_id = EXCLUDED.subreddit_id
AND spamd.module_name = EXCLUDED.module_name
";
            return conn.ExecuteAsync(spamDetectorModulesQuery, modules);

        }

        public async Task<Models.SubredditSettings> GetSubredditSettingsAsync( string subreddit ) {
            string subredditQuery = @"
SELECT 
s.subreddit_name ""Subreddit"", s.redditbot_name ""BotName"", oauth.password ""BotPass"", oauth.app_id ""BotAppID"", oauth.app_secret ""BotAppSecret"", 
    dbag.report_score_threshold ""ReportScoreThreshold"", dbag.removal_score_threshold ""RemoveScoreThreshold"",
    dbag.edited_utc ""LastModified"", dbag.edited_by ""ModifiedBy""

FROM dirtbag.dirtbag_settings dbag
INNER JOIN public.subreddit s on s.id = dbag.subreddit_id
left join dirtbag.traveler_oauth_data oauth on oauth.username ilike s.redditbot_name
where s.subreddit_name like @subreddit
AND s.dirtbag_enabled = true
";

            string licensingSmasherQuery = @"
SELECT
ls.enabled ""Enabled"", ls.score_multiplier ""ScoreMultiplier"", ls.edited_utc ""LastModified"", ls.edited_by ""ModifiedBy"",
ls.removal_flair_text ""Text"", ls.removal_flair_class ""Class"", ls.removal_flair_priority ""Priority"", ls.removal_flair_enabled ""Enabled"",
ls_t.match_term ""MatchTerms""

FROM dirtbag.licensing_smasher ls
INNER JOIN public.subreddit s on s.id = ls.subreddit_id
LEFT JOIN dirtbag.licensing_smasher_terms ls_t on ls_t.subreddit_id = s.id

WHERE s.subreddit_name like @subreddit
";
            string licensingSmasherLicensors = @"
SELECT
ls_l.licensor_id ""Key"", ls_l.display_name ""Value""
FROM dirtbag.licensing_smasher_licensors ls_l 
INNER JOIN public.subreddit s on s.id = ls_l.subreddit_id

WHERE s.subreddit_name like @subreddit
";
            string selfPromoQuery = @"
SELECT
spc.enabled ""Enabled"", spc.score_multiplier ""ScoreMultiplier"", spc.percentage_threshold ""PercentageThreshold"", spc.include_post ""IncludePostInPercentage"", 
    spc.grace_period ""GracePeriod"", spc.edited_utc ""LastModified"", spc.edited_by ""ModifiedBy"",
spc.removal_flair_text ""Text"", spc.removal_flair_class ""Class"", spc.removal_flair_priority ""Priority"", spc.removal_flair_enabled ""Enabled""

FROM dirtbag.self_promotion_combustor spc 
INNER JOIN public.subreddit s on s.id = spc.subreddit_id

WHERE s.subreddit_name like @subreddit
";
            string spamDetectQuery = @"
SELECT
sd.enabled ""Enabled"", sd.score_multiplier ""ScoreMultiplier"", sd.edited_utc ""LastModified"", sd.edited_by ""ModifiedBy"",
sd.removal_flair_text ""Text"", sd.removal_flair_class ""Class"", sd.removal_flair_priority ""Priority"", sd.removal_flair_enabled ""Enabled"",
sd_m.module_name ""Name"", sd_m.enabled ""Enabled"", sd_m.threshold ""Value"", sd_m.weight ""Weight""

FROM dirtbag.spam_detector sd
INNER JOIN public.subreddit s on s.id = sd.subreddit_id
LEFT JOIN dirtbag.spam_detector_modules sd_m on sd_m.subreddit_id = s.id

WHERE s.subreddit_name like @subreddit
";
            Models.SubredditSettings toReturn = null;
            toReturn = await conn.QuerySingleOrDefaultAsync<SubredditSettings>(subredditQuery, new { subreddit = new CitextParameter(subreddit) }).ConfigureAwait(false);
            if(toReturn == null) { return null; }


            await conn.QueryAsync<LicensingSmasherSettings, Flair, string, LicensingSmasherSettings>(licensingSmasherQuery,
            ( ls, f, term ) => {
                if(toReturn.LicensingSmasher == null) {
                    ls.RemovalFlair = f ?? new Flair();
                    toReturn.LicensingSmasher = ls;
                }
                if(term != null) {
                    toReturn.LicensingSmasher.MatchTerms.Add(term);
                }
                return ls;
            }
            , param: new { subreddit = new CitextParameter(subreddit) }, splitOn: "Text,MatchTerms").ConfigureAwait(false);
            if(toReturn.LicensingSmasher != null) {
                toReturn.LicensingSmasher.KnownLicensers = (await conn.QueryAsync(licensingSmasherLicensors, new { subreddit = new CitextParameter(subreddit) })).Select(r => new KeyValuePair<string,string>((string) r.Key, (string) r.Value)).ToList();
            }
            toReturn.SelfPromotionCombustor =
                (
                    await conn.QueryAsync<SelfPromotionCombustorSettings, Flair, SelfPromotionCombustorSettings>(selfPromoQuery,
                    ( sp, flair ) => {
                        sp.RemovalFlair = flair ?? new Flair();
                        return sp;
                    }
                    , param: new { subreddit = new CitextParameter(subreddit) }, splitOn: "Text").ConfigureAwait(false)
                ).SingleOrDefault();


            await conn.QueryAsync<YouTubeSpamDetectorSettings, Flair, YouTubeSpamDetectorModule, YouTubeSpamDetectorSettings>(spamDetectQuery,
            ( sd, flair, module ) => {
                if(toReturn.YouTubeSpamDetector == null) {
                    sd.RemovalFlair = flair ?? new Flair();
                    toReturn.YouTubeSpamDetector = sd;
                }
                switch(module?.Name) {
                    case "ChannelAgeThreshold":
                        toReturn.YouTubeSpamDetector.ChannelAgeThreshold = module; break;
                    case "ViewCountThreshold":
                        toReturn.YouTubeSpamDetector.ViewCountThreshold = module; break;
                    case "VoteCountThreshold":
                        toReturn.YouTubeSpamDetector.VoteCountThreshold = module; break;
                    case "NegativeVoteRatio":
                        toReturn.YouTubeSpamDetector.NegativeVoteRatio = module; break;
                    case "RedditAccountAgeThreshold":
                        toReturn.YouTubeSpamDetector.RedditAccountAgeThreshold = module; break;
                    case "LicensedChannel":
                        toReturn.YouTubeSpamDetector.LicensedChannel = module; break;
                    case "ChannelSubscribersThreshold":
                        toReturn.YouTubeSpamDetector.ChannelSubscribersThreshold = module; break;
                    case "CommentCountThreshold":
                        toReturn.YouTubeSpamDetector.CommentCountThreshold = module; break;
                }
                return sd;
            }, param: new { subreddit = new CitextParameter(subreddit) }, splitOn: "Text,Name").ConfigureAwait(false);



            return toReturn;
        }

        public Task<IEnumerable<string>> GetLicensingSmasherSubredditsAsync() {
            string licensingSmasherSubsQuery = @"
select s.subreddit_name ""Subreddit""
FROM dirtbag.licensing_smasher ls
INNER JOIN public.subreddit s on s.id = ls.subreddit_id
WHERE s.dirtbag_enabled = true and ls.enabled = true
";
            return conn.QueryAsync<string>(licensingSmasherSubsQuery);
        }

        public Task<bool> DirtbagEnabledAsync(string subreddit ) {
            string query = @"
select count(1) 
from public.subreddit s 
where s.subreddit_name = @subreddit AND s.dirtbag_enabled = true";
            return conn.ExecuteScalarAsync<bool>(query, new { subreddit = new CitextParameter(subreddit) });
        }
    }
}
