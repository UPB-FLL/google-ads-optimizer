using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Ads.GoogleAds.Lib;
using Google.Ads.GoogleAds.V17;
using Google.Ads.GoogleAds.V17.Common;
using Google.Ads.GoogleAds.V17.Enums;
using Google.Ads.GoogleAds.V17.Resources;
using Google.Ads.GoogleAds.V17.Services;
using GoogleAdsOptimizer.Models;

namespace GoogleAdsOptimizer.Services
{
    /// <summary>
    /// Main service for interacting with Google Ads API
    /// </summary>
    public class GoogleAdsService : IDisposable
    {
        private GoogleAdsClient _client;
        private string _customerId;
        private bool _isDisposed;

        /// <summary>
        /// Initialize the Google Ads client with OAuth credentials
        /// </summary>
        public async Task InitializeAsync(string clientId, string clientSecret, string refreshToken, string developerToken, string customerId)
        {
            var config = new GoogleAdsConfig
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                RefreshToken = refreshToken,
                DeveloperToken = developerToken,
                LoginCustomerId = customerId
            };

            _client = new GoogleAdsClient(config);
            _customerId = customerId;

            // Test connection
            await TestConnectionAsync();
        }

        /// <summary>
        /// Test the connection to Google Ads API
        /// </summary>
        private async Task TestConnectionAsync()
        {
            try
            {
                var service = _client.GetService(CustomerService.Name);
                var request = new GetCustomerRequest
                {
                    ResourceName = ResourceNames.Customer(_customerId)
                };

                var customer = await service.GetCustomerAsync(request);

                if (customer == null)
                {
                    throw new Exception("Unable to connect to Google Ads API");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Google Ads connection failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieve all campaigns with performance metrics
        /// </summary>
        public async Task<List<CampaignExportData>> GetCampaignsWithMetricsAsync(DateTime startDate, DateTime endDate)
        {
            var service = _client.GetService(GoogleAdsServiceClient.Name);
            var dateFormat = "yyyy-MM-dd";

            var query = $@"
                SELECT
                    campaign.id,
                    campaign.name,
                    campaign.status,
                    campaign.advertising_channel_type,
                    campaign.start_date,
                    campaign.end_date,
                    campaign.daily_budget,
                    campaign.bidding_strategy_type,
                    campaign.target_cpa,
                    campaign.target_roas,
                    campaign.campaign_trial_type,
                    metrics.impressions,
                    metrics.clicks,
                    metrics.cost_micros,
                    metrics.conversions,
                    metrics.cost_per_conversion,
                    metrics.click_through_rate,
                    metrics.conversion_value,
                    metrics.value_per_conversion
                FROM campaign
                WHERE
                    segments.date DURING {startDate.ToString(dateFormat)}, {endDate.ToString(dateFormat)}
                ORDER BY metrics.cost_micros DESC";

            var response = await service.SearchAsync(_customerId, query);
            var campaigns = new List<CampaignData>();

            foreach (var row in response)
            {
                var campaign = new CampaignExportData
                {
                    Id = row.Campaign.Id,
                    Name = row.Campaign.Name,
                    Status = row.Campaign.Status,
                    AdvertisingChannelType = row.Campaign.AdvertisingChannelType,
                    StartDate = row.Campaign.StartDate,
                    EndDate = row.Campaign.EndDate,
                    DailyBudget = row.Campaign.DailyBudgetMicros / 1_000_000.0,
                    BiddingStrategyType = row.Campaign.BiddingStrategyType.ToString(),
                    TargetCpa = row.Campaign.TargetCpa?.Micros / 1_000_000.0,
                    TargetRoas = row.Campaign.TargetRoas,
                    CampaignType = row.Campaign.CampaignTrialType.ToString(),

                    // Metrics
                    Impressions = row.Metrics.Impressions,
                    Clicks = row.Metrics.Clicks,
                    Cost = row.Metrics.CostMicros / 1_000_000.0,
                    Conversions = row.Metrics.Conversions,
                    CostPerConversion = row.Metrics.CostPerConversion / 1_000_000.0,
                    ClickThroughRate = row.Metrics.ClickThroughRate * 100,
                    ConversionValue = row.Metrics.ConversionValue / 1_000_000.0,
                    ValuePerConversion = row.Metrics.ValuePerConversion / 1_000_000.0
                };

                // Calculate ROI
                if (campaign.Conversions > 0)
                {
                    campaign.Roi = ((campaign.ConversionValue - campaign.Cost) / campaign.Cost) * 100;
                }

                campaigns.Add(campaign);
            }

            return campaigns;
        }

        /// <summary>
        /// Get ad groups with performance data
        /// </summary>
        public async Task<List<AdGroupExportData>> GetAdGroupsWithMetricsAsync(string campaignId, DateTime startDate, DateTime endDate)
        {
            var service = _client.GetService(GoogleAdsServiceClient.Name);
            var dateFormat = "yyyy-MM-dd";

            var query = $@"
                SELECT
                    ad_group.id,
                    ad_group.name,
                    ad_group.status,
                    ad_group.type,
                    ad_group.cpc_bid_micros,
                    ad_group.cpa_bid_micros,
                    ad_group.roas_bid,
                    ad_group.target_cpa_micros,
                    metrics.impressions,
                    metrics.clicks,
                    metrics.cost_micros,
                    metrics.conversions,
                    metrics.click_through_rate,
                    metrics.cost_per_conversion
                FROM ad_group
                WHERE
                    campaign.id = {campaignId}
                    AND segments.date DURING {startDate.ToString(dateFormat)}, {endDate.ToString(dateFormat)}
                ORDER BY metrics.cost_micros DESC";

            var response = await service.SearchAsync(_customerId, query);
            var adGroups = new List<AdGroupData>();

            foreach (var row in response)
            {
                var adGroup = new AdGroupExportData
                {
                    Id = row.AdGroup.Id,
                    Name = row.AdGroup.Name,
                    Status = row.AdGroup.Status,
                    AdGroupType = row.AdGroup.Type,
                    CampaignId = campaignId,
                    DefaultBid = row.AdGroup.CpcBidMicros / 1_000_000.0,
                    CpaBid = row.AdGroup.CpaBidMicros / 1_000_000.0,
                    RoasBid = row.AdGroup.RoasBid,
                    TargetCpa = row.AdGroup.TargetCpaMicros / 1_000_000.0,

                    // Metrics
                    Impressions = row.Metrics.Impressions,
                    Clicks = row.Metrics.Clicks,
                    Cost = row.Metrics.CostMicros / 1_000_000.0,
                    Conversions = row.Metrics.Conversions,
                    ClickThroughRate = row.Metrics.ClickThroughRate * 100,
                    CostPerConversion = row.Metrics.CostPerConversion / 1_000_000.0
                };

                if (adGroup.Conversions > 0)
                {
                    adGroup.Roi = ((adGroup.Cost / adGroup.Conversions) / adGroup.Cost) * 100;
                }

                adGroups.Add(adGroup);
            }

            return adGroups;
        }

        /// <summary>
        /// Get ads with performance data for analysis
        /// </summary>
        public async Task<List<TextAdExportData>> GetAdsWithMetricsAsync(string campaignId, DateTime startDate, DateTime endDate)
        {
            var service = _client.GetService(GoogleAdsServiceClient.Name);
            var dateFormat = "yyyy-MM-dd";

            var query = $@"
                SELECT
                    ad_group_ad.ad.id,
                    ad_group_ad.ad.type,
                    ad_group_ad.status,
                    ad_group_ad.policy_summary.approval_status,
                    ad_group_ad.ad.expanded_text_ad.headline_part1,
                    ad_group_ad.ad.expanded_text_ad.headline_part2,
                    ad_group_ad.ad.expanded_text_ad.headline_part3,
                    ad_group_ad.ad.expanded_text_ad.description,
                    ad_group_ad.ad.expanded_text_ad.description2,
                    ad_group_ad.ad.final_urls,
                    ad_group.name,
                    campaign.name,
                    metrics.impressions,
                    metrics.clicks,
                    metrics.cost_micros,
                    metrics.conversions,
                    metrics.click_through_rate,
                    metrics.ctr,
                    metrics.cost_per_conversion
                FROM ad_group_ad
                WHERE
                    campaign.id = {campaignId}
                    AND ad_group_ad.ad.type = EXPANDED_TEXT_AD
                    AND segments.date DURING {startDate.ToString(dateFormat)}, {endDate.ToString(dateFormat)}
                ORDER BY metrics.impressions DESC";

            var response = await service.SearchAsync(_customerId, query);
            var ads = new List<TextAdData>();

            foreach (var row in response)
            {
                var expandedAd = row.AdGroupAd.Ad.ExpandedTextAd;

                var ad = new TextAdExportData
                {
                    Id = row.AdGroupAd.Id,
                    Name = row.AdGroup.Name,
                    CampaignName = row.Campaign.Name,
                    Status = row.AdGroupAd.Status,
                    ApprovalStatus = row.AdGroupAd.PolicySummary.ApprovalStatus,
                    Headline1 = expandedAd.HeadlinePart1,
                    Headline2 = expandedAd.HeadlinePart2,
                    Headline3 = expandedAd.HeadlinePart3,
                    Description = expandedAd.Description,
                    Description2 = expandedAd.Description2,
                    FinalUrl = expandedAd.FinalUrls?.FirstOrDefault() ?? "",

                    // Metrics
                    Impressions = row.Metrics.Impressions,
                    Clicks = row.Metrics.Clicks,
                    Cost = row.Metrics.CostMicros / 1_000_000.0,
                    Conversions = row.Metrics.Conversions,
                    ClickThroughRate = row.Metrics.Ctr * 100,
                    CostPerConversion = row.Metrics.CostPerConversion / 1_000_000.0
                };

                // Calculate performance score
                if (ad.Impressions > 0)
                {
                    ad.PerformanceScore = CalculateAdPerformanceScore(ad);
                }

                ads.Add(ad);
            }

            return ads;
        }

        /// <summary>
        /// Get keywords with performance data
        /// </summary>
        public async Task<List<KeywordExportData>> GetKeywordsWithMetricsAsync(string campaignId, DateTime startDate, DateTime endDate)
        {
            var service = _client.GetService(GoogleAdsServiceClient.Name);
            var dateFormat = "yyyy-MM-dd";

            var query = $@"
                SELECT
                    ad_group_criterion.criterion_id,
                    ad_group_criterion.keyword.text,
                    ad_group_criterion.keyword.match_type,
                    ad_group_criterion.status,
                    ad_group_criterion.bidding.strategy_cpc_bid_micros,
                    ad_group_criterion.quality_info.quality_score,
                    metrics.impressions,
                    metrics.clicks,
                    metrics.cost_micros,
                    metrics.conversions,
                    metrics.click_through_rate,
                    metrics.cost_per_conversion
                FROM ad_group_criterion
                WHERE
                    campaign.id = {campaignId}
                    AND ad_group_criterion.type = KEYWORD
                    AND segments.date DURING {startDate.ToString(dateFormat)}, {endDate.ToString(dateFormat)}
                ORDER BY metrics.impressions DESC";

            var response = await service.SearchAsync(_customerId, query);
            var keywords = new List<KeywordData>();

            foreach (var row in response)
            {
                var keyword = new KeywordExportData
                {
                    Id = row.AdGroupCriterion.CriterionId,
                    Text = row.AdGroupCriterion.Keyword.Text,
                    MatchType = row.AdGroupCriterion.Keyword.MatchType,
                    Status = row.AdGroupCriterion.Status,
                    CpcBid = row.AdGroupCriterion.Bidding.StrategyCpcBidMicros / 1_000_000.0,
                    QualityScore = row.AdGroupCriterion.QualityInfo.QualityScore ?? 0,

                    // Metrics
                    Impressions = row.Metrics.Impressions,
                    Clicks = row.Metrics.Clicks,
                    Cost = row.Metrics.CostMicros / 1_000_000.0,
                    Conversions = row.Metrics.Conversions,
                    ClickThroughRate = row.Metrics.ClickThroughRate * 100,
                    CostPerConversion = row.Metrics.CostPerConversion / 1_000_000.0
                };

                // Calculate keyword effectiveness
                if (keyword.Impressions > 0)
                {
                    keyword.EffectivenessScore = CalculateKeywordEffectiveness(keyword);
                }

                keywords.Add(keyword);
            }

            return keywords;
        }

        /// <summary>
        /// Calculate a performance score for an ad (0-100)
        /// </summary>
        private double CalculateAdPerformanceScore(TextAdExportData ad)
        {
            var score = 50.0; // Base score

            // Click-through rate impact (high impact)
            if (ad.ClickThroughRate > 5.0) score += 20;
            else if (ad.ClickThroughRate > 3.0) score += 15;
            else if (ad.ClickThroughRate > 1.0) score += 10;
            else if (ad.ClickThroughRate < 0.5) score -= 15;

            // Conversion rate impact
            var conversionRate = ad.Impressions > 0 ? (ad.Conversions / ad.Impressions) * 100 : 0;
            if (conversionRate > 5.0) score += 20;
            else if (conversionRate > 2.0) score += 15;
            else if (conversionRate > 1.0) score += 10;
            else if (conversionRate < 0.5) score -= 10;

            // Cost efficiency
            if (ad.CostPerConversion > 0 && ad.CostPerConversion < 10.0) score += 10;
            else if (ad.CostPerConversion > 50.0) score -= 15;

            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// Calculate effectiveness score for a keyword (0-100)
        /// </summary>
        private double CalculateKeywordEffectiveness(KeywordExportData keyword)
        {
            var score = 50.0;

            // Quality score impact
            if (keyword.QualityScore >= 8) score += 20;
            else if (keyword.QualityScore >= 6) score += 10;
            else if (keyword.QualityScore <= 3) score -= 15;

            // Click-through rate
            if (keyword.ClickThroughRate > 5.0) score += 15;
            else if (keyword.ClickThroughRate > 2.0) score += 10;
            else if (keyword.ClickThroughRate < 1.0) score -= 10;

            // Conversion performance
            if (keyword.Conversions > 10) score += 15;
            else if (keyword.Conversions > 5) score += 10;
            else if (keyword.Conversions == 0 && keyword.Impressions > 100) score -= 10;

            return Math.Max(0, Math.Min(100, score));
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _client?.Dispose();
                _isDisposed = true;
            }
        }
    }
}