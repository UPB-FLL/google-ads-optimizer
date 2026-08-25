using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleAdsOptimizer.Views
{
    public partial class AdGeneratorView : Page
    {
        private readonly ViewModels.AdGeneratorViewModel _viewModel;

        public AdGeneratorView()
        {
            InitializeComponent();
            _viewModel = App.Current.Properties["ServiceProvider"]?
                .GetType()?.GetProperty("ServiceProvider")?
                .GetValue(App.Current.Properties["ServiceProvider"])?
                .GetType()?.GetMethod("GetService")?
                .Invoke(App.Current.Properties["ServiceProvider"], new object[] { typeof(ViewModels.AdGeneratorViewModel) }
                ) as ViewModels.AdGeneratorViewModel;

            DataContext = _viewModel ?? new ViewModels.AdGeneratorViewModel();
            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            if (_viewModel != null)
            {
                EmptyStateText.Visibility = _viewModel.GeneratedAds.Count == 0 ?
                    Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void ResearchBrand_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                await _viewModel.ResearchBrandAsync();
            }
        }

        private async void GenerateAds_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                if (_viewModel != null)
                {
                    await _viewModel.GenerateAdsAsync();
                    UpdateEmptyState();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating ads: {ex.Message}",
                    "Generation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void ExportSelectedAds_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                if (_viewModel != null)
                {
                    await _viewModel.ExportSelectedAdsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting ads: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearAds_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Clear all generated ads?",
                "Clear Ads",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _viewModel?.ClearGeneratedAds();
                UpdateEmptyState();
            }
        }

        private void AddBenefit_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && !string.IsNullOrWhiteSpace(KeyBenefitTextBox.Text))
            {
                _viewModel.AddKeyBenefit(KeyBenefitTextBox.Text.Trim());
                KeyBenefitTextBox.Clear();
            }
        }

        private void RemoveBenefit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string benefit)
            {
                _viewModel?.RemoveKeyBenefit(benefit);
            }
        }

        private void SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                foreach (var ad in _viewModel.GeneratedAds)
                {
                    ad.IsSelected = true;
                }
            }
        }

        private void SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                foreach (var ad in _viewModel.GeneratedAds)
                {
                    ad.IsSelected = false;
                }
            }
        }
    }
}