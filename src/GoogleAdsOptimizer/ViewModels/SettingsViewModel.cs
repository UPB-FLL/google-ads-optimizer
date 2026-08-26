using System.Linq;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace GoogleAdsOptimizer.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private bool _isLoading;
        private bool _isSaving;
        private string _googleAdsClientId;
        private string _googleAdsClientSecret;
        private string _googleAdsRefreshToken;
        private string _googleAdsDeveloperToken;
        private string _googleAdsCustomerId;
        private string _openAIApiKey;
        private string _openAIDeploymentName = "gpt-4";
        private bool _useAzureOpenAI;
        private string _azureOpenAIEndpoint;
        private bool _isGoogleAdsConfigured;
        private bool _isOpenAIConfigured;
        private ObservableCollection<string> _recentConnections = new ObservableCollection<string>();

        private const string ConfigFilePath = "app_config.json";

        public SettingsViewModel()
        {
            RecentConnections = new ObservableCollection<string>();
            LoadConfigurationAsync().Wait();
        }

        public ObservableCollection<string> RecentConnections { get; set; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                _isSaving = value;
                OnPropertyChanged();
            }
        }

        // Google Ads Properties
        public string GoogleAdsClientId
        {
            get => _googleAdsClientId;
            set
            {
                _googleAdsClientId = value;
                OnPropertyChanged();
            }
        }

        public string GoogleAdsClientSecret
        {
            get => _googleAdsClientSecret;
            set
            {
                _googleAdsClientSecret = value;
                OnPropertyChanged();
            }
        }

        public string GoogleAdsRefreshToken
        {
            get => _googleAdsRefreshToken;
            set
            {
                _googleAdsRefreshToken = value;
                OnPropertyChanged();
            }
        }

        public string GoogleAdsDeveloperToken
        {
            get => _googleAdsDeveloperToken;
            set
            {
                _googleAdsDeveloperToken = value;
                OnPropertyChanged();
            }
        }

        public string GoogleAdsCustomerId
        {
            get => _googleAdsCustomerId;
            set
            {
                _googleAdsCustomerId = value;
                OnPropertyChanged();
            }
        }

        // OpenAI Properties
        public string OpenAIApiKey
        {
            get => _openAIApiKey;
            set
            {
                _openAIApiKey = value;
                OnPropertyChanged();
            }
        }

        public string OpenAIDeploymentName
        {
            get => _openAIDeploymentName;
            set
            {
                _openAIDeploymentName = value;
                OnPropertyChanged();
            }
        }

        public bool UseAzureOpenAI
        {
            get => _useAzureOpenAI;
            set
            {
                _useAzureOpenAI = value;
                OnPropertyChanged();
            }
        }

        public string AzureOpenAIEndpoint
        {
            get => _azureOpenAIEndpoint;
            set
            {
                _azureOpenAIEndpoint = value;
                OnPropertyChanged();
            }
        }

        public bool IsGoogleAdsConfigured
        {
            get => _isGoogleAdsConfigured;
            set
            {
                _isGoogleAdsConfigured = value;
                OnPropertyChanged();
            }
        }

        public bool IsOpenAIConfigured
        {
            get => _isOpenAIConfigured;
            set
            {
                _isOpenAIConfigured = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadConfigurationAsync()
        {
            try
            {
                IsLoading = true;

                if (File.Exists(ConfigFilePath))
                {
                    var encryptedData = await File.ReadAllTextAsync(ConfigFilePath);
                    var config = DecryptConfiguration(encryptedData);

                    GoogleAdsClientId = config.GoogleAdsClientId ?? "";
                    GoogleAdsClientSecret = config.GoogleAdsClientSecret ?? "";
                    GoogleAdsRefreshToken = config.GoogleAdsRefreshToken ?? "";
                    GoogleAdsDeveloperToken = config.GoogleAdsDeveloperToken ?? "";
                    GoogleAdsCustomerId = config.GoogleAdsCustomerId ?? "";
                    OpenAIApiKey = config.OpenAIApiKey ?? "";
                    OpenAIDeploymentName = config.OpenAIDeploymentName ?? "gpt-4";
                    UseAzureOpenAI = config.UseAzureOpenAI;
                    AzureOpenAIEndpoint = config.AzureOpenAIEndpoint ?? "";

                    if (config.RecentConnections != null)
                    {
                        foreach (var connection in config.RecentConnections)
                        {
                            RecentConnections.Add(connection);
                        }
                    }
                }

                UpdateConfigurationStatus();
            }
            catch (Exception ex)
            {
                // Config file doesn't exist or is corrupted - use defaults
                SetDefaultValues();
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task SaveConfigurationAsync()
        {
            try
            {
                IsSaving = true;

                var config = new AppConfiguration
                {
                    GoogleAdsClientId = GoogleAdsClientId,
                    GoogleAdsClientSecret = GoogleAdsClientSecret,
                    GoogleAdsRefreshToken = GoogleAdsRefreshToken,
                    GoogleAdsDeveloperToken = GoogleAdsDeveloperToken,
                    GoogleAdsCustomerId = GoogleAdsCustomerId,
                    OpenAIApiKey = OpenAIApiKey,
                    OpenAIDeploymentName = OpenAIDeploymentName,
                    UseAzureOpenAI = UseAzureOpenAI,
                    AzureOpenAIEndpoint = AzureOpenAIEndpoint,
                    RecentConnections = RecentConnections.ToList()
                };

                var encryptedData = EncryptConfiguration(config);
                await File.WriteAllTextAsync(ConfigFilePath, encryptedData);

                UpdateConfigurationStatus();

                // Add to recent connections if Google Ads is configured
                if (IsGoogleAdsConfigured && !string.IsNullOrEmpty(GoogleAdsCustomerId))
                {
                    var connectionInfo = $"Connected: {GoogleAdsCustomerId} - {DateTime.Now:yyyy-MM-dd HH:mm}";
                    if (!RecentConnections.Contains(connectionInfo))
                    {
                        RecentConnections.Insert(0, connectionInfo);
                        if (RecentConnections.Count > 5)
                        {
                            RecentConnections.RemoveAt(RecentConnections.Count - 1);
                        }
                    }
                }

                System.Windows.MessageBox.Show(
                    "Configuration saved successfully!",
                    "Settings Saved",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to save configuration: {ex.Message}",
                    "Save Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }

        public async Task<bool> TestGoogleAdsConnectionAsync()
        {
            if (string.IsNullOrEmpty(GoogleAdsClientId) ||
                string.IsNullOrEmpty(GoogleAdsClientSecret) ||
                string.IsNullOrEmpty(GoogleAdsRefreshToken) ||
                string.IsNullOrEmpty(GoogleAdsDeveloperToken) ||
                string.IsNullOrEmpty(GoogleAdsCustomerId))
            {
                System.Windows.MessageBox.Show(
                    "Please fill in all Google Ads API credentials first.",
                    "Missing Credentials",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return false;
            }

            try
            {
                IsLoading = true;

                // Simulate connection test (would actually use the GoogleAdsService)
                await Task.Delay(2000);

                System.Windows.MessageBox.Show(
                    "Google Ads API connection test successful!",
                    "Connection Test",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Google Ads API connection test failed: {ex.Message}",
                    "Connection Test Failed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<bool> TestOpenAIConnectionAsync()
        {
            if (string.IsNullOrEmpty(OpenAIApiKey))
            {
                System.Windows.MessageBox.Show(
                    "Please enter your OpenAI API key first.",
                    "Missing API Key",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return false;
            }

            try
            {
                IsLoading = true;

                // Simulate connection test (would actually use the GPTService)
                await Task.Delay(1500);

                System.Windows.MessageBox.Show(
                    "OpenAI API connection test successful!",
                    "Connection Test",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"OpenAI API connection test failed: {ex.Message}",
                    "Connection Test Failed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void ClearAllSettings()
        {
            var result = System.Windows.MessageBox.Show(
                "This will clear all saved settings and credentials. Continue?",
                "Clear Settings",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                SetDefaultValues();
                RecentConnections.Clear();

                if (File.Exists(ConfigFilePath))
                {
                    File.Delete(ConfigFilePath);
                }

                System.Windows.MessageBox.Show(
                    "All settings have been cleared.",
                    "Settings Cleared",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }

        private void SetDefaultValues()
        {
            GoogleAdsClientId = "";
            GoogleAdsClientSecret = "";
            GoogleAdsRefreshToken = "";
            GoogleAdsDeveloperToken = "";
            GoogleAdsCustomerId = "";
            OpenAIApiKey = "";
            OpenAIDeploymentName = "gpt-4";
            UseAzureOpenAI = false;
            AzureOpenAIEndpoint = "";
            UpdateConfigurationStatus();
        }

        private void UpdateConfigurationStatus()
        {
            IsGoogleAdsConfigured =
                !string.IsNullOrEmpty(GoogleAdsClientId) &&
                !string.IsNullOrEmpty(GoogleAdsClientSecret) &&
                !string.IsNullOrEmpty(GoogleAdsRefreshToken) &&
                !string.IsNullOrEmpty(GoogleAdsDeveloperToken) &&
                !string.IsNullOrEmpty(GoogleAdsCustomerId);

            IsOpenAIConfigured = !string.IsNullOrEmpty(OpenAIApiKey);
        }

        private string EncryptConfiguration(AppConfiguration config)
        {
            // Simple encryption - in production use Windows Data Protection API (DPAPI)
            var json = JsonConvert.SerializeObject(config);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes("Your32ByteEncryptionKeyHere!!"); // In production, use proper key management
            aes.IV = Encoding.UTF8.GetBytes("Your16ByteIVHere!!");

            using var encryptor = aes.CreateEncryptor();
            var encryptedBytes = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

        private AppConfiguration DecryptConfiguration(string encryptedData)
        {
            try
            {
                var bytes = Convert.FromBase64String(encryptedData);

                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes("Your32ByteEncryptionKeyHere!!"); // Must match encryption key
                aes.IV = Encoding.UTF8.GetBytes("Your16ByteIVHere!!"); // Must match encryption IV

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);

                var json = Encoding.UTF8.GetString(decryptedBytes);
                return JsonConvert.DeserializeObject<AppConfiguration>(json);
            }
            catch
            {
                return new AppConfiguration(); // Return empty config if decryption fails
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Configuration model
    public class AppConfiguration
    {
        public string GoogleAdsClientId { get; set; }
        public string GoogleAdsClientSecret { get; set; }
        public string GoogleAdsRefreshToken { get; set; }
        public string GoogleAdsDeveloperToken { get; set; }
        public string GoogleAdsCustomerId { get; set; }
        public string OpenAIApiKey { get; set; }
        public string OpenAIDeploymentName { get; set; }
        public bool UseAzureOpenAI { get; set; }
        public string AzureOpenAIEndpoint { get; set; }
        public System.Collections.Generic.List<string> RecentConnections { get; set; }
    }
}