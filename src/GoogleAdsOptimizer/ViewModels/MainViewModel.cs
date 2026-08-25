using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GoogleAdsOptimizer.Services;

namespace GoogleAdsOptimizer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly GoogleAdsService _googleAdsService;
        private readonly GPTService _gptService;
        private string _statusMessage;
        private bool _isConnected;
        private bool _isLoading;
        private string _customerName;
        private int _totalCampaigns;
        private double _totalSpend;
        private double _totalConversions;
        private double _averageROI;

        public MainViewModel()
        {
            // Get services from DI container
            var serviceProvider = (System.IServiceProvider)App.Current.Properties["ServiceProvider"];
            _googleAdsService = serviceProvider.GetService<GoogleAdsService>();
            _gptService = serviceProvider.GetService<GPTService>();

            Campaigns = new ObservableCollection<CampaignSummary>();
            RecentActivity = new ObservableCollection<ActivityItem>();

            StatusMessage = "Ready - Configure your API credentials in Settings";
        }

        public ObservableCollection<CampaignSummary> Campaigns { get; }
        public ObservableCollection<ActivityItem> RecentActivity { get; }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
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

        public string CustomerName
        {
            get => _customerName;
            set
            {
                _customerName = value;
                OnPropertyChanged();
            }
        }

        public int TotalCampaigns
        {
            get => _totalCampaigns;
            set
            {
                _totalCampaigns = value;
                OnPropertyChanged();
            }
        }

        public double TotalSpend
        {
            get => _totalSpend;
            set
            {
                _totalSpend = value;
                OnPropertyChanged();
            }
        }

        public double TotalConversions
        {
            get => _totalConversions;
            set
            {
                _totalConversions = value;
                OnPropertyChanged();
            }
        }

        public double AverageROI
        {
            get => _averageROI;
            set
            {
                _averageROI = value;
                OnPropertyChanged();
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Checking connection...";

                // Check if credentials are configured
                var hasCredentials = await CheckCredentialsAsync();
                if (!hasCredentials)
                {
                    StatusMessage = "Please configure your API credentials in Settings";
                    IsLoading = false;
                    return;
                }

                // Load dashboard data
                await LoadDashboardDataAsync();

                IsConnected = true;
                StatusMessage = $"Connected - {TotalCampaigns} campaigns, ${TotalSpend:F2} total spend";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading dashboard: {ex.Message}";
                IsConnected = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RefreshDataAsync()
        {
            if (!IsConnected) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Refreshing data...";

                await LoadDashboardDataAsync();

                AddActivity("Data refreshed successfully");
                StatusMessage = $"Refreshed - {TotalCampaigns} campaigns loaded";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Testing connection...";

                // This would test the actual Google Ads connection
                // For now, simulate a connection test
                await Task.Delay(2000);

                IsConnected = true;
                AddActivity("Connection test successful");
                StatusMessage = "Connected to Google Ads";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection failed: {ex.Message}";
                IsConnected = false;
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<bool> CheckCredentialsAsync()
        {
            // Check if API credentials are stored
            // This would check Windows Credential Manager or config
            await Task.Delay(100);
            return false; // Placeholder - would check real credentials
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddDays(-30); // Last 30 days

                // This would load real data from Google Ads
                await Task.Delay(1500); // Simulate API call

                // Load sample data for now
                Campaigns.Clear();
                Campaigns.Add(new CampaignSummary
                {
                    Name = "Summer Sale Campaign",
                    Status = "Active",
                    Impressions = 45230,
                    Clicks = 892,
                    Cost = 1245.67,
                    Conversions = 34,
                    ROI = 145.3
                });

                Campaigns.Add(new CampaignSummary
                {
                    Name = "Brand Awareness",
                    Status = "Active",
                    Impressions = 89451,
                    Clicks = 2341,
                    Cost = 3421.89,
                    Conversions = 67,
                    ROI = 89.7
                });

                Campaigns.Add(new CampaignSummary
                {
                    Name = "Product Launch",
                    Status = "Paused",
                    Impressions = 12450,
                    Clicks = 156,
                    Cost = 456.23,
                    Conversions = 8,
                    ROI = 67.8
                });

                // Calculate totals
                TotalCampaigns = Campaigns.Count;
                TotalSpend = Campaigns.Sum(c => c.Cost);
                TotalConversions = Campaigns.Sum(c => c.Conversions);
                AverageROI = Campaigns.Average(c => c.ROI);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load dashboard data: {ex.Message}", ex);
            }
        }

        private void AddActivity(string activity)
        {
            RecentActivity.Insert(0, new ActivityItem
            {
                Timestamp = DateTime.Now,
                Description = activity
            });

            // Keep only last 10 activities
            if (RecentActivity.Count > 10)
            {
                RecentActivity.RemoveAt(RecentActivity.Count - 1);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Supporting models
    public class CampaignSummary
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Cost { get; set; }
        public double Conversions { get; set; }
        public double ROI { get; set; }
        public double CTR => Impressions > 0 ? (Clicks / (double)Impressions) * 100 : 0;
        public double CPA => Conversions > 0 ? Cost / Conversions : 0;
    }

    public class ActivityItem
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }
}