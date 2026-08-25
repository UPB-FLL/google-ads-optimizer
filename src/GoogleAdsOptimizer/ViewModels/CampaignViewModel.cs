using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GoogleAdsOptimizer.Services;

namespace GoogleAdsOptimizer.ViewModels
{
    public class CampaignViewModel : INotifyPropertyChanged
    {
        private readonly GoogleAdsService _googleAdsService;
        private readonly CampaignAnalyzer _campaignAnalyzer;
        private bool _isLoading;
        private string _selectedCampaignId;
        private DateTime _startDate = DateTime.Now.AddDays(-30);
        private DateTime _endDate = DateTime.Now;
        private ComprehensiveAnalysis _currentAnalysis;

        public CampaignViewModel()
        {
            // Initialize services
            var serviceProvider = (System.IServiceProvider)App.Current.Properties["ServiceProvider"];
            _googleAdsService = serviceProvider.GetService<GoogleAdsService>();
            _campaignAnalyzer = serviceProvider.GetService<CampaignAnalyzer>();

            AvailableCampaigns = new ObservableCollection<CampaignItem>();
            AnalysisResults = new ObservableCollection<AnalysisResult>();
            AdGroups = new ObservableCollection<AdGroupItem>();
            TopAds = new ObservableCollection<AdPerformanceItem>();
            UnderperformingAds = new ObservableCollection<AdPerformanceItem>();
            Keywords = new ObservableCollection<KeywordItem>();
            Recommendations = new ObservableCollection<RecommendationItem>();
        }

        public ObservableCollection<CampaignItem> AvailableCampaigns { get; }
        public ObservableCollection<AnalysisResult> AnalysisResults { get; }
        public ObservableCollection<AdGroupItem> AdGroups { get; }
        public ObservableCollection<AdPerformanceItem> TopAds { get; }
        public ObservableCollection<AdPerformanceItem> UnderperformingAds { get; }
        public ObservableCollection<KeywordItem> Keywords { get; }
        public ObservableCollection<RecommendationItem> Recommendations { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string SelectedCampaignId
        {
            get => _selectedCampaignId;
            set
            {
                _selectedCampaignId = value;
                OnPropertyChanged();
                // Trigger analysis when campaign is selected
                if (!string.IsNullOrEmpty(value))
                {
                    _ = AnalyzeCampaignAsync();
                }
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        public ComprehensiveAnalysis CurrentAnalysis
        {
            get => _currentAnalysis;
            set
            {
                _currentAnalysis = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadCampaignsAsync()
        {
            try
            {
                IsLoading = true;
                AvailableCampaigns.Clear();

                var campaigns = await _googleAdsService.GetCampaignsWithMetricsAsync(StartDate, EndDate);

                foreach (var campaign in campaigns)
                {
                    AvailableCampaigns.Add(new CampaignItem
                    {
                        Id = campaign.Id.ToString(),
                        Name = campaign.Name,
                        Status = campaign.Status.ToString(),
                        Impressions = campaign.Impressions,
                        Clicks = campaign.Clicks,
                        Cost = campaign.Cost,
                        Conversions = campaign.Conversions,
                        ROI = campaign.Roi,
                        CTR = campaign.ClickThroughRate,
                        CPA = campaign.CostPerConversion
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load campaigns: {ex.Message}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task AnalyzeCampaignAsync()
        {
            if (string.IsNullOrEmpty(SelectedCampaignId)) return;

            try
            {
                IsLoading = true;

                // Perform comprehensive analysis
                var analysis = await _campaignAnalyzer.AnalyzeCampaignAsync(SelectedCampaignId, StartDate, EndDate);
                CurrentAnalysis = analysis;

                // Update UI collections
                UpdateAnalysisResults(analysis);
                UpdateAdGroups(analysis);
                UpdateAds(analysis);
                UpdateKeywords(analysis);
                UpdateRecommendations(analysis);
            }
            catch (Exception ex)
            {
                throw new Exception($"Campaign analysis failed: {ex.Message}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ExportToGoogleAdsEditorAsync()
        {
            if (CurrentAnalysis == null) return;

            try
            {
                IsLoading = true;

                // Create export package
                var exportService = new GoogleAdsExportService();
                var packageData = new CampaignPackageData
                {
                    Campaigns = new[] { CurrentAnalysis.Campaign },
                    AdGroups = CurrentAnalysis.AdGroupPerformance.Select(ag => new AdGroupData
                    {
                        Name = ag.AdGroupName,
                        Status = AdGroupStatus.Enabled,
                        AdGroupType = AdGroupType.SearchStandard
                    }).ToList(),
                    TextAds = CurrentAnalysis.AdPerformance.Select(ad => new TextAdData
                    {
                        Name = ad.Headline,
                        Headline1 = "Headline", // Would come from actual ad data
                        Headline2 = "Headline 2",
                        Description = "Description",
                        Status = AdGroupAdStatus.Enabled
                    }).ToList()
                };

                var exportPackage = await exportService.CreateEditorPackage(packageData);

                // Save to disk
                var outputPath = await exportService.SavePackageToDisk(exportPackage,
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

                // Show success message
                System.Windows.MessageBox.Show(
                    $"Campaign exported successfully to:\n{outputPath}\n\n" +
                    "Import these files into Google Ads Editor:\n" +
                    "1. campaigns.csv\n" +
                    "2. adgroups.csv\n" +
                    "3. ads.csv\n" +
                    "4. keywords.csv",
                    "Export Successful",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Export failed: {ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateAnalysisResults(ComprehensiveAnalysis analysis)
        {
            AnalysisResults.Clear();

            AnalysisResults.Add(new AnalysisResult
            {
                Metric = "Overall Score",
                Value = $"{analysis.CampaignPerformance.OverallScore:F0}/100",
                Status = GetStatusColor(analysis.CampaignPerformance.OverallScore)
            });

            AnalysisResults.Add(new AnalysisResult
            {
                Metric = "Budget Utilization",
                Value = $"{analysis.CampaignPerformance.BudgetUtilization:P0}",
                Status = analysis.CampaignPerformance.BudgetUtilization > 0.8 ? "Good" : "Warning"
            });

            AnalysisResults.Add(new AnalysisResult
            {
                Metric = "ROI",
                Value = $"{analysis.CampaignPerformance.ROAScore:F1}%",
                Status = analysis.CampaignPerformance.ROAScore > 100 ? "Excellent" :
                        analysis.CampaignPerformance.ROAScore > 0 ? "Good" : "Poor"
            });

            AnalysisResults.Add(new AnalysisResult
            {
                Metric = "Conversion Rate",
                Value = $"{analysis.CampaignPerformance.ConversionRate:F2}%",
                Status = analysis.CampaignPerformance.ConversionRate > 2.0 ? "Good" : "Fair"
            });
        }

        private void UpdateAdGroups(ComprehensiveAnalysis analysis)
        {
            AdGroups.Clear();

            foreach (var adGroup in analysis.AdGroupPerformance.Take(10))
            {
                AdGroups.Add(new AdGroupItem
                {
                    Name = adGroup.AdGroupName,
                    Status = adGroup.Status.ToString(),
                    Impressions = adGroup.Impressions,
                    Clicks = adGroup.Clicks,
                    Cost = adGroup.Cost,
                    Conversions = adGroup.Conversions,
                    CTR = adGroup.CTR,
                    CPA = adGroup.CPA,
                    PerformanceScore = adGroup.PerformanceScore,
                    Recommendation = adGroup.Recommendation
                });
            }
        }

        private void UpdateAds(ComprehensiveAnalysis analysis)
        {
            TopAds.Clear();
            UnderperformingAds.Clear();

            foreach (var ad in analysis.AdPerformance)
            {
                var adItem = new AdPerformanceItem
                {
                    Headline = ad.Headline,
                    Impressions = ad.Impressions,
                    Clicks = ad.Clicks,
                    Conversions = ad.Conversions,
                    CTR = ad.CTR,
                    CPA = ad.CPA,
                    PerformanceScore = ad.PerformanceScore,
                    Status = ad.Status.ToString()
                };

                if (ad.Category == AdCategory.TopPerformer || ad.Category == AdCategory.Good)
                {
                    TopAds.Add(adItem);
                }
                else if (ad.Category == AdCategory.NeedsImprovement || ad.Category == AdCategory.Underperforming)
                {
                    UnderperformingAds.Add(adItem);
                }
            }
        }

        private void UpdateKeywords(ComprehensiveAnalysis analysis)
        {
            Keywords.Clear();

            foreach (var keyword in analysis.KeywordPerformance.Take(20))
            {
                Keywords.Add(new KeywordItem
                {
                    Text = keyword.KeywordText,
                    MatchType = keyword.MatchType,
                    QualityScore = keyword.QualityScore,
                    EffectivenessScore = keyword.EffectivenessScore,
                    Impressions = keyword.Impressions,
                    Clicks = keyword.Clicks,
                    CTR = keyword.CTR,
                    Conversions = keyword.Conversions,
                    Cost = keyword.Cost,
                    Status = keyword.Status.ToString(),
                    Recommendation = keyword.Recommendation
                });
            }
        }

        private void UpdateRecommendations(ComprehensiveAnalysis analysis)
        {
            Recommendations.Clear();

            foreach (var rec in analysis.Recommendations)
            {
                Recommendations.Add(new RecommendationItem
                {
                    Category = rec.Category.ToString(),
                    Priority = rec.Priority.ToString(),
                    Title = rec.Title,
                    Description = rec.Description,
                    Action = rec.Action
                });
            }
        }

        private string GetStatusColor(double score)
        {
            return score >= 70 ? "Excellent" :
                   score >= 50 ? "Good" :
                   score >= 30 ? "Fair" : "Poor";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Supporting models
    public class CampaignItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Cost { get; set; }
        public double Conversions { get; set; }
        public double ROI { get; set; }
        public double CTR { get; set; }
        public double CPA { get; set; }
    }

    public class AnalysisResult
    {
        public string Metric { get; set; }
        public string Value { get; set; }
        public string Status { get; set; }
    }

    public class AdGroupItem
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Cost { get; set; }
        public double Conversions { get; set; }
        public double CTR { get; set; }
        public double CPA { get; set; }
        public double PerformanceScore { get; set; }
        public string Recommendation { get; set; }
    }

    public class AdPerformanceItem
    {
        public string Headline { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Conversions { get; set; }
        public double CTR { get; set; }
        public double CPA { get; set; }
        public double PerformanceScore { get; set; }
        public string Status { get; set; }
    }

    public class KeywordItem
    {
        public string Text { get; set; }
        public string MatchType { get; set; }
        public int QualityScore { get; set; }
        public double EffectivenessScore { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double CTR { get; set; }
        public double Conversions { get; set; }
        public double Cost { get; set; }
        public string Status { get; set; }
        public string Recommendation { get; set; }
    }

    public class RecommendationItem
    {
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
    }
}