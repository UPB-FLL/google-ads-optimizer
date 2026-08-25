using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleAdsOptimizer.Views
{
    public partial class CampaignView : Page
    {
        private readonly ViewModels.CampaignViewModel _viewModel;

        public CampaignView()
        {
            InitializeComponent();
            _viewModel = App.Current.Properties["ServiceProvider"]?
                .GetType()?.GetProperty("ServiceProvider")?
                .GetValue(App.Current.Properties["ServiceProvider"])?
                .GetType()?.GetMethod("GetService")?
                .Invoke(App.Current.Properties["ServiceProvider"], new object[] { typeof(ViewModels.CampaignViewModel) }
                ) as ViewModels.CampaignViewModel;

            DataContext = _viewModel ?? new ViewModels.CampaignViewModel();
            LoadCampaigns();
        }

        private async void LoadCampaigns()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                StatusText.Text = "Loading campaigns...";

                if (_viewModel != null)
                {
                    await _viewModel.LoadCampaignsAsync();
                    CampaignComboBox.ItemsSource = _viewModel.AvailableCampaigns;

                    // Set default date range
                    StartDatePicker.SelectedDate = DateTime.Now.AddDays(-30);
                    EndDatePicker.SelectedDate = DateTime.Now;

                    StatusText.Text = $"Loaded {_viewModel.AvailableCampaigns.Count} campaigns";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading campaigns: {ex.Message}";
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void CampaignComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CampaignComboBox.SelectedItem is ViewModels.CampaignItem selectedCampaign)
            {
                StatusText.Text = $"Analyzing {selectedCampaign.Name}...";
                await AnalyzeCampaign();
            }
        }

        private async void Analyze_Click(object sender, RoutedEventArgs e)
        {
            await AnalyzeCampaign();
        }

        private async System.Threading.Tasks.Task AnalyzeCampaign()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                if (_viewModel != null)
                {
                    await _viewModel.AnalyzeCampaignAsync();

                    // Update summary displays
                    UpdateSummaryDisplay();

                    StatusText.Text = "Analysis complete";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Analysis error: {ex.Message}";
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSummaryDisplay()
        {
            if (_viewModel?.CurrentAnalysis == null) return;

            var analysis = _viewModel.CurrentAnalysis;

            // Update summary cards
            OverallScoreText.Text = $"{analysis.CampaignPerformance.OverallScore:F0}/100";
            ROIText.Text = $"{analysis.CampaignPerformance.ROAScore:F1}%";
            ConversionRateText.Text = $"{analysis.CampaignPerformance.ConversionRate:F2}%";
            BudgetUsageText.Text = $"{analysis.CampaignPerformance.BudgetUtilization:P0}";

            // Set colors based on performance
            OverallScoreText.Foreground = System.Windows.Media.Brushes.Green;
            ROIText.Foreground = analysis.CampaignPerformance.ROAScore > 0 ?
                System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            ConversionRateText.Foreground = analysis.CampaignPerformance.ConversionRate > 2.0 ?
                System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Orange;
            BudgetUsageText.Foreground = analysis.CampaignPerformance.BudgetUtilization > 0.8 ?
                System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Orange;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadCampaigns();
        }

        private async void ExportToEditor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                StatusText.Text = "Preparing export...";

                if (_viewModel != null)
                {
                    await _viewModel.ExportToGoogleAdsEditorAsync();
                    StatusText.Text = "Export completed";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Export error: {ex.Message}";
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
    }
}