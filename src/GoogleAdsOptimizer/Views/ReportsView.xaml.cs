using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleAdsOptimizer.Views
{
    public partial class ReportsView : Page
    {
        private readonly ViewModels.ReportsViewModel _viewModel;

        public ReportsView()
        {
            InitializeComponent();
            _viewModel = App.Current.Properties["ServiceProvider"]?
                .GetType()?.GetProperty("ServiceProvider")?
                .GetValue(App.Current.Properties["ServiceProvider"])?
                .GetType()?.GetMethod("GetService")?
                .Invoke(App.Current.Properties["ServiceProvider"], new object[] { typeof(ViewModels.ReportsViewModel) }
                ) as ViewModels.ReportsViewModel;

            DataContext = _viewModel ?? new ViewModels.ReportsViewModel();
            LoadSampleMetrics();
        }

        private void LoadSampleMetrics()
        {
            // Load sample account metrics
            if (_viewModel != null)
            {
                _viewModel.AccountMetrics.Add(new ViewModels.PerformanceMetric
                {
                    Name = "Total Spend",
                    Value = "$12,456.78",
                    Change = "+12.5%",
                    IsPositive = true
                });

                _viewModel.AccountMetrics.Add(new ViewModels.PerformanceMetric
                {
                    Name = "Total Conversions",
                    Value = "234",
                    Change = "+8.3%",
                    IsPositive = true
                });

                _viewModel.AccountMetrics.Add(new ViewModels.PerformanceMetric
                {
                    Name = "Average ROI",
                    Value = "145.2%",
                    Change = "+5.2%",
                    IsPositive = true
                });

                _viewModel.AccountMetrics.Add(new ViewModels.PerformanceMetric
                {
                    Name = "Active Campaigns",
                    Value = "8",
                    Change = "0%",
                    IsPositive = true
                });
            }
        }

        private async void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                await _viewModel.GeneratePerformanceReportAsync();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadSampleMetrics();
        }

        private async void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ViewModels.ReportItem report)
            {
                await _viewModel.ExportReportAsync(report);
            }
        }

        private void DeleteReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ViewModels.ReportItem report && _viewModel != null)
            {
                _viewModel.DeleteReport(report);
            }
        }
    }
}