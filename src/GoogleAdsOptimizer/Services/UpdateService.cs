using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleAdsOptimizer.Services
{
    /// <summary>
    /// Auto-update service for checking and installing application updates
    /// </summary>
    public class UpdateService : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        private bool _isCheckingForUpdates;
        private bool _updateAvailable;
        private bool _isDownloading;
        private bool _isInstalling;
        private string _currentVersion;
        private string _latestVersion;
        private UpdateManifest _updateManifest;
        private UpdateInfo _availableUpdate;
        private double _downloadProgress;
        private string _statusMessage;

        public event EventHandler<UpdateAvailableEventArgs> UpdateAvailable;

        public UpdateService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _currentVersion = GetCurrentVersion();
        }

        public bool IsCheckingForUpdates
        {
            get => _isCheckingForUpdates;
            set
            {
                _isCheckingForUpdates = value;
                OnPropertyChanged();
            }
        }

        public bool UpdateAvailable
        {
            get => _updateAvailable;
            set
            {
                _updateAvailable = value;
                OnPropertyChanged();
            }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                _isDownloading = value;
                OnPropertyChanged();
            }
        }

        public bool IsInstalling
        {
            get => _isInstalling;
            set
            {
                _isInstalling = value;
                OnPropertyChanged();
            }
        }

        public string CurrentVersion
        {
            get => _currentVersion;
            set
            {
                _currentVersion = value;
                OnPropertyChanged();
            }
        }

        public string LatestVersion
        {
            get => _latestVersion;
            set
            {
                _latestVersion = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Check for available updates from the update server
        /// </summary>
        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                IsCheckingForUpdates = true;
                StatusMessage = "Checking for updates...";

                // Default update URLs (can be overridden)
                var updateUrl = "https://raw.githubusercontent.com/UPB-FLL/google-ads-optimizer/main/update-manifest.json";
                var backupUpdateUrl = "https://api.github.com/repos/UPB-FLL/google-ads-optimizer/releases/latest";

                try
                {
                    // Try primary update URL first
                    var response = await _httpClient.GetAsync(updateUrl);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    _updateManifest = JsonSerializer.Deserialize<UpdateManifest>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    // Fallback to GitHub API
                    StatusMessage = "Primary update server unavailable, checking GitHub...";
                    var response = await _httpClient.GetAsync(backupUpdateUrl);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var githubRelease = JsonSerializer.Deserialize<GithubRelease>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _updateManifest = new UpdateManifest
                    {
                        currentVersion = githubRelease.Tag_name.TrimStart('v'),
                        versions = new[]
                        {
                            new UpdateInfo
                            {
                                version = githubRelease.Tag_name.TrimStart('v'),
                                releaseDate = githubRelease.Published_at,
                                msiUrl = githubRelease.Assets?.FirstOrDefault(a => a.Name.EndsWith(".msi"))?.Browser_download_url,
                                releaseNotes = githubRelease.Body?.Split('\n').Take(10).ToArray()
                            }
                        }
                    };
                }

                if (_updateManifest == null || _updateManifest.versions == null || !_updateManifest.versions.Any())
                {
                    StatusMessage = "Unable to retrieve update information";
                    return false;
                }

                // Find the latest version
                var latestUpdate = _updateManifest.versions
                    .OrderByDescending(v => ParseVersion(v.version))
                    .FirstOrDefault();

                if (latestUpdate == null)
                {
                    StatusMessage = "No version information available";
                    return false;
                }

                LatestVersion = latestUpdate.version;

                // Check if update is available
                var current = ParseVersion(_currentVersion);
                var latest = ParseVersion(latestUpdate.version);

                if (latest > current)
                {
                    _availableUpdate = latestUpdate;
                    UpdateAvailable = true;
                    StatusMessage = $"Update available: {_currentVersion} → {latestUpdate.version}";

                    // Notify listeners
                    UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs
                    {
                        CurrentVersion = _currentVersion,
                        LatestVersion = latestUpdate.version,
                        ReleaseNotes = latestUpdate.releaseNotes,
                        IsMandatory = latestUpdate.mandatory,
                        Size = latestUpdate.size
                    });

                    return true;
                }
                else
                {
                    UpdateAvailable = false;
                    StatusMessage = "You have the latest version";
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update check failed: {ex.Message}";
                return false;
            }
            finally
            {
                IsCheckingForUpdates = false;
            }
        }

        /// <summary>
        /// Download the available update
        /// </summary>
        public async Task<string> DownloadUpdateAsync()
        {
            if (_availableUpdate == null)
            {
                throw new InvalidOperationException("No update available to download");
            }

            try
            {
                IsDownloading = true;
                StatusMessage = "Downloading update...";

                var downloadUrl = _availableUpdate.msiUrl;
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    throw new InvalidOperationException("No download URL available");
                }

                // Create temp directory for download
                var tempDir = Path.Combine(Path.GetTempPath(), "GoogleAdsOptimizer", "Updates");
                Directory.CreateDirectory(tempDir);

                var fileName = $"GoogleAdsOptimizer-{_availableUpdate.version}.msi";
                var destinationPath = Path.Combine(tempDir, fileName);

                // Download with progress tracking
                var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var buffer = new byte[8192];
                var bytesRead = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    int read;
                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        bytesRead += read;

                        if (totalBytes > 0)
                        {
                            DownloadProgress = (bytesRead / (double)totalBytes) * 100;
                            StatusMessage = $"Downloading update... {DownloadProgress:F0}%";
                        }
                    }
                }

                StatusMessage = "Download completed successfully";
                return destinationPath;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Download failed: {ex.Message}";
                throw;
            }
            finally
            {
                IsDownloading = false;
                DownloadProgress = 0;
            }
        }

        /// <summary>
        /// Install the downloaded update
        /// </summary>
        public async Task<bool> InstallUpdateAsync(string installerPath)
        {
            if (!File.Exists(installerPath))
            {
                throw new FileNotFoundException("Update installer not found", installerPath);
            }

            try
            {
                IsInstalling = true;
                StatusMessage = "Installing update...";

                // Create a batch script to run the installer
                var scriptPath = Path.Combine(Path.GetTempPath(), "install_update.bat");
                var scriptContent = $@"
@echo off
echo Installing Google Ads Optimizer update...
echo.
echo This will close the application and install the update.
echo The application will restart automatically after installation.
echo.
timeout /t 5 /nobreak
msiexec /i ""{installerPath}"" /quiet /norestart PROMPT=RESTART
start "" ""{GetCurrentExecutablePath()}""

                await File.WriteAllTextAsync(scriptPath, scriptContent);

                // Launch the installer script and exit the application
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = scriptPath,
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };

                System.Diagnostics.Process.Start(processInfo);

                // Shutdown the application
                Application.Current?.Shutdown();

                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Installation failed: {ex.Message}";
                return false;
            }
            finally
            {
                IsInstalling = false;
            }
        }

        /// <summary>
        /// Perform automatic update check and installation
        /// </summary>
        public async Task<bool> AutoUpdateAsync()
        {
            try
            {
                StatusMessage = "Checking for automatic updates...";

                // Check if update is available
                var hasUpdate = await CheckForUpdatesAsync();

                if (hasUpdate && _availableUpdate != null)
                {
                    // If update is mandatory or user has auto-update enabled
                    if (_availableUpdate.mandatory || IsAutoUpdateEnabled())
                    {
                        StatusMessage = "Downloading update automatically...";

                        // Download the update
                        var installerPath = await DownloadUpdateAsync();

                        // Install the update
                        await InstallUpdateAsync(installerPath);

                        return true;
                    }
                    else
                    {
                        StatusMessage = "Update available but not mandatory";
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Auto-update failed: {ex.Message}";
                return false;
            }
        }

        private string GetCurrentVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                return versionInfo.FileVersion ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }

        private string GetCurrentExecutablePath()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                return assembly.Location;
            }
            catch
            {
                return "GoogleAdsOptimizer.exe";
            }
        }

        private Version ParseVersion(string versionString)
        {
            if (string.IsNullOrEmpty(versionString))
                return new Version(0, 0, 0, 0);

            // Clean version string (remove 'v' prefix, etc.)
            versionString = versionString.Trim().TrimStart('v');

            // Handle different version formats
            var parts = versionString.Split('.');
            if (parts.Length >= 3)
            {
                return new Version(
                    int.Parse(parts[0]),
                    int.Parse(parts[1]),
                    int.Parse(parts[2]),
                    parts.Length > 3 ? int.Parse(parts[3]) : 0
                );
            }

            return new Version(versionString);
        }

        private bool IsAutoUpdateEnabled()
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\GoogleAdsOptimizer\Settings");

                if (key != null)
                {
                    var value = key.GetValue("AutoUpdateEnabled");
                    key.Close();

                    return value != null && (int)value == 1;
                }
            }
            catch
            {
                // If registry access fails, default to enabled
                return true;
            }

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Update data models
    public class UpdateManifest
    {
        public string currentVersion { get; set; }
        public string updateUrl { get; set; }
        public string installerUrl { get; set; }
        public string changelog { get; set; }
        public UpdateInfo[] versions { get; set; }
    }

    public class UpdateInfo
    {
        public string version { get; set; }
        public string releaseDate { get; set; }
        public string msiUrl { get; set; }
        public string sha256 { get; set; }
        public double size { get; set; }
        public bool mandatory { get; set; }
        public string[] releaseNotes { get; set; }
    }

    public class GithubRelease
    {
        public string Tag_name { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public string Published_at { get; set; }
        public GithubAsset[] Assets { get; set; }
    }

    public class GithubAsset
    {
        public string Name { get; set; }
        public string Browser_download_url { get; set; }
        public long Size { get; set; }
    }

    public class UpdateAvailableEventArgs : EventArgs
    {
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public string[] ReleaseNotes { get; set; }
        public bool IsMandatory { get; set; }
        public double Size { get; set; }
    }
}