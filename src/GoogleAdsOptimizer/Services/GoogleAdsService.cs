using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Ads.Gax.Config;
using Google.Ads.GoogleAds.Config;
using Google.Ads.GoogleAds.Lib;
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

        public async Task InitializeAsync(string clientId, string clientSecret, string refreshToken, string developerToken, string customerId)
        {
            var config = new GoogleAdsConfig
            {
                OAuth2ClientId = clientId,
                OAuth2ClientSecret = clientSecret,
                OAuth2RefreshToken = refreshToken,
                OAuth2Mode = OAuth2Flow.APPLICATION,
                DeveloperToken = developerToken,
                LoginCustomerId = customerId.Replace("-", "")
            };

            _client = new GoogleAdsClient(config);
            _customerId = customerId.Replace("-", "");

            await TestConnectionAsync();
        }

        private async Task<List<GoogleAdsRow>> RunQueryAsync(string query)
        {
            var service = (GoogleAdsServiceClient)_client.GetService(Google.Ads.GoogleAds.Services.V17.GoogleAdsService);
            var request = new SearchGoogleAdsRequest
            {
                CustomerId = _customerId,
                Query = query
            };

            var pageable = service.SearchAsync(request);
            var rows = new List<GoogleAdsRow>();
            await foreach (var row in pageable)
            {
                rows.Add(row);
            }
            return rows;
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                await RunQueryAsync("SELECT customer.id FROM customer LIMIT 1");
            }
            catch (Exception ex)
            {
                throw new Exception($"Google Ads connection failed: {ex.Message}", ex);
            }
        }

        private static DateTime? ParseProtoDate(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return DateTime.TryParseExact(value, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var d) ? d : (DateTime?)null;
        }

        private static T ParseEnum<T>(object rawValue) where T : struct, Enum
        {
            return Enum.TryParse<T>(rawValue?.ToString(), true, out var parsed) ? parsed : default;
        }

        public async Task<List<CampaignExportData>> GetCampaignsWithMetricsAsync(DateTime startDate, DateTime endDate)
        {
            var query = $@"
                SELECT
                    campaign.id,
                    campaign.name,
                    campaign.status,
                    campaign.advertising_channel_type,
                    campaign.start_date,
                    campaign.end_date,
                    campaign_budget.amount_micros,
                    metrics.impressions,
                    metrics.clicks,
                    metrics.cost_micros,
                    metrics.conversions,
                    metrics.conversions_value,
                    metrics.ctr,
                    metrics.cost_per_conversion,
                    metrics.value_per_conversion
                FROM campaign
                WHERE
                    segments.date BETWEEN '{startDate:yyyy-MM-dd}' AND '{endDate:yyyy-MM-dd}'
                ORDER BY metrics.cost_micros DESC";

            var campaigns = new List<CampaignExportData>();
            foreach (var row in await RunQueryAsync(query))
            {
                var campaign = new CampaignExportData
                {
                    Id = row.Campaign.Id.ToString(),
                    Name = row.Campaign.Name,
                    Status = ParseEnum<CampaignStatus>(row.Campaign.Status),
                    AdvertisingChannelType = ParseEnum<AdvertisingChannelType>(row.Campaign.AdvertisingChannelType),
                    StartDate = ParseProtoDate(row.Campaign.StartDate),
                    EndDate = ParseProtoDate(row.Campaign.EndDate),
                    DailyBudget = row.CampaignBudget?.AmountMicros / 1_000_000.0,

                    Impressions = row.Metrics.Impressions,
                    Clicks = row.Metrics.Clicks,
                    Cost = row.Metrics.CostMicros / 1_000_000.0,
                    Conversions = row.Metrics.Conversions,
                    CostPerConversion = row.Metrics.CostPerConversion / 1_000_000.0,
                    ClickThroughRate = row.Metrics.Ctr * 100,
                    ConversionValue = row.Metrics.ConversionsValue / 1_000_000.0,
                    ValuePerConversion = row.Metrics.ValuePerConversion
                };

                if (campaign.Conversions > 0 && campaign.Cost > 0)
                {
                    campaign.Roi = ((campaign.ConversionValue - campaign.Cost) / campaign.Cost) * 100;
                }

                campaigns.Add(campaign);
            }

            return campaigns;
        }

        public async Task<List<AdGroupExportData>> GetAdGroupsWithMetricsAsync(string campaignId, DateTime startDate, DateTime endDate)
        {
            var query = $@"
                SELECT
                    ad_group.id,
                    ad_group.name,
                    ad_group.status,
                    ad_group.type,
                    metrics.impressions,
                    metrics.clicks,
                    metrics.cost_micros,
                    metrics.conversions,
                    metrics.ctr,
                    metrics.cost_per_conversion
                FROM ad_group
                WHERE
                    campaign.id = {campaignId}
                    AND segments.date BETWEEN '{startDate:yyyy-MM-dd}' AND '{endDate:yyyy-MM-dd}'
                ORDER BY metrics.cost_micros DESC";

            var adGroups = new List<AdGroupExportData>();
            foreach (var row in await RunQueryAsync(query))
            {
                var adGroup = new AdGroupExportData
                {
                    Id = row.AdGroup.Id.ToString(),
                    Name = row.AdGroup.Name,
                    Status = ParseEnum<CampaignStatus>(row.AdGroup.Status),
                    AdGroupType = ParseEnum<AdGroupType>(row.AdGroup.Type),
                    CampaignId = campaignId,

                    Impressions = row.Metrics.Impressions,
                    Clicks = row.Metrics.Clicks,
                    Cost = row.Metrics.CostMicros / 1_000_000.0,
                    Conversions = row.Metrics.Conversions,
                    ClickThroughRate = row.Metrics.Ctr * 100,
                    CostPerConversion = row.Metrics.CostPerConversion / 1_000_000.0
                };

                if (adGroup.Conversions > 0 && adGroup.Cost > 0)
                {
                    adGroup.Roi = ((adGroup.Cost / adGroup.Conversions) / adGroup.Cost) * 100;
                }

                adGroups.Add(adGroup);
            }

            return adGroups;
        }

        public async Task<List<TextAdExportData>> GetAdsWithMetricsAsync(string campaignId, DateTime startDate, DateTime endDate)
        {
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
                    metrics.ctr,
                    metrics.cost_per_conversion
                FROM ad_group_ad
                WHERE
                    campaign.id = {campaignId}
                    AND ad_group_ad.ad.type = EXPANDED_TEXT_AD
                    AND segments.date BETWEEN '{startDate:yyyy-MM-dd}' AND '{endDate:yyyy-MM-dd}'
                ORDER BY metrics.impressions DESC";

            var ads = new List<TextAdExportData>();
            foreach (var row in await RunQueryAsync(query))
            {
                var expandedAd = row.AdGroupAd.Ad.ExpandedTextAd;

                var ad = new TextAdExportData
                {
                    Id = row.AdGroupAd.Ad.Id.ToString(),
                    Name = row.AdGroup.Name,
                    CampaignName = row.Campaign.Name,
                    Status = ParseEnum<CampaignStatus>(row.AdGroupAd.Status),
                    Headline1 = expandedAd.HeadlinePart1,
                    Headline2 = expandedAd.HeadlinePart2,
                    Headline3 = expandedAd.HeadlinePart3,
                    Description = expandedAd.Description,
                    Description2 = expandedAd.Description2,
                    FinalUrl = row.AdGroupAd.Ad.FinalUrls?.FirstOrDefault() ?? "",

                    Impressions = row.Metrics.Impressions,
                    Clicks = row.Metrics.Clicks,
                    Cost = row.Metrics.CostMicros / 1_000_000.0,
                    Conversions = row.Metrics.Conversions,
                    ClickThroughRate = row.Metrics.Ctr * 100,
                    CostPerConversion = row.Metrics.CostPerConversion / 1_000_000.0
                };

                if (ad.Impressions > 0)
                {
                    ad.PerformanceScore = CalculateAdPerformanceScore(ad);
                }

                ads.Add(ad);
            }

            return ads;
        }

        public async Task<List<KeywordExportData>> GetKeywordsWithMetricsAsync(string campaignId, DateTime startDate, DateTime endDate)
        {
            var query = $@"
                SELECT
                    ad_group_criterion.criterion_id,
                    ad_group_criterion.keyword.text,
                    ad_group_criterion.keyword.match_type,
                    ad_group_criterion.status,
                    ad_group_criterion.quality_info.quality_score,
                    metrics.impressions,
                    metrics.clicks,
                    metrics.cost_micros,
                    metrics.conversions,
                    metrics.ctr,
                    metrics.cost_per_conversion
                FROM ad_group_criterion
                WHERE
                    campaign.id = {campaignId}
                    AND ad_group_criterion.type = KEYWORD
                    AND segments.date BETWEEN '{startDate:yyyy-MM-dd}' AND '{endDate:yyyy-MM-dd}'
                ORDER BY metrics.impressions DESC";

            var keywords = new List<KeywordExportData>();
            foreach (var row in await RunQueryAsync(query))
            {
                var keyword = new KeywordExportData
                {
                    Id = row.AdGroupCriterion.CriterionId.ToString(),
                    Text = row.AdGroupCriterion.Keyword.Text,
                    MatchType = ParseEnum<KeywordMatchType>(row.AdGroupCriterion.Keyword.MatchType),
                    Status = ParseEnum<CampaignStatus>(row.AdGroupCriterion.Status),
                    QualityScore = (int)(row.AdGroupCriterion.QualityInfo?.QualityScore ?? 0),

                    Impressions = row.Metrics.Impressions,
                    Clicks = row.Metrics.Clicks,
                    Cost = row.Metrics.CostMicros / 1_000_000.0,
                    Conversions = row.Metrics.Conversions,
                    ClickThroughRate = row.Metrics.Ctr * 100,
                    CostPerConversion = row.Metrics.CostPerConversion / 1_000_000.0
                };

                if (keyword.Impressions > 0)
                {
                    keyword.EffectivenessScore = CalculateKeywordEffectiveness(keyword);
                }

                keywords.Add(keyword);
            }

            return keywords;
        }

        private double CalculateAdPerformanceScore(TextAdExportData ad)
        {
            var score = 50.0;

            if (ad.ClickThroughRate > 5.0) score += 20;
            else if (ad.ClickThroughRate > 3.0) score += 15;
            else if (ad.ClickThroughRate > 1.0) score += 10;
            else if (ad.ClickThroughRate < 0.5) score -= 15;

            var conversionRate = ad.Impressions > 0 ? (ad.Conversions / ad.Impressions) * 100 : 0;
            if (conversionRate > 5.0) score += 20;
            else if (conversionRate > 2.0) score += 15;
            else if (conversionRate > 1.0) score += 10;
            else if (conversionRate < 0.5) score -= 10;

            if (ad.CostPerConversion > 0 && ad.CostPerConversion < 10.0) score += 10;
            else if (ad.CostPerConversion > 50.0) score -= 15;

            return Math.Max(0, Math.Min(100, score));
        }

        private double CalculateKeywordEffectiveness(KeywordExportData keyword)
        {
            var score = 50.0;

            if (keyword.QualityScore >= 8) score += 20;
            else if (keyword.QualityScore >= 6) score += 10;
            else if (keyword.QualityScore <= 3) score -= 15;

            if (keyword.ClickThroughRate > 5.0) score += 15;
            else if (keyword.ClickThroughRate > 2.0) score += 10;
            else if (keyword.ClickThroughRate < 1.0) score -= 10;

            if (keyword.Conversions > 10) score += 15;
            else if (keyword.Conversions > 5) score += 10;
            else if (keyword.Conversions == 0 && keyword.Impressions > 100) score -= 10;

            return Math.Max(0, Math.Min(100, score));
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _client = null;
                _isDisposed = true;
            }
        }
    }
}
