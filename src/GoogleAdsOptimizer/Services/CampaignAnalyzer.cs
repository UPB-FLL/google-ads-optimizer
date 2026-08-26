using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoogleAdsOptimizer.Models;

namespace GoogleAdsOptimizer.Services
{
    /// <summary>
    /// Service for analyzing campaign performance and generating optimization recommendations
    /// </summary>
    public class CampaignAnalyzer
    {
        private readonly GoogleAdsService _googleAdsService;
        private readonly GPTService _gptService;

        public CampaignAnalyzer(GoogleAdsService googleAdsService, GPTService gptService)
        {
            _googleAdsService = googleAdsService;
            _gptService = gptService;
        }

        /// <summary>
        /// Perform comprehensive campaign analysis
        /// </summary>
        public async Task<ComprehensiveAnalysis> AnalyzeCampaignAsync(string campaignId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get campaign data
                var campaigns = await _googleAdsService.GetCampaignsWithMetricsAsync(startDate, endDate);
                var campaign = campaigns.FirstOrDefault(c => c.Id.ToString() == campaignId);

                if (campaign == null)
                {
                    throw new Exception($"Campaign {campaignId} not found");
                }

                // Get ad groups and ads
                var adGroups = await _googleAdsService.GetAdGroupsWithMetricsAsync(campaignId, startDate, endDate);
                var ads = await _googleAdsService.GetAdsWithMetricsAsync(campaignId, startDate, endDate);
                var keywords = await _googleAdsService.GetKeywordsWithMetricsAsync(campaignId, startDate, endDate);

                // Perform analysis
                var analysis = new ComprehensiveAnalysis
                {
                    Campaign = campaign,
                    AnalysisDate = DateTime.Now,
                    DateRange = new DateRange { Start = startDate, End = endDate }
                };

                // Campaign-level analysis
                analysis.CampaignPerformance = AnalyzeCampaignPerformance(campaign);

                // Ad group analysis
                analysis.AdGroupPerformance = AnalyzeAdGroups(adGroups);

                // Ad analysis
                analysis.AdPerformance = AnalyzeAds(ads);

                // Keyword analysis
                analysis.KeywordPerformance = AnalyzeKeywords(keywords);

                // Generate recommendations
                analysis.Recommendations = GenerateRecommendations(analysis);

                // Get AI-powered insights
                analysis.AIInsights = await GetAIInsights(campaign, ads, startDate, endDate);

                return analysis;
            }
            catch (Exception ex)
            {
                throw new Exception($"Campaign analysis failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Analyze all campaigns in the account
        /// </summary>
        public async Task<AccountAnalysis> AnalyzeAccountAsync(DateTime startDate, DateTime endDate)
        {
            var campaigns = await _googleAdsService.GetCampaignsWithMetricsAsync(startDate, endDate);
            var accountAnalysis = new AccountAnalysis
            {
                AnalysisDate = DateTime.Now,
                DateRange = new DateRange { Start = startDate, End = endDate },
                TotalCampaigns = campaigns.Count,
                TotalSpend = campaigns.Sum(c => c.Cost),
                TotalConversions = campaigns.Sum(c => c.Conversions),
                AverageROI = campaigns.Average(c => c.Roi)
            };

            // Analyze each campaign
            foreach (var campaign in campaigns)
            {
                try
                {
                    var campaignAnalysis = await AnalyzeCampaignAsync(campaign.Id.ToString(), startDate, endDate);
                    accountAnalysis.CampaignAnalyses.Add(campaignAnalysis);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other campaigns
                    accountAnalysis.Errors.Add($"Failed to analyze campaign {campaign.Name}: {ex.Message}");
                }
            }

            // Generate account-level recommendations
            accountAnalysis.Recommendations = GenerateAccountRecommendations(accountAnalysis);

            return accountAnalysis;
        }

        private CampaignPerformanceAnalysis AnalyzeCampaignPerformance(CampaignExportData campaign)
        {
            var analysis = new CampaignPerformanceAnalysis
            {
                CampaignName = campaign.Name,
                OverallScore = CalculateCampaignScore(campaign),
                BudgetUtilization = CalculateBudgetUtilization(campaign),
                ROAScore = CalculateROAScore(campaign),
                ConversionRate = CalculateConversionRate(campaign),
                CostEfficiency = CalculateCostEfficiency(campaign)
            };

            // Determine status
            analysis.Status = analysis.OverallScore >= 70 ? PerformanceStatus.Excellent :
                            analysis.OverallScore >= 50 ? PerformanceStatus.Good :
                            analysis.OverallScore >= 30 ? PerformanceStatus.Fair :
                            PerformanceStatus.Poor;

            return analysis;
        }

        private List<AdGroupPerformance> AnalyzeAdGroups(List<AdGroupExportData> adGroups)
        {
            return adGroups.Select(ag => new AdGroupPerformance
            {
                AdGroupName = ag.Name,
                PerformanceScore = CalculateAdGroupScore(ag),
                Status = DetermineAdGroupStatus(ag),
                Impressions = ag.Impressions,
                Clicks = ag.Clicks,
                Cost = ag.Cost,
                Conversions = ag.Conversions,
                CTR = ag.ClickThroughRate,
                CPA = ag.CostPerConversion,
                Recommendation = GetAdGroupRecommendation(ag)
            }).OrderByDescending(ag => ag.PerformanceScore).ToList();
        }

        private List<AdPerformance> AnalyzeAds(List<TextAdExportData> ads)
        {
            var adPerformance = new List<AdPerformance>();

            foreach (var ad in ads)
            {
                var performance = new AdPerformance
                {
                    AdId = ad.Id,
                    Headline = $"{ad.Headline1} - {ad.Headline2}",
                    Impressions = ad.Impressions,
                    Clicks = ad.Clicks,
                    Conversions = ad.Conversions,
                    CTR = ad.ClickThroughRate,
                    CPA = ad.CostPerConversion,
                    PerformanceScore = ad.PerformanceScore,
                    Status = DetermineAdStatus(ad),
                    ApprovalStatus = ad.Status.ToString()
                };

                // Categorize ad performance
                if (performance.PerformanceScore >= 70)
                {
                    performance.Category = AdCategory.TopPerformer;
                }
                else if (performance.PerformanceScore >= 50)
                {
                    performance.Category = AdCategory.Good;
                }
                else if (performance.PerformanceScore >= 30)
                {
                    performance.Category = AdCategory.NeedsImprovement;
                }
                else
                {
                    performance.Category = AdCategory.Underperforming;
                }

                performance.Suggestion = GetAdSuggestion(performance);
                adPerformance.Add(performance);
            }

            return adPerformance.OrderByDescending(ap => ap.PerformanceScore).ToList();
        }

        private List<KeywordPerformance> AnalyzeKeywords(List<KeywordExportData> keywords)
        {
            return keywords.Select(kw => new KeywordPerformance
            {
                KeywordText = kw.Text,
                MatchType = kw.MatchType.ToString(),
                QualityScore = kw.QualityScore ?? 0,
                EffectivenessScore = kw.EffectivenessScore,
                Impressions = kw.Impressions,
                Clicks = kw.Clicks,
                CTR = kw.ClickThroughRate,
                Conversions = kw.Conversions,
                Cost = kw.Cost,
                Status = DetermineKeywordStatus(kw),
                Recommendation = GetKeywordRecommendation(kw)
            }).OrderByDescending(kp => kp.EffectivenessScore).ToList();
        }

        private List<Recommendation> GenerateRecommendations(ComprehensiveAnalysis analysis)
        {
            var recommendations = new List<Recommendation>();

            // Budget recommendations
            if (analysis.CampaignPerformance.BudgetUtilization < 0.8)
            {
                recommendations.Add(new Recommendation
                {
                    Category = RecommendationCategory.Budget,
                    Priority = RecommendationPriority.High,
                    Title = "Increase budget utilization",
                    Description = $"Your campaign is only using {analysis.CampaignPerformance.BudgetUtilization:P0} of daily budget. Consider increasing bids or expanding keywords.",
                    Action = "Review bid strategy and consider increasing bids or adding more keywords"
                });
            }

            // Ad performance recommendations
            var poorAds = analysis.AdPerformance.Where(ap => ap.Category == AdCategory.Underperforming).ToList();
            if (poorAds.Count >= 3)
            {
                recommendations.Add(new Recommendation
                {
                    Category = RecommendationCategory.AdCopy,
                    Priority = RecommendationPriority.High,
                    Title = "Optimize underperforming ads",
                    Description = $"You have {poorAds.Count} ads that are underperforming. Consider pausing them and creating new variations.",
                    Action = "Pause low-performing ads and use AI Ad Generator to create new variations"
                });
            }

            // Keyword recommendations
            var lowQualityKeywords = analysis.KeywordPerformance.Where(kp => kp.QualityScore <= 3).ToList();
            if (lowQualityKeywords.Any())
            {
                recommendations.Add(new Recommendation
                {
                    Category = RecommendationCategory.Keywords,
                    Priority = RecommendationPriority.Medium,
                    Title = "Improve keyword quality scores",
                    Description = $"You have {lowQualityKeywords.Count} keywords with low quality scores. This impacts your ad position and costs.",
                    Action = "Review low-quality keywords and improve ad relevance or landing page experience"
                });
            }

            // ROI recommendations
            if (analysis.CampaignPerformance.ROAScore < 100) // Negative ROI
            {
                recommendations.Add(new Recommendation
                {
                    Category = RecommendationCategory.ROI,
                    Priority = RecommendationPriority.Critical,
                    Title = "Negative ROI detected",
                    Description = $"Your campaign has a negative ROI of {analysis.CampaignPerformance.ROAScore:F1}%. Immediate optimization needed.",
                    Action = "Review targeting, bids, and ad copy. Consider pausing the campaign until optimization is complete"
                });
            }

            return recommendations.OrderByDescending(r => r.Priority).ToList();
        }

        private List<Recommendation> GenerateAccountRecommendations(AccountAnalysis accountAnalysis)
        {
            var recommendations = new List<Recommendation>();

            // Account-level budget allocation
            var bestPerformingCampaign = accountAnalysis.CampaignAnalyses
                .OrderByDescending(ca => ca.CampaignPerformance.OverallScore).FirstOrDefault();

            if (bestPerformingCampaign != null && accountAnalysis.TotalCampaigns > 1)
            {
                recommendations.Add(new Recommendation
                {
                    Category = RecommendationCategory.Budget,
                    Priority = RecommendationPriority.High,
                    Title = "Shift budget to top performer",
                    Description = $"{bestPerformingCampaign.Campaign.Name} is your best performing campaign. Consider allocating more budget there.",
                    Action = $"Move 10-20% of budget from underperforming campaigns to {bestPerformingCampaign.Campaign.Name}"
                });
            }

            // Overall account health
            if (accountAnalysis.AverageROI < 50)
            {
                recommendations.Add(new Recommendation
                {
                    Category = RecommendationCategory.ROI,
                    Priority = RecommendationPriority.Critical,
                    Title = "Account-wide ROI concerns",
                    Description = $"Your average ROI across all campaigns is {accountAnalysis.AverageROI:F1}%. Comprehensive optimization needed.",
                    Action = "Review all campaigns and focus budget on top performers. Consider pausing low-ROI campaigns."
                });
            }

            return recommendations;
        }

        private async Task<AIInsights> GetAIInsights(CampaignExportData campaign, List<TextAdExportData> ads, DateTime startDate, DateTime endDate)
        {
            try
            {
                var performanceData = new CampaignPerformanceData
                {
                    CampaignName = campaign.Name,
                    StartDate = startDate,
                    EndDate = endDate,
                    Impressions = campaign.Impressions,
                    Clicks = campaign.Clicks,
                    Cost = campaign.Cost,
                    Conversions = campaign.Conversions,
                    ClickThroughRate = campaign.ClickThroughRate,
                    CostPerClick = campaign.Cost / campaign.Clicks,
                    CostPerAcquisition = campaign.CostPerConversion,
                    ReturnOnAdSpend = campaign.ConversionValue / campaign.Cost,
                    DailyBudget = (double?)campaign.DailyBudget ?? 0,
                    BidStrategy = campaign.BiddingStrategyType,
                    TopAds = ads.Take(5).Select(ad => new AdPerformanceData
                    {
                        Headline = $"{ad.Headline1} - {ad.Headline2}",
                        CTR = ad.ClickThroughRate,
                        Conversions = ad.Conversions,
                        Cost = ad.Cost
                    }).ToList(),
                    UnderperformingAds = ads.TakeLast(3).Select(ad => new AdPerformanceData
                    {
                        Headline = $"{ad.Headline1} - {ad.Headline2}",
                        CTR = ad.ClickThroughRate,
                        Conversions = ad.Conversions,
                        Cost = ad.Cost
                    }).ToList()
                };

                var gptAnalysis = await _gptService.AnalyzeCampaignPerformanceAsync(performanceData);
                return new AIInsights
                {
                    Strengths = gptAnalysis.Strengths ?? new List<string>(),
                    Issues = gptAnalysis.Issues ?? new List<string>(),
                    Recommendations = (gptAnalysis.Recommendations ?? new List<GptRecommendation>())
                        .Select(r => new Recommendation
                        {
                            Priority = r.Priority == "high" ? RecommendationPriority.High
                                     : r.Priority == "critical" ? RecommendationPriority.Critical
                                     : r.Priority == "low" ? RecommendationPriority.Low
                                     : RecommendationPriority.Medium,
                            Action = r.Action,
                            Title = r.Action
                        }).ToList()
                };
            }
            catch
            {
                // Return basic insights if AI analysis fails
                return new AIInsights
                {
                    Strengths = new List<string> { "Campaign is active and gathering data" },
                    Issues = new List<string> { "AI analysis unavailable" },
                    Recommendations = new List<Recommendation>()
                };
            }
        }

        // Helper calculation methods
        private double CalculateCampaignScore(CampaignExportData campaign)
        {
            var score = 50.0;

            // ROI impact (40 points max)
            if (campaign.Roi > 200) score += 40;
            else if (campaign.Roi > 100) score += 30;
            else if (campaign.Roi > 0) score += 20;
            else if (campaign.Roi < -50) score -= 20;

            // CTR impact (20 points max)
            if (campaign.ClickThroughRate > 5.0) score += 20;
            else if (campaign.ClickThroughRate > 2.0) score += 15;
            else if (campaign.ClickThroughRate > 1.0) score += 10;
            else if (campaign.ClickThroughRate < 0.5) score -= 10;

            // Conversion rate impact (20 points max)
            var conversionRate = campaign.Impressions > 0 ? (campaign.Conversions / campaign.Impressions) * 100 : 0;
            if (conversionRate > 5.0) score += 20;
            else if (conversionRate > 2.0) score += 15;
            else if (conversionRate > 1.0) score += 10;

            // Cost efficiency (10 points max)
            if (campaign.CostPerConversion < 10.0) score += 10;
            else if (campaign.CostPerConversion < 50.0) score += 5;

            return Math.Max(0, Math.Min(100, score));
        }

        private double CalculateBudgetUtilization(CampaignExportData campaign)
        {
            return campaign.DailyBudget > 0 ? (campaign.Cost / (campaign.DailyBudget ?? 1)) : 0;
        }

        private double CalculateROAScore(CampaignExportData campaign)
        {
            return campaign.Roi;
        }

        private double CalculateConversionRate(CampaignExportData campaign)
        {
            return campaign.Impressions > 0 ? (campaign.Conversions / campaign.Impressions) * 100 : 0;
        }

        private double CalculateCostEfficiency(CampaignExportData campaign)
        {
            return campaign.CostPerConversion;
        }

        private double CalculateAdGroupScore(AdGroupExportData adGroup)
        {
            var score = 50.0;

            if (adGroup.Conversions > 0) score += 20;
            if (adGroup.ClickThroughRate > 2.0) score += 15;
            if (adGroup.CostPerConversion < 50.0) score += 15;
            if (adGroup.Roi > 0) score += 10;

            return Math.Max(0, Math.Min(100, score));
        }

        private PerformanceStatus DetermineAdGroupStatus(AdGroupExportData adGroup)
        {
            var score = CalculateAdGroupScore(adGroup);
            return score >= 70 ? PerformanceStatus.Excellent :
                   score >= 50 ? PerformanceStatus.Good :
                   score >= 30 ? PerformanceStatus.Fair :
                   PerformanceStatus.Poor;
        }

        private PerformanceStatus DetermineAdStatus(TextAdExportData ad)
        {
            return ad.PerformanceScore >= 70 ? PerformanceStatus.Excellent :
                   ad.PerformanceScore >= 50 ? PerformanceStatus.Good :
                   ad.PerformanceScore >= 30 ? PerformanceStatus.Fair :
                   PerformanceStatus.Poor;
        }

        private KeywordStatus DetermineKeywordStatus(KeywordExportData keyword)
        {
            if (keyword.QualityScore >= 8 && keyword.EffectivenessScore >= 70)
                return KeywordStatus.Excellent;
            if (keyword.QualityScore >= 5 && keyword.EffectivenessScore >= 50)
                return KeywordStatus.Good;
            if (keyword.QualityScore >= 3 || keyword.EffectivenessScore >= 30)
                return KeywordStatus.Fair;
            return KeywordStatus.Poor;
        }

        private string GetAdGroupRecommendation(AdGroupExportData adGroup)
        {
            if (adGroup.Conversions == 0 && adGroup.Impressions > 100)
                return "Consider pausing - no conversions despite good impressions";
            if (adGroup.ClickThroughRate < 1.0)
                return "Improve ad relevance or landing page experience";
            if (adGroup.CostPerConversion > 100.0)
                return "Reduce bids or improve ad targeting";
            return "Good performance - maintain current strategy";
        }

        private string GetAdSuggestion(AdPerformance performance)
        {
            return performance.Category switch
            {
                AdCategory.TopPerformer => "Create similar ad variations to scale success",
                AdCategory.Good => "Minor optimizations could improve performance",
                AdCategory.NeedsImprovement => "Test new headlines and descriptions",
                AdCategory.Underperforming => "Pause this ad and create new variation",
                _ => "Review and optimize"
            };
        }

        private string GetKeywordRecommendation(KeywordExportData keyword)
        {
            if (keyword.QualityScore <= 3)
                return "Improve ad relevance or landing page experience";
            if (keyword.Conversions == 0 && keyword.Impressions > 50)
                return "Consider pausing - no conversions after significant impressions";
            if (keyword.ClickThroughRate < 1.0)
                return "Improve keyword-ad relevance";
            return "Good performing keyword";
        }
    }

    // Analysis result models
    public class ComprehensiveAnalysis
    {
        public CampaignExportData Campaign { get; set; }
        public DateTime AnalysisDate { get; set; }
        public DateRange DateRange { get; set; }
        public CampaignPerformanceAnalysis CampaignPerformance { get; set; }
        public List<AdGroupPerformance> AdGroupPerformance { get; set; }
        public List<AdPerformance> AdPerformance { get; set; }
        public List<KeywordPerformance> KeywordPerformance { get; set; }
        public List<Recommendation> Recommendations { get; set; }
        public AIInsights AIInsights { get; set; }
    }

    public class AccountAnalysis
    {
        public DateTime AnalysisDate { get; set; }
        public DateRange DateRange { get; set; }
        public int TotalCampaigns { get; set; }
        public double TotalSpend { get; set; }
        public double TotalConversions { get; set; }
        public double AverageROI { get; set; }
        public List<ComprehensiveAnalysis> CampaignAnalyses { get; set; } = new List<ComprehensiveAnalysis>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<Recommendation> Recommendations { get; set; }
    }

    public class CampaignPerformanceAnalysis
    {
        public string CampaignName { get; set; }
        public double OverallScore { get; set; }
        public double BudgetUtilization { get; set; }
        public double ROAScore { get; set; }
        public double ConversionRate { get; set; }
        public double CostEfficiency { get; set; }
        public PerformanceStatus Status { get; set; }
    }

    public class AdGroupPerformance
    {
        public string AdGroupName { get; set; }
        public double PerformanceScore { get; set; }
        public PerformanceStatus Status { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Cost { get; set; }
        public double Conversions { get; set; }
        public double CTR { get; set; }
        public double CPA { get; set; }
        public string Recommendation { get; set; }
    }

    public class AdPerformance
    {
        public string AdId { get; set; }
        public string Headline { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Conversions { get; set; }
        public double CTR { get; set; }
        public double CPA { get; set; }
        public double PerformanceScore { get; set; }
        public PerformanceStatus Status { get; set; }
        public AdCategory Category { get; set; }
        public string ApprovalStatus { get; set; }
        public string Suggestion { get; set; }
    }

    public class KeywordPerformance
    {
        public string KeywordText { get; set; }
        public string MatchType { get; set; }
        public int QualityScore { get; set; }
        public double EffectivenessScore { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double CTR { get; set; }
        public double Conversions { get; set; }
        public double Cost { get; set; }
        public KeywordStatus Status { get; set; }
        public string Recommendation { get; set; }
    }

    public class DateRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class Recommendation
    {
        public RecommendationCategory Category { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
    }

    public class AIInsights
    {
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> Issues { get; set; } = new List<string>();
        public List<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
    }

    public enum PerformanceStatus { Excellent, Good, Fair, Poor }
    public enum AdCategory { TopPerformer, Good, NeedsImprovement, Underperforming }
    public enum KeywordStatus { Excellent, Good, Fair, Poor }
    public enum RecommendationCategory { Budget, AdCopy, Keywords, ROI, Targeting }
    public enum RecommendationPriority { Critical, High, Medium, Low }
}