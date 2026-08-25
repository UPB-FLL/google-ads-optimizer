using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GoogleAdsOptimizer.Services;

namespace GoogleAdsOptimizer.ViewModels
{
    public class AdGeneratorViewModel : INotifyPropertyChanged
    {
        private readonly GPTService _gptService;
        private readonly GoogleAdsExportService _exportService;
        private bool _isLoading;
        private bool _isGenerating;
        private string _campaignName;
        private string _productService;
        private string _targetAudience;
        private string _campaignGoal;
        private string _brandName;
        private string _brandVoice;
        private string _industry;
        private int _numberOfVariations = 3;
        private ObservableCollection<string> _keyBenefits = new ObservableCollection<string>();
        private ObservableCollection<GeneratedAdItem> _generatedAds = new ObservableCollection<GeneratedAdItem>();
        private ObservableCollection<string> _competitorInfo = new ObservableCollection<string>();
        private BrandResearchResult _currentBrandResearch;

        public AdGeneratorViewModel()
        {
            var serviceProvider = (System.IServiceProvider)App.Current.Properties["ServiceProvider"];
            _gptService = serviceProvider.GetService<GPTService>();
            _exportService = serviceProvider.GetService<GoogleAdsExportService>();

            GeneratedAds = new ObservableCollection<GeneratedAdItem>();
            KeyBenefits = new ObservableCollection<string>();
            CompetitorInfo = new ObservableCollection<string>();

            // Add some sample key benefits
            KeyBenefits.Add("High quality products");
            KeyBenefits.Add("Excellent customer service");
            KeyBenefits.Add("Competitive pricing");
        }

        public ObservableCollection<GeneratedAdItem> GeneratedAds
        {
            get => _generatedAds;
            set
            {
                _generatedAds = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> KeyBenefits
        {
            get => _keyBenefits;
            set
            {
                _keyBenefits = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> CompetitorInfo
        {
            get => _competitorInfo;
            set
            {
                _competitorInfo = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                _isGenerating = value;
                OnPropertyChanged();
            }
        }

        public string CampaignName
        {
            get => _campaignName;
            set
            {
                _campaignName = value;
                OnPropertyChanged();
            }
        }

        public string ProductService
        {
            get => _productService;
            set
            {
                _productService = value;
                OnPropertyChanged();
            }
        }

        public string TargetAudience
        {
            get => _targetAudience;
            set
            {
                _targetAudience = value;
                OnPropertyChanged();
            }
        }

        public string CampaignGoal
        {
            get => _campaignGoal;
            set
            {
                _campaignGoal = value;
                OnPropertyChanged();
            }
        }

        public string BrandName
        {
            get => _brandName;
            set
            {
                _brandName = value;
                OnPropertyChanged();
            }
        }

        public string BrandVoice
        {
            get => _brandVoice;
            set
            {
                _brandVoice = value;
                OnPropertyChanged();
            }
        }

        public string Industry
        {
            get => _industry;
            set
            {
                _industry = value;
                OnPropertyChanged();
            }
        }

        public int NumberOfVariations
        {
            get => _numberOfVariations;
            set
            {
                _numberOfVariations = value;
                OnPropertyChanged();
            }
        }

        public BrandResearchResult CurrentBrandResearch
        {
            get => _currentBrandResearch;
            set
            {
                _currentBrandResearch = value;
                OnPropertyChanged();
            }
        }

        public async Task ResearchBrandAsync()
        {
            if (string.IsNullOrWhiteSpace(BrandName) || string.IsNullOrWhiteSpace(Industry))
            {
                System.Windows.MessageBox.Show(
                    "Please enter brand name and industry first.",
                    "Missing Information",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;

                var research = await _gptService.ResearchBrandAsync(BrandName, Industry);
                CurrentBrandResearch = new BrandResearchResult
                {
                    BrandVoice = research.BrandVoice,
                    KeyValues = research.KeyValues,
                    TargetDemographics = research.TargetDemographics,
                    CompetitivePositioning = research.CompetitivePositioning,
                    RecommendedTone = research.RecommendedTone,
                    RawAnalysis = research.RawAnalysis
                };

                // Auto-fill brand voice if it was empty
                if (string.IsNullOrEmpty(BrandVoice) && !string.IsNullOrEmpty(research.BrandVoice))
                {
                    BrandVoice = research.BrandVoice;
                }

                System.Windows.MessageBox.Show(
                    $"Brand research completed!\n\n" +
                    $"Brand Voice: {research.BrandVoice}\n" +
                    $"Target Demographics: {research.TargetDemographics}\n" +
                    $"Recommended Tone: {research.RecommendedTone}",
                    "Brand Research Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Brand research failed: {ex.Message}",
                    "Research Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task GenerateAdsAsync()
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(CampaignName) ||
                string.IsNullOrWhiteSpace(ProductService) ||
                string.IsNullOrWhiteSpace(TargetAudience) ||
                string.IsNullOrWhiteSpace(BrandName) ||
                string.IsNullOrWhiteSpace(Industry))
            {
                System.Windows.MessageBox.Show(
                    "Please fill in all required fields:\n" +
                    "- Campaign Name\n" +
                    "- Product/Service\n" +
                    "- Target Audience\n" +
                    "- Brand Name\n" +
                    "- Industry",
                    "Missing Information",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsGenerating = true;
                GeneratedAds.Clear();

                var request = new AdGenerationRequest
                {
                    CampaignName = CampaignName,
                    ProductService = ProductService,
                    TargetAudience = TargetAudience,
                    CampaignGoal = CampaignGoal ?? "Drive conversions",
                    BrandName = BrandName,
                    BrandVoice = BrandVoice ?? "Professional and trustworthy",
                    Industry = Industry,
                    KeyBenefits = KeyBenefits.ToList(),
                    CompetitorInfo = CompetitorInfo.ToList(),
                    NumberOfVariations = NumberOfVariations
                };

                var generatedAds = await _gptService.GenerateAdsAsync(request);

                foreach (var ad in generatedAds)
                {
                    GeneratedAds.Add(new GeneratedAdItem
                    {
                        CampaignName = ad.CampaignName,
                        Headline1 = ad.Headline1,
                        Headline2 = ad.Headline2,
                        Headline3 = ad.Headline3,
                        Description = ad.Description,
                        Description2 = ad.Description2,
                        FinalUrl = ad.FinalUrl,
                        DisplayUrl = ad.DisplayUrl,
                        IsSelected = false
                    });
                }

                System.Windows.MessageBox.Show(
                    $"Successfully generated {GeneratedAds.Count} ad variations!",
                    "Generation Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Ad generation failed: {ex.Message}",
                    "Generation Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsGenerating = false;
            }
        }

        public async Task ExportSelectedAdsAsync()
        {
            var selectedAds = GeneratedAds.Where(ad => ad.IsSelected).ToList();

            if (selectedAds.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Please select at least one ad to export.",
                    "No Ads Selected",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;

                // Create export package
                var packageData = new CampaignPackageData
                {
                    Campaigns = new[]
                    {
                        new CampaignData
                        {
                            Name = CampaignName,
                            Status = CampaignStatus.Enabled,
                            AdvertisingChannelType = AdvertisingChannelType.Search
                        }
                    },
                    TextAds = selectedAds.Select(ad => new TextAdData
                    {
                        Name = ad.CampaignName,
                        Headline1 = ad.Headline1,
                        Headline2 = ad.Headline2,
                        Headline3 = ad.Headline3,
                        Description = ad.Description,
                        Description2 = ad.Description2,
                        FinalUrl = ad.FinalUrl,
                        DisplayUrl = ad.DisplayUrl,
                        Status = AdGroupAdStatus.Enabled,
                        CampaignName = CampaignName
                    }).ToList()
                };

                var exportPackage = await _exportService.CreateEditorPackage(packageData);

                // Save to disk
                var outputPath = await _exportService.SavePackageToDisk(exportPackage,
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

                System.Windows.MessageBox.Show(
                    $"Successfully exported {selectedAds.Count} ads to:\n{outputPath}\n\n" +
                    "Import these files into Google Ads Editor:\n" +
                    "1. campaigns.csv\n" +
                    "2. ads.csv",
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

        public void AddKeyBenefit(string benefit)
        {
            if (!string.IsNullOrWhiteSpace(benefit))
            {
                KeyBenefits.Add(benefit);
            }
        }

        public void RemoveKeyBenefit(string benefit)
        {
            KeyBenefits.Remove(benefit);
        }

        public void AddCompetitorInfo(string competitor)
        {
            if (!string.IsNullOrWhiteSpace(competitor))
            {
                CompetitorInfo.Add(competitor);
            }
        }

        public void RemoveCompetitorInfo(string competitor)
        {
            CompetitorInfo.Remove(competitor);
        }

        public void ClearGeneratedAds()
        {
            GeneratedAds.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Supporting models
    public class GeneratedAdItem
    {
        public string CampaignName { get; set; }
        public string Headline1 { get; set; }
        public string Headline2 { get; set; }
        public string Headline3 { get; set; }
        public string Description { get; set; }
        public string Description2 { get; set; }
        public string FinalUrl { get; set; }
        public string DisplayUrl { get; set; }
        public bool IsSelected { get; set; }

        public string AdPreview => $"{Headline1} | {Headline2} | {Description}";
    }

    public class BrandResearchResult
    {
        public string BrandVoice { get; set; }
        public string KeyValues { get; set; }
        public string TargetDemographics { get; set; }
        public string CompetitivePositioning { get; set; }
        public string RecommendedTone { get; set; }
        public string RawAnalysis { get; set; }
    }
}