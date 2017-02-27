using DirtBagWebservice.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace DirtBagWebservice.BLL {
    public class SubredditSettingsBLL : ISubredditSettingsBLL {
        private static IMemoryCache cache;
        private const string CACHE_PREFIX = "SubredditSettings:";
        private DAL.ISubredditSettingsDAL dal;

        public SubredditSettingsBLL( DAL.ISubredditSettingsDAL ssDAL, IMemoryCache memCache ) {
            dal = ssDAL;
            cache = memCache;
        }

        public Task<SubredditSettings> GetSubredditSettingsAsync( string subreddit, bool defaults = false ) {
            return GetOrUpdateSettingsAsync( subreddit, defaults );
        }

        public async Task SetSubredditSettingsAsync( SubredditSettings settings, string modifiedBy ) {
            var currentSettings = await GetOrUpdateSettingsAsync( settings.Subreddit );

            PurgeSubSettingsFromCache( settings.Subreddit );
            DateTime now = DateTime.UtcNow;

            if ( !CompareSettings(currentSettings, settings ) ) {
                settings.ModifiedBy = modifiedBy;
                settings.LastModified = now;
                await dal.SetSubredditSettingsAsync( settings );
            }
            if(!CompareSettings(currentSettings.LicensingSmasher, settings.LicensingSmasher ) ) {
                settings.LicensingSmasher.ModifiedBy = modifiedBy;
                settings.LicensingSmasher.LastModified = now;

                await dal.SetLicensingSmasherSettingsAsync( settings.LicensingSmasher, settings.Subreddit );

                var termsToAdd = settings.LicensingSmasher.MatchTerms.Where( t => !currentSettings.LicensingSmasher.MatchTerms.Contains( t ) ).Select(t=>new Models.DAL.LicensingSmasherTerm() {Term = t, Subreddit = settings.Subreddit });
                var termsToRemove = currentSettings.LicensingSmasher.MatchTerms.Where( t => !settings.LicensingSmasher.MatchTerms.Contains( t ) ).Select( t => new Models.DAL.LicensingSmasherTerm() { Term = t, Subreddit = settings.Subreddit } );

                if(termsToAdd.Count() > 0 ) {
                    await dal.AddLicensingSmasherTermsAsync( termsToAdd );
                }
                if(termsToRemove.Count() > 0 ) {
                    await dal.DeleteLicensingSmasherTermsAsync( termsToRemove );
                }

                var licensorsToAdd = settings.LicensingSmasher.KnownLicensers.Where( l => !currentSettings.LicensingSmasher.KnownLicensers.Contains( l ) );
                var licensorsToRemove = currentSettings.LicensingSmasher.KnownLicensers.Where( l => !settings.LicensingSmasher.KnownLicensers.Contains( l ) );

                if(licensorsToAdd.Count() > 0 ) {
                    await dal.UpsertLicensingSmasherLicensorsAsync( 
                        licensorsToAdd.Select(l=>
                        new Models.DAL.LicensingSmasherLicensor {
                            Subreddit = settings.Subreddit,
                            DisplayName = l.Value,
                            LicensorID = l.Key
                        } )
                        );
                }

                if(licensorsToRemove.Count() > 0 ) {
                    await dal.DeleteLicensingSmasherLicensorsAsync(licensorsToRemove.Select( l =>
                         new Models.DAL.LicensingSmasherLicensor {
                             Subreddit = settings.Subreddit,
                             DisplayName = l.Value,
                             LicensorID = l.Key
                         } )
                        );
                }
            }
            if(!CompareSettings(currentSettings.SelfPromotionCombustor, settings.SelfPromotionCombustor ) ) {
                settings.SelfPromotionCombustor.ModifiedBy = modifiedBy;
                settings.SelfPromotionCombustor.LastModified = now;

                await dal.SetSelfPromoSettingsAsync( settings.SelfPromotionCombustor, settings.Subreddit );
            }
            if(!CompareSettings(currentSettings.YouTubeSpamDetector, settings.YouTubeSpamDetector ) ) {
                settings.YouTubeSpamDetector.ModifiedBy = modifiedBy;
                settings.YouTubeSpamDetector.LastModified = now;

                await dal.SetSpamDetectorSettingsAsync( settings.YouTubeSpamDetector, settings.Subreddit );

                //TODO refactor this bullshit to generics as well.
                List<Models.DAL.SpamDetectorModule> spamModules = new List<Models.DAL.SpamDetectorModule>();
                try {
                    spamModules.Add( new Models.DAL.SpamDetectorModule( settings.Subreddit, settings.YouTubeSpamDetector.ChannelAgeThreshold ) );
                    spamModules.Add( new Models.DAL.SpamDetectorModule( settings.Subreddit, settings.YouTubeSpamDetector.CommentCountThreshold ) );
                    spamModules.Add( new Models.DAL.SpamDetectorModule( settings.Subreddit, settings.YouTubeSpamDetector.LicensedChannel ) );
                    spamModules.Add( new Models.DAL.SpamDetectorModule( settings.Subreddit, settings.YouTubeSpamDetector.NegativeVoteRatio ) );
                    spamModules.Add( new Models.DAL.SpamDetectorModule( settings.Subreddit, settings.YouTubeSpamDetector.RedditAccountAgeThreshold ) );
                    spamModules.Add( new Models.DAL.SpamDetectorModule( settings.Subreddit, settings.YouTubeSpamDetector.ViewCountThreshold ) );
                    spamModules.Add( new Models.DAL.SpamDetectorModule( settings.Subreddit, settings.YouTubeSpamDetector.VoteCountThreshold ) );
                    spamModules.Add( new Models.DAL.SpamDetectorModule( settings.Subreddit, settings.YouTubeSpamDetector.ChannelSubscribersThreshold ) );

                    await dal.SetSpamDetectorModuleSettingsAsync( spamModules );
                }
                catch {
                    throw new Exception( "Failed to save SpamDetectorModules" );
                }
            }


        }

        public void PurgeSubSettingsFromCache( string subreddit ) {

            cache.Remove( CACHE_PREFIX + subreddit );

        }

        private async Task<SubredditSettings> GetOrUpdateSettingsAsync( string subreddit, bool returnDefault = false ) {
            object cacheVal;
            if ( !cache.TryGetValue( CACHE_PREFIX + subreddit, out cacheVal ) && cacheVal != null ) {
                var settings = (SubredditSettings) cacheVal;
                if ( returnDefault ) {
                    if ( settings == null ) {
                        settings = SubredditSettings.GetDefaultSettings();
                        settings.Subreddit = subreddit;
                    }
                    if ( settings.LicensingSmasher == null ) {
                        settings.LicensingSmasher = new LicensingSmasherSettings();
                        settings.LicensingSmasher.SetDefaultSettings();
                    }
                    if ( settings.SelfPromotionCombustor == null ) {
                        settings.SelfPromotionCombustor = new Models.SelfPromotionCombustorSettings();
                        settings.SelfPromotionCombustor.SetDefaultSettings();
                    }
                    if ( settings.YouTubeSpamDetector == null ) {
                        settings.YouTubeSpamDetector = new YouTubeSpamDetectorSettings();
                        settings.YouTubeSpamDetector.SetDefaultSettings();
                    }
                }
                return settings;
            }
            else {
                var settings = await dal.GetSubredditSettingsAsync( subreddit );
                if ( returnDefault ) {
                    if ( settings == null ) {
                        settings = SubredditSettings.GetDefaultSettings();
                        settings.Subreddit = subreddit;
                    }
                    if ( settings.LicensingSmasher == null ) {
                        settings.LicensingSmasher = new LicensingSmasherSettings();
                        settings.LicensingSmasher.SetDefaultSettings();
                    }
                    if ( settings.SelfPromotionCombustor == null ) {
                        settings.SelfPromotionCombustor = new Models.SelfPromotionCombustorSettings();
                        settings.SelfPromotionCombustor.SetDefaultSettings();
                    }
                    if ( settings.YouTubeSpamDetector == null ) {
                        settings.YouTubeSpamDetector = new YouTubeSpamDetectorSettings();
                        settings.YouTubeSpamDetector.SetDefaultSettings();
                    }
                }
                cache.Set( CACHE_PREFIX + subreddit, settings, DateTimeOffset.Now.AddMinutes( 30 ) );
                return settings;
            }
        }

        private bool CompareSettings( SubredditSettings current, SubredditSettings newSettings ) {
            if ( current == null && newSettings == null ) {
                return true;
            }
            if ( ( current == null && newSettings != null ) || ( current != null && newSettings == null ) ) {
                return false;
            }
            if ( current.RemoveScoreThreshold == newSettings.RemoveScoreThreshold &&
                current.ReportScoreThreshold == newSettings.ReportScoreThreshold ) {

                return true;
            }

            return false;
        }


        //TODO all this bullshit should really be refactored to live on the IModuleSettings interface and implementations
        private bool CompareSettings( LicensingSmasherSettings current, LicensingSmasherSettings newSettings ) {
            if ( current == null && newSettings == null ) {
                return true;
            }
            if((current == null && newSettings != null) || (current != null && newSettings == null ) ) {
                return false;
            }
            if ( current.Enabled == newSettings.Enabled &&
                CompareSettings( current.RemovalFlair, newSettings.RemovalFlair ) &&
                current.ScoreMultiplier == newSettings.ScoreMultiplier &&
                current.KnownLicensers.DictionaryEqual( newSettings.KnownLicensers ) &&
                Enumerable.SequenceEqual( current.MatchTerms, newSettings.MatchTerms ) ) {

                return true;
            }

            return false;
        }

        private bool CompareSettings( SelfPromotionCombustorSettings current, SelfPromotionCombustorSettings newSettings ) {
            if ( current == null && newSettings == null ) {
                return true;
            }
            if ( ( current == null && newSettings != null ) || ( current != null && newSettings == null ) ) {
                return false;
            }
            if ( current.Enabled == newSettings.Enabled &&
                current.GracePeriod == newSettings.GracePeriod &&
                current.IncludePostInPercentage == newSettings.IncludePostInPercentage &&
                current.PercentageThreshold == newSettings.PercentageThreshold &&
                CompareSettings( current.RemovalFlair, newSettings.RemovalFlair ) &&
                current.ScoreMultiplier == newSettings.ScoreMultiplier ) {
                return true;
            }
            return false;
        }

        private bool CompareSettings( YouTubeSpamDetectorSettings current, YouTubeSpamDetectorSettings newSettings ) {
            if ( current == null && newSettings == null ) {
                return true;
            }
            if ( ( current == null && newSettings != null ) || ( current != null && newSettings == null ) ) {
                return false;
            }
            if (current.Enabled == newSettings.Enabled &&
                CompareSettings(current.RemovalFlair, newSettings.RemovalFlair) &&
                current.ScoreMultiplier == newSettings.ScoreMultiplier &&
                CompareSettings(current.ChannelAgeThreshold, newSettings.ChannelAgeThreshold) &&
                CompareSettings(current.ChannelSubscribersThreshold, newSettings.ChannelSubscribersThreshold)&&
                CompareSettings(current.CommentCountThreshold, newSettings.CommentCountThreshold)&&
                CompareSettings(current.LicensedChannel, newSettings.LicensedChannel)&&
                CompareSettings(current.NegativeVoteRatio, newSettings.NegativeVoteRatio)&&
                CompareSettings(current.RedditAccountAgeThreshold, newSettings.RedditAccountAgeThreshold)&&
                CompareSettings(current.ViewCountThreshold, newSettings.ViewCountThreshold)&&
                CompareSettings(current.VoteCountThreshold, newSettings.VoteCountThreshold)) {

                return true;
            }
            return false;
        }

        private bool CompareSettings( YouTubeSpamDetectorModule current, YouTubeSpamDetectorModule newSettings ) {
            if ( current == null && newSettings == null ) {
                return true;
            }
            if ( ( current == null && newSettings != null ) || ( current != null && newSettings == null ) ) {
                return false;
            }
            if ( current.Enabled == newSettings.Enabled &&
                current.Value == newSettings.Value &&
                current.Weight == newSettings.Weight ) {

                return true;

            }
            return false;
        }

        private bool CompareSettings( Flair current, Flair newSettings ) {
            if ( current == null && newSettings == null ) {
                return true;
            }
            if ( ( current == null && newSettings != null ) || ( current != null && newSettings == null ) ) {
                return false;
            }
            if ( current.Class == newSettings.Class &&
                current.Priority == newSettings.Priority &&
                current.Text == newSettings.Text ) {

                return true;
            }
            return false;
        }

    }
}
