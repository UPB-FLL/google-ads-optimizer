using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleAdsOptimizer.Models
{
    // Data transfer objects and models used across the application

    public class ConnectionStatus
    {
        public bool IsConnected { get; set; }
        public string ServiceName { get; set; }
        public DateTime LastConnected { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ExportOptions
    {
        public string OutputDirectory { get; set; }
        public bool IncludeImages { get; set; }
        public bool IncludeKeywords { get; set; }
        public bool IncludeAdGroups { get; set; }
        public List<string> SelectedCampaigns { get; set; } = new List<string>();
    }

    public class AnalysisRequest
    {
        public string CampaignId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IncludeKeywords { get; set; } = true;
        public bool IncludeAds { get; set; } = true;
        public bool IncludeAdGroups { get; set; } = true;
    }

    public class GenerationOptions
    {
        public int NumberOfVariations { get; set; } = 3;
        public string Tone { get; set; } = "Professional";
        public bool IncludeEmojis { get; set; } = false;
        public int MaxHeadlineLength { get; set; } = 30;
        public int MaxDescriptionLength { get; set; } = 90;
    }

    public class PerformanceMetrics
    {
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Cost { get; set; }
        public double Conversions { get; set; }
        public double Revenue { get; set; }
        public double CTR => Impressions > 0 ? (Clicks / (double)Impressions) * 100 : 0;
        public double CPC => Clicks > 0 ? Cost / Clicks : 0;
        public double CPA => Conversions > 0 ? Cost / Conversions : 0;
        public double ROI => Cost > 0 ? ((Revenue - Cost) / Cost) * 100 : 0;
        public double ROAS => Cost > 0 ? Revenue / Cost : 0;
        public double ConversionRate => Impressions > 0 ? (Conversions / Impressions) * 100 : 0;
    }

    public class CampaignComparison
    {
        public string CampaignAName { get; set; }
        public string CampaignBName { get; set; }
        public PerformanceMetrics MetricsA { get; set; }
        public PerformanceMetrics MetricsB { get; set; }
        public string BetterCampaign => MetricsB.ROAS > MetricsA.ROAS ? CampaignBName : CampaignAName;
        public double ROASDifference => Math.Abs(MetricsA.ROAS - MetricsB.ROAS);
    }

    public class AdVariation
    {
        public string OriginalAdId { get; set; }
        public string OriginalHeadline { get; set; }
        public List<string> SuggestedHeadlines { get; set; } = new List<string>();
        public List<string> SuggestedDescriptions { get; set; } = new List<string>();
        public string ReasonForVariation { get; set; }
        public double ExpectedImprovement { get; set; }
    }

    public class BudgetRecommendation
    {
        public string CampaignName { get; set; }
        public double CurrentBudget { get; set; }
        public double RecommendedBudget { get; set; }
        public string Reasoning { get; set; }
        public double ExpectedROIImpact { get; set; }
        public string Priority { get; set; } // "High", "Medium", "Low"
    }

    public class KeywordOpportunity
    {
        public string KeywordText { get; set; }
        public string MatchType { get; set; }
        public double EstimatedVolume { get; set; }
        public double EstimatedCPC { get; set; }
        public double EstimatedCompetition { get; set; }
        public string OpportunityScore { get; set; }
        public string Recommendation { get; set; }
    }

    public class CompetitorInsight
    {
        public string CompetitorName { get; set; }
        public List<string> TopKeywords { get; set; } = new List<string>();
        public List<string> AdCopyThemes { get; set; } = new List<string>();
        public double EstimatedMonthlySpend { get; set; }
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> Weaknesses { get; set; } = new List<string>();
    }

    public class OptimizationReport
    {
        public DateTime GeneratedDate { get; set; }
        public string AccountId { get; set; }
        public int TotalCampaignsAnalyzed { get; set; }
        public List<string> TopPerformingCampaigns { get; set; } = new List<string>();
        public List<string> CampaignsNeedingAttention { get; set; } = new List<string>();
        public double TotalPotentialImprovement { get; set; }
        public List<string> KeyRecommendations { get; set; } = new List<string>();
        public string ExecutiveSummary { get; set; }
    }

    public class ImageAsset
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string MimeType { get; set; }
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsApproved { get; set; }
    }

    public class ImageManifest
    {
        public List<ImageAsset> Images { get; set; } = new List<ImageAsset>();
        public string OutputDirectory { get; set; }
        public DateTime GeneratedDate { get; set; }
        public int TotalImages => Images.Count;
        public long TotalSizeBytes => Images.Sum(img => img.FileSize);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Suggestions { get; set; } = new List<string>();
    }

    public class AdPreview
    {
        public string Headline1 { get; set; }
        public string Headline2 { get; set; }
        public string Headline3 { get; set; }
        public string Description { get; set; }
        public string Description2 { get; set; }
        public string DisplayUrl { get; set; }
        public string FinalUrl { get; set; }

        public string GetPlainText()
        {
            return $"{Headline1} | {Headline2} | {Headline3}\n{Description} {Description2}";
        }

        public ValidationResult Validate()
        {
            var result = new ValidationResult { IsValid = true };

            // Check character limits
            if (Headline1?.Length > 30)
            {
                result.Errors.Add("Headline 1 exceeds 30 characters");
                result.IsValid = false;
            }

            if (Headline2?.Length > 30)
            {
                result.Errors.Add("Headline 2 exceeds 30 characters");
                result.IsValid = false;
            }

            if (Headline3?.Length > 30)
            {
                result.Errors.Add("Headline 3 exceeds 30 characters");
                result.IsValid = false;
            }

            if (Description?.Length > 90)
            {
                result.Errors.Add("Description exceeds 90 characters");
                result.IsValid = false;
            }

            if (Description2?.Length > 90)
            {
                result.Errors.Add("Description 2 exceeds 90 characters");
                result.IsValid = false;
            }

            // Check required fields
            if (string.IsNullOrWhiteSpace(Headline1))
            {
                result.Errors.Add("Headline 1 is required");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(Headline2))
            {
                result.Errors.Add("Headline 2 is required");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                result.Errors.Add("Description is required");
                result.IsValid = false;
            }

            return result;
        }
    }

    // Additional data models for Google Ads export functionality
    public class CampaignExportData
    {
        public string Name { get; set; }
        public CampaignStatus Status { get; set; }
        public double? DailyBudget { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public AdvertisingChannelType AdvertisingChannelType { get; set; }
        public double? TargetCpa { get; set; }
        public double? TargetRoas { get; set; }
        public string BiddingStrategyType { get; set; }
        public CampaignType CampaignType { get; set; }
        public string Id { get; set; }

        // Performance metrics for internal use
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Cost { get; set; }
        public double Conversions { get; set; }
        public double CostPerConversion { get; set; }
        public double ClickThroughRate { get; set; }
        public double ConversionValue { get; set; }
        public double ValuePerConversion { get; set; }
        public double Roi { get; set; }
    }

    public class AdGroupExportData
    {
        public string CampaignName { get; set; }
        public string Name { get; set; }
        public CampaignStatus Status { get; set; }
        public double? DefaultBid { get; set; }
        public AdGroupType AdGroupType { get; set; }
        public double? CpaBid { get; set; }
        public double? RoasBid { get; set; }
        public string Id { get; set; }
        public string CampaignId { get; set; }

        // Performance metrics for internal use
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Cost { get; set; }
        public double Conversions { get; set; }
        public double ClickThroughRate { get; set; }
        public double CostPerConversion { get; set; }
        public double Roi { get; set; }
    }

    public class TextAdExportData
    {
        public string CampaignName { get; set; }
        public string AdGroupName { get; set; }
        public string Name { get; set; }
        public CampaignStatus Status { get; set; }
        public string Headline1 { get; set; }
        public string Headline2 { get; set; }
        public string Headline3 { get; set; }
        public string Description { get; set; }
        public string Description2 { get; set; }
        public string DisplayUrl { get; set; }
        public string FinalUrl { get; set; }
        public string TrackingTemplate { get; set; }
        public object CustomParameters { get; set; }
        public List<string> ImageNames { get; set; } = new List<string>();
        public string Id { get; set; }

        // Performance metrics for internal use
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Cost { get; set; }
        public double Conversions { get; set; }
        public double ClickThroughRate { get; set; }
        public double CostPerConversion { get; set; }
        public double PerformanceScore { get; set; }
    }

    public class KeywordExportData
    {
        public string CampaignName { get; set; }
        public string AdGroupName { get; set; }
        public string Text { get; set; }
        public CampaignStatus Status { get; set; }
        public KeywordMatchType MatchType { get; set; }
        public double? CpcBid { get; set; }
        public double? FirstPageBid { get; set; }
        public int? QualityScore { get; set; }
        public string Id { get; set; }

        // Performance metrics for internal use
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Cost { get; set; }
        public double Conversions { get; set; }
        public double ClickThroughRate { get; set; }
        public double CostPerConversion { get; set; }
        public double EffectivenessScore { get; set; }
    }

    public enum CampaignStatus
    {
        Unspecified,
        Unknown,
        Enabled,
        Paused,
        Removed
    }

    public enum AdvertisingChannelType
    {
        Unspecified,
        Search,
        Display,
        Shopping,
        Video,
        MultiChannel,
        PerformanceMax,
        Discovery,
        Local,
        Smart
    }

    public enum CampaignType
    {
        Standard,
        Advanced,
        DynamicSearchAds,
        ShoppingComparisonListing,
        VideoAction,
        AppPromotion,
        Local
    }

    public enum AdGroupType
    {
        Standard,
        SearchStandard,
        DisplayStandard,
        ShoppingProductShopping,
        ShoppingShowcaseAds,
        ShoppingComparisonListingAds,
        ShoppingSmartAds,
        VideoAction,
        VideoOutstream,
        VideoTrueViewInStream,
        AppCampaign
    }

    public enum KeywordMatchType
    {
        Broad,
        Phrase,
        Exact
    }
}