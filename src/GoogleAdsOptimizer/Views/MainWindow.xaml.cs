using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleAdsOptimizer.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadDashboard();
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboard();
        }

        private void CampaignAnalysis_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("CampaignView.xaml");
        }

        private void AdGenerator_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("AdGeneratorView.xaml");
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("ReportsView.xaml");
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("SettingsView.xaml");
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Google Ads Optimizer v1.0.0\n\n" +
                "Features:\n" +
                "• Connect to Google Ads API\n" +
                "• Analyze campaign performance\n" +
                "• Generate AI-powered ad copy\n" +
                "• Export to Google Ads Editor\n\n" +
                "For support, visit:\nhttps://github.com/UPB-FLL/google-ads-optimizer",
                "Google Ads Optimizer Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void LoadDashboard()
        {
            // Load dashboard overview
            var serviceProvider = (System.IServiceProvider)Application.Current.Properties["ServiceProvider"];
            var viewModel = serviceProvider.GetService<ViewModels.MainViewModel>();

            if (viewModel != null)
            {
                // For now, show a simple dashboard message
                MainFrame.Navigate(new Page());
                UpdateStatus("Dashboard loaded");
            }
        }

        private void NavigateToPage(string pageName)
        {
            try
            {
                MainFrame.Navigate(new Uri($"/Views/{pageName}", UriKind.Relative));
                UpdateStatus($"Navigated to {pageName.Replace("View.xaml", "")}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load page: {ex.Message}", "Navigation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = $"Ready | {message}";
        }
    }
}