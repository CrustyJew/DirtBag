using System.Collections.Generic;
using System.Threading.Tasks;
using DirtBagWebservice.Models;

namespace DirtBagWebservice.DAL {
    public interface ISubredditSettingsDAL {
        Task AddLicensingSmasherTermsAsync( IEnumerable<Models.DAL.LicensingSmasherTerm> terms );
        Task DeleteLicensingSmasherLicensorsAsync( IEnumerable<Models.DAL.LicensingSmasherLicensor> licensors );
        Task DeleteLicensingSmasherTermsAsync( IEnumerable<Models.DAL.LicensingSmasherTerm> terms );
        Task<SubredditSettings> GetSubredditSettingsAsync( string subreddit );
        Task SetLicensingSmasherSettingsAsync( LicensingSmasherSettings settings, string subreddit );
        Task SetSelfPromoSettingsAsync( SelfPromotionCombustorSettings settings, string subreddit );
        Task SetSpamDetectorModuleSettingsAsync( IEnumerable<Models.DAL.SpamDetectorModule> modules );
        Task SetSpamDetectorSettingsAsync( YouTubeSpamDetectorSettings settings, string subreddit );
        Task SetSubredditSettingsAsync( SubredditSettings settings );
        Task UpsertLicensingSmasherLicensorsAsync( IEnumerable<Models.DAL.LicensingSmasherLicensor> licensors );
    }
}