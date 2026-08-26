using System.Linq;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GoogleAdsOptimizer.Services;

namespace GoogleAdsOptimizer.ViewModels
{
    public class ReportsViewModel : INotifyPropertyChanged
    {
        private readonly GoogleAdsService _googleAdsService;
        private readonly CampaignAnalyzer _campaignAnalyzer;
        private bool _isLoading;
        private DateTime _reportStartDate = DateTime.Now.AddDays(-30);
        private DateTime _reportEndDate = DateTime.Now;
        private ObservableCollection<ReportItem> _reports = new ObservableCollection<ReportItem>();
        private ObservableCollection<PerformanceMetric> _accountMetrics = new ObservableCollection<PerformanceMetric>();

        public ReportsViewModel()
        {
            var serviceProvider = (System.IServiceProvider)App.Current.Properties["ServiceProvider"];
            _googleAdsService = serviceProvider.GetService<GoogleAdsService>();
            _campaignAnalyzer = serviceProvider.GetService<CampaignAnalyzer>();

            Reports = new ObservableCollection<ReportItem>();
            AccountMetrics = new ObservableCollection<PerformanceMetric>();

            // Add sample reports
            Reports.Add(new ReportItem
            {
                Name = "Monthly Performance Report",
                Type = "Performance",
                CreatedDate = DateTime.Now.AddDays(-5),
                Status = "Ready"
            });

            Reports.Add(new ReportItem
            {
                Name = "Q2 Optimization Analysis",
                Type = "Analysis",
                CreatedDate = DateTime.Now.AddDays(-15),
                Status = "Ready"
            });
        }

        public ObservableCollection<ReportItem> Reports
        {
            get => _reports;
            set
            {
                _reports = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PerformanceMetric> AccountMetrics
        {
            get => _accountMetrics;
            set
            {
                _accountMetrics = value;
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

        public DateTime ReportStartDate
        {
            get => _reportStartDate;
            set
            {
                _reportStartDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime ReportEndDate
        {
            get => _reportEndDate;
            set
            {
                _reportEndDate = value;
                OnPropertyChanged();
            }
        }

        public async Task GeneratePerformanceReportAsync()
        {
            try
            {
                IsLoading = true;

                // Generate comprehensive account report
                var accountAnalysis = await _campaignAnalyzer.AnalyzeAccountAsync(ReportStartDate, ReportEndDate);

                // Update account metrics
                AccountMetrics.Clear();
                AccountMetrics.Add(new PerformanceMetric
                {
                    Name = "Total Spend",
                    Value = accountAnalysis.TotalSpend.ToString("C2"),
                    Change = "+12.5%",
                    IsPositive = true
                });

                AccountMetrics.Add(new PerformanceMetric
                {
                    Name = "Total Conversions",
                    Value = accountAnalysis.TotalConversions.ToString("F0"),
                    Change = "+8.3%",
                    IsPositive = true
                });

                AccountMetrics.Add(new PerformanceMetric
                {
                    Name = "Average ROI",
                    Value = accountAnalysis.AverageROI.ToString("F1") + "%",
                    Change = "+5.2%",
                    IsPositive = true
                });

                AccountMetrics.Add(new PerformanceMetric
                {
                    Name = "Active Campaigns",
                    Value = accountAnalysis.TotalCampaigns.ToString(),
                    Change = "0%",
                    IsPositive = true
                });

                // Add the generated report to the list
                var report = new ReportItem
                {
                    Name = $"Performance Report {DateTime.Now:yyyy-MM-dd}",
                    Type = "Performance",
                    CreatedDate = DateTime.Now,
                    Status = "Ready",
                    AccountAnalysis = accountAnalysis
                };

                Reports.Insert(0, report);

                System.Windows.MessageBox.Show(
                    $"Performance report generated successfully!\n\n" +
                    $"Period: {ReportStartDate:yyyy-MM-dd} to {ReportEndDate:yyyy-MM-dd}\n" +
                    $"Campaigns Analyzed: {accountAnalysis.TotalCampaigns}\n" +
                    $"Total Spend: ${accountAnalysis.TotalSpend:F2}\n" +
                    $"Conversions: {accountAnalysis.TotalConversions:F0}\n" +
                    $"Average ROI: {accountAnalysis.AverageROI:F1}%",
                    "Report Generated",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to generate report: {ex.Message}",
                    "Report Generation Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ExportReportAsync(ReportItem report)
        {
            try
            {
                IsLoading = true;

                // Export report to file
                var exportService = new GoogleAdsExportService();

                // This would generate a comprehensive report file
                await Task.Delay(1000); // Simulate export

                var fileName = $"GoogleAdsReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var outputPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    fileName
                );

                // Simulate creating the file
                System.IO.File.WriteAllText(outputPath, $"Google Ads Report\nGenerated: {DateTime.Now}\n\nAccount Analysis would be exported here.");

                System.Windows.MessageBox.Show(
                    $"Report exported to:\n{outputPath}",
                    "Export Successful",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to export report: {ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void DeleteReport(ReportItem report)
        {
            var result = System.Windows.MessageBox.Show(
                $"Delete report '{report.Name}'?",
                "Delete Report",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                Reports.Remove(report);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Supporting models
    public class ReportItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; }
        public AccountAnalysis AccountAnalysis { get; set; }
        public string TypeDisplay => $"{Type} Report - {CreatedDate:yyyy-MM-dd}";
    }

    public class PerformanceMetric
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Change { get; set; }
        public bool IsPositive { get; set; }
        public string ChangeColor => IsPositive ? "Green" : "Red";
        public string ChangeSymbol => IsPositive ? "↑" : "↓";
    }
}