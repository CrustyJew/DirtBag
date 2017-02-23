using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DirtBagWebservice.Models;

namespace DirtBagWebservice.DAL
{
    public class SubredditSettingsPostgresDAL : ISubredditSettingsDAL
    {
        private IDbConnection conn;
        public SubredditSettingsPostgresDAL(IDbConnection conn)
        {
            this.conn = conn;
        }
        public async Task UpdateSubredditSettingsAsync(SubredditSettings settings)
        {
            string subredditQuery = @"
INSERT INTO dirtbag.dirtbag_settings (subreddit_id, report_score_threshold, removal_score_threshold, edited_utc, edited_by)
SELECT
s.id, @ReportScoreThreshold, @RemovalScoreThreshold, @LastModified, @ModifiedBy
FROM public.subreddit s
WHERE s.subreddit_name like @subreddit
ON CONFLICT (subreddit_id) DO UPDATE SET
report_score_threshold = EXCLUDED.report_score_threshold,
removal_score_threshold = EXCLUDED.removal_score_threshold,
edited_utc = EXCLUDED.edited_utc,
edited_by = EXCLUDED.edited_by
WHERE
subreddit_id = EXCLUDED.subreddit_id
";

            string licensingQuery = @"
INSERT INTO dirtbag.licensing_smasher (subreddit_id, enabled, score_multiplier, edited_utc, edited_by, removal_flair_text, removal_flair_class, removal_flair_priority)
SELECT s.id, @Enabled, @ScoreMultiplier, @LastModified, @ModifiedBy, @FlairText, @FlairClass, @FlairPriority
FROM public.subreddit s
WHERE s.subreddit_name like @subreddit
ON CONFLICT (subreddit_id) DO UPDATE SET
enabled = EXCLUDED.enabled,
score_multiplier = EXCLUDED.score_multiplier,
edited_utc = EXCLUDED.edited_utc,
edited_by = EXCLUDED.edited_by,
removal_flair_text = EXCLUDED.removal_flair_text,
removal_flair_class = EXCLUDED.removal_flair_class,
removal_flair_priority = EXCLUDED.removal_flair_priority
WHERE 
subreddit_id = EXCLUDED.subreddit_id
";

            string licensingTermsQuery = @"
INSERT INTO dirtbag.licensing_smasher_terms (subreddit_id, match_term)
SELECT s.id, @Term
FROM public.subreddit s
WHERE s.subreddit_name like @subreddit
ON CONFLICT (subreddit_id, match_term) DO NOTHING;

DELETE FROM dirtbag.licensing_smasher_terms
WHERE subreddit_id = (SELECT id from public.Subreddit where subreddit_name like @Subreddit)
AND term not in @Term;
";

            string licensingCompaniesQuery = @"
INSERT INTO dirtbag.licensing_smasher_licensors (subreddit_id, licensor_id, display_name)
SELECT s.id, @LicensorID, @DisplayName
FROM public.subreddit s
WHERE s.subreddit_name like @subreddit
ON CONFLICT (subreddit_id, licensor_id) DO UPDATE SET
display_name = EXCLUDED.display_name
WHERE 
subreddit_id = EXCLUDED.subreddit_id
AND licensor_id = EXCLUDED.licensor_id;

DELETE FROM dirtbag.licensing_smasher_licensors
WHERE subreddit_id = (SELECT id from public.Subreddit where subreddit_name like @Subreddit)
AND licensor_id in @LicensorID;
";

            string selfPromoQuery = @"
INSERT INTO dirtbag.self_promotion_combustor (subreddit_id, enabled, score_multiplier, percentage_threshold, include_post, grace_period, edited_utc, edited_by, removal_flair_text, removal_flair_class, removal_flair_priority)
SELECT s.id, @Enabled, @ScoreMultiplier, @PercentageThreshold, @IncludePostInPercentage, @GracePeriod, @LastModified, @ModifiedBy, @FlairText, @FlairClass, @FlairPriority
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
removal_flair_priority = EXCLUDED.removal_flair_priority
WHERE 
subreddit_id = EXCLUDED.subreddit_id
";

            string spamDetectorQuery = @"
INSERT INTO dirtbag.spam_detector (subreddit_id, enabled, score_multiplier, edited_utc, edited_by, removal_flair_text, removal_flair_class, removal_flair_priority)
SELECT s.id, @Enabled, @ScoreMultiplier, @LastModified, @ModifiedBy, @FlairText, @FlairClass, @FlairPriority
FROM public.subreddit s
WHERE s.subreddit_name like @subreddit
ON CONFLICT (subreddit_id) DO UPDATE SET
enabled = EXCLUDED.enabled,
score_multiplier = EXCLUDED.score_multiplier,
edited_utc = EXCLUDED.edited_utc,
edited_by = EXCLUDED.edited_by,
removal_flair_text = EXCLUDED.removal_flair_text,
removal_flair_class = EXCLUDED.removal_flair_class,
removal_flair_priority = EXCLUDED.removal_flair_priority
WHERE 
subreddit_id = EXCLUDED.subreddit_id
";

            string spamDetectorModulesQuery = @"
INSERT INTO dirtbag.spam_detector_modules (subreddit_id, module_name, enabled, threshold, weight)
SELECT s.id, @Name, @Enabled, @Value, @Weight
FROM public.subreddit s
WHERE s.subreddit_name like @subreddit
ON CONFLICT (subreddit_id, module_name) DO UPDATE SET
enabled = EXCLUDED.enabled,
threshold = EXCLUDED.threshold,
weight = EXCLUDED.weight
WHERE
subreddit_id = EXCLUDED.subreddit_id
AND module_name = EXCLUDED.module_name
";

            using (var transact = conn.BeginTransaction())
            {
                await conn.ExecuteAsync(subredditQuery, settings);
                await conn.ExecuteAsync(licensingQuery, new { settings.Subreddit, settings.LicensingSmasher });
                await conn.ExecuteAsync(licensingTermsQuery, settings.LicensingSmasher.MatchTerms.Select(t=>new DirtBagWebservice.Models.DAL.LicensingSmasherTerm{ Subreddit = settings.Subreddit, Term = t}));
                await conn.ExecuteAsync(licensingCompaniesQuery, settings.LicensingSmasher.KnownLicensers.Select(l => new DirtBagWebservice.Models.DAL.LicensingSmasherLicensor { Subreddit = settings.Subreddit, LicensorID = l.Key }));
                await conn.ExecuteAsync(selfPromoQuery, new { settings.Subreddit, settings.SelfPromotionCombustor });
                await conn.ExecuteAsync(spamDetectorQuery, new { settings.Subreddit, settings.YouTubeSpamDetector });
                List<Models.DAL.SpamDetectorModule> spamModules = new List<Models.DAL.SpamDetectorModule>();
                spamModules.Add(new Models.DAL.SpamDetectorModule(settings.Subreddit, settings.YouTubeSpamDetector.ChannelAgeThreshold));
                spamModules.Add(new Models.DAL.SpamDetectorModule(settings.Subreddit, settings.YouTubeSpamDetector.CommentCountThreshold));
                spamModules.Add(new Models.DAL.SpamDetectorModule(settings.Subreddit, settings.YouTubeSpamDetector.LicensedChannel));
                spamModules.Add(new Models.DAL.SpamDetectorModule(settings.Subreddit, settings.YouTubeSpamDetector.NegativeVoteRatio));
                spamModules.Add(new Models.DAL.SpamDetectorModule(settings.Subreddit, settings.YouTubeSpamDetector.RedditAccountAgeThreshold));
                spamModules.Add(new Models.DAL.SpamDetectorModule(settings.Subreddit, settings.YouTubeSpamDetector.ViewCountThreshold));
                spamModules.Add(new Models.DAL.SpamDetectorModule(settings.Subreddit, settings.YouTubeSpamDetector.VoteCountThreshold));
                spamModules.Add(new Models.DAL.SpamDetectorModule(settings.Subreddit, settings.YouTubeSpamDetector.ChannelSubscribersThreshold));
                await conn.ExecuteAsync(spamDetectorModulesQuery, spamModules);
                transact.Commit();
            }

        }
        public async Task<Models.SubredditSettings> GetSubredditSettingsAsync(string subreddit)
        {
            string subredditQuery = @"
SELECT 
s.subreddit_name ""Subreddit"", dbag.report_score_threshold ""ReportScoreThreshold"", dbag.removal_score_threshold ""RemovalScoreThreshold"",
    dbag.edited_utc ""LastModified"", dbag.edited_by ""ModifiedBy""

FROM dirtbag.dirtbag_settings dbag
INNER JOIN public.subreddit s on s.id = dbag.subreddit_id
where s.subreddit_name like @subreddit
";

            string licensingSmasherQuery = @"
SELECT
ls.enabled ""Enabled"", ls.score_multiplier ""ScoreMultiplier"", ls.edited_utc ""LastModified"", ls.edited_by ""ModifiedBy"",
ls.removal_flair_text ""Text"", ls.removal_flair_class ""Class"", ls.removal_flair_priority ""Priority"",
ls_t.match_term ""MatchTerms"",
ls_l.licensor_id ""LicensorID"", ls_l.display_name ""DisplayName"",

FROM dirtbag.licensing_smasher ls
INNER JOIN public.subreddit s on s.id = ls.subreddit_id
LEFT JOIN dirtbag.licensing_smasher_terms ls_t on ls_t.subreddit_id = s.id
LEFT JOIN dirtbag.licensing_smasher_licensors ls_l on ls_l.subreddit_id = s.id

WHERE s.subreddit_name like @subreddit
";

            string selfPromoQuery = @"
SELECT
spc.enabled ""Enabled"", spc.score_multiplier ""ScoreMultiplier"", spc.percentage_threshold ""PercentageThreshold"", spc.include_post ""IncludePostInPercentage"", 
    spc.grace_period ""GracePeriod"", spc.edited_utc ""LastModified"", spc.edited_by ""ModifiedBy"",
spc.removal_flair_text ""Text"", spc.removal_flair_class ""Class"", spc.removal_flair_priority ""Priority""

FROM dirtbag.self_promotion_combustor spc 
INNER JOIN public.subreddit s on s.id = spc.subreddit_id

WHERE s.subreddit_name like @subreddit
";
            string spamDetectQuery = @"
SELECT
sd.enabled ""Enabled"", sd.score_multiplier ""ScoreMultiplier"", sd.edited_utc ""LastModified"", sd.edited_by ""ModifiedBy"",
sd.removal_flair_text ""Text"", sd.removal_flair_class ""Class"", sd.removal_flair_priority ""Priority"",
sd_m.module_name ""Name"", sd_m.enabled ""Enabled"", sd_m.threshold ""Value"", sd_m.weight ""Weight""

FROM dirtbag.spam_detector sd
INNER JOIN public.subreddit s on s.id = sd.subreddit_id
LEFT JOIN dirtbag.spam_detector_modules sd_m on sd_m.subreddit_id = s.id

WHERE s.subreddit_name like @subreddit
";
            Models.SubredditSettings toReturn = new Models.SubredditSettings();
            toReturn = await conn.QuerySingleOrDefaultAsync<SubredditSettings>(subredditQuery, new { subreddit });
            if (toReturn == null ) { return null; }

            toReturn.LicensingSmasher =
                (
                    await conn.QueryAsync<LicensingSmasherSettings, Flair, string[], Dictionary<string, string>, LicensingSmasherSettings>(licensingSmasherQuery,
                    (ls, f, terms, dict) =>
                    {
                        ls.RemovalFlair = f;
                        ls.KnownLicensers = dict;
                        ls.MatchTerms = terms;
                        return ls;
                    }
                    , param: new { subreddit }, splitOn:"Text,MatchTerms,LicensorID")
                ).SingleOrDefault();

            toReturn.SelfPromotionCombustor =
                (
                    await conn.QueryAsync<SelfPromotionCombustorSettings, Flair, SelfPromotionCombustorSettings>(selfPromoQuery,
                    (sp, flair) =>
                    {
                        sp.RemovalFlair = flair;
                        return sp;
                    }
                    , param: new { subreddit }, splitOn:"Text")
                ).SingleOrDefault();

            toReturn.YouTubeSpamDetector =
                (
                    await conn.QueryAsync<YouTubeSpamDetectorSettings,Flair,YouTubeSpamDetectorModule,YouTubeSpamDetectorSettings>(spamDetectQuery,
                    (sd,flair,module) =>
                    {
                        sd.RemovalFlair = flair;
                        switch (module.Name)
                        {
                            case "ChannelAgeThreshold":
                                sd.ChannelAgeThreshold = module;break;
                            case "ViewCountThreshold":
                                sd.ViewCountThreshold = module; break;
                            case "VoteCountThreshold":
                                sd.VoteCountThreshold = module; break;
                            case "NegativeVoteRatio":
                                sd.NegativeVoteRatio = module; break;
                            case "RedditAccountAgeThreshold":
                                sd.RedditAccountAgeThreshold = module; break;
                            case "LicensedChannel":
                                sd.LicensedChannel = module; break;
                            case "ChannelSubscribersThreshold":
                                sd.ChannelSubscribersThreshold = module; break;
                            case "CommentCountThreshold":
                                sd.CommentCountThreshold = module; break;
                        }
                        return sd;
                    }, param:new { subreddit },splitOn:"Text,Name")

                ).SingleOrDefault();

            return toReturn;
        }
    }
}
