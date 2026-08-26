using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleAdsOptimizer.Models;
using Newtonsoft.Json;

namespace GoogleAdsOptimizer.Services
{
    using ImageData = Models.ImageAsset;
    /// <summary>
    /// Service for exporting data in Google Ads Editor-compatible format
    /// </summary>
    public class GoogleAdsExportService
    {
        private const string CSV_DELIMITER = ",";
        private const string IMAGE_PREFIX = "Image:";

        /// <summary>
        /// Generate Google Ads Editor-compatible CSV export for campaigns
        /// </summary>
        public async Task<string> ExportCampaignsToEditorFormat(IEnumerable<CampaignExportData> campaigns)
        {
            var csv = new StringBuilder();

            // Header row - Google Ads Editor format
            csv.AppendLine("Campaign,Status,Daily Budget,Start Date,End Date," +
                         "Advertising Channel Type,Target CPA,Target ROAS," +
                         "Bid Strategy Type, Campaign Type");

            foreach (var campaign in campaigns)
            {
                var row = new List<string>
                {
                    EscapeCsvField(campaign.Name),
                    MapStatusToEditor(campaign.Status),
                    campaign.DailyBudget?.ToString("F2") ?? "",
                    FormatDate(campaign.StartDate),
                    FormatDate(campaign.EndDate),
                    MapChannelType(campaign.AdvertisingChannelType),
                    campaign.TargetCpa?.ToString("F2") ?? "",
                    campaign.TargetRoas?.ToString("F2") ?? "",
                    campaign.BiddingStrategyType,
                    campaign.CampaignType.ToString()
                };

                csv.AppendLine(string.Join(CSV_DELIMITER, row));
            }

            return csv.ToString();
        }

        /// <summary>
        /// Generate Google Ads Editor-compatible CSV export for ad groups
        /// </summary>
        public async Task<string> ExportAdGroupsToEditorFormat(IEnumerable<AdGroupExportData> adGroups)
        {
            var csv = new StringBuilder();

            csv.AppendLine("Campaign,Ad Group,Status,Default Bid,Ad Group Type," +
                         "CPA Bid,ROAS Bid");

            foreach (var adGroup in adGroups)
            {
                var row = new List<string>
                {
                    EscapeCsvField(adGroup.CampaignName),
                    EscapeCsvField(adGroup.Name),
                    MapStatusToEditor(adGroup.Status),
                    adGroup.DefaultBid?.ToString("F2") ?? "",
                    adGroup.AdGroupType.ToString(),
                    adGroup.CpaBid?.ToString("F2") ?? "",
                    adGroup.RoasBid?.ToString("F2") ?? ""
                };

                csv.AppendLine(string.Join(CSV_DELIMITER, row));
            }

            return csv.ToString();
        }

        /// <summary>
        /// Generate Google Ads Editor-compatible CSV export for text ads
        /// </summary>
        public async Task<string> ExportTextAdsToEditorFormat(IEnumerable<TextAdExportData> ads)
        {
            var csv = new StringBuilder();

            csv.AppendLine("Campaign,Ad Group,Ad,Status,Headline 1,Headline 2,Headline 3," +
                         "Description,Description 2,Display URL,Final URL,Tracking Template," +
                         "Custom Parameters,Image Names");

            foreach (var ad in ads)
            {
                var row = new List<string>
                {
                    EscapeCsvField(ad.CampaignName),
                    EscapeCsvField(ad.AdGroupName),
                    EscapeCsvField(ad.Name),
                    MapStatusToEditor(ad.Status),
                    EscapeCsvField(ad.Headline1),
                    EscapeCsvField(ad.Headline2),
                    EscapeCsvField(ad.Headline3 ?? ""),
                    EscapeCsvField(ad.Description),
                    EscapeCsvField(ad.Description2 ?? ""),
                    EscapeCsvField(ad.DisplayUrl ?? ""),
                    EscapeCsvField(ad.FinalUrl),
                    EscapeCsvField(ad.TrackingTemplate ?? ""),
                    EscapeJsonField(ad.CustomParameters),
                    string.Join(";", ad.ImageNames.Select(n => IMAGE_PREFIX + n))
                };

                csv.AppendLine(string.Join(CSV_DELIMITER, row));
            }

            return csv.ToString();
        }

        /// <summary>
        /// Generate Google Ads Editor-compatible CSV export for keywords
        /// </summary>
        public async Task<string> ExportKeywordsToEditorFormat(IEnumerable<KeywordExportData> keywords)
        {
            var csv = new StringBuilder();

            csv.AppendLine("Campaign,Ad Group,Keyword,Status,Match Type,CPC Bid," +
                         "First Page Bid,Quality Score");

            foreach (var keyword in keywords)
            {
                var row = new List<string>
                {
                    EscapeCsvField(keyword.CampaignName),
                    EscapeCsvField(keyword.AdGroupName),
                    EscapeCsvField(keyword.Text),
                    MapStatusToEditor(keyword.Status),
                    keyword.MatchType.ToString().ToLower(),
                    keyword.CpcBid?.ToString("F2") ?? "",
                    keyword.FirstPageBid?.ToString("F2") ?? "",
                    keyword.QualityScore?.ToString() ?? ""
                };

                csv.AppendLine(string.Join(CSV_DELIMITER, row));
            }

            return csv.ToString();
        }

        /// <summary>
        /// Create image manifest file for Google Ads Editor
        /// </summary>
        public async Task<string> CreateImageManifest(IEnumerable<ImageData> images)
        {
            var manifest = new StringBuilder();
            manifest.AppendLine("# Image Manifest for Google Ads Editor");
            manifest.AppendLine("# Format: ImageName,FilePath,Width,Height");
            manifest.AppendLine("# Images should be placed in the same folder as this manifest");

            foreach (var image in images)
            {
                manifest.AppendLine($"{image.Name},{image.FilePath},{image.Width},{image.Height}");
            }

            return manifest.ToString();
        }

        /// <summary>
        /// Export complete campaign package with all components
        /// </summary>
        public async Task<GoogleAdsEditorPackage> CreateEditorPackage(CampaignPackageData package)
        {
            var exportPackage = new GoogleAdsEditorPackage
            {
                ExportDate = DateTime.Now,
                PackageName = $"GoogleAdsExport_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            // Generate all CSV files
            exportPackage.CampaignsCsv = await ExportCampaignsToEditorFormat(package.Campaigns);
            exportPackage.AdGroupsCsv = await ExportAdGroupsToEditorFormat(package.AdGroups);
            exportPackage.AdsCsv = await ExportTextAdsToEditorFormat(package.TextAds);
            exportPackage.KeywordsCsv = await ExportKeywordsToEditorFormat(package.Keywords);

            // Handle images
            if (package.Images?.Any() == true)
            {
                exportPackage.ImagesCsv = await CreateImageManifest(package.Images);
                exportPackage.HasImages = true;
            }

            // Create package metadata
            exportPackage.Metadata = new PackageMetadata
            {
                TotalCampaigns = package.Campaigns.Count(),
                TotalAdGroups = package.AdGroups.Count(),
                TotalAds = package.TextAds.Count(),
                TotalKeywords = package.Keywords.Count(),
                TotalImages = package.Images?.Count() ?? 0,
                GeneratedBy = "Google Ads Optimizer v1.0.0"
            };

            return exportPackage;
        }

        /// <summary>
        /// Save package to disk with proper folder structure
        /// </summary>
        public async Task<string> SavePackageToDisk(GoogleAdsEditorPackage package, string outputDirectory)
        {
            var packagePath = Path.Combine(outputDirectory, package.PackageName);
            Directory.CreateDirectory(packagePath);

            // Save CSV files
            File.WriteAllText(Path.Combine(packagePath, "campaigns.csv"), package.CampaignsCsv, Encoding.UTF8);
            File.WriteAllText(Path.Combine(packagePath, "adgroups.csv"), package.AdGroupsCsv, Encoding.UTF8);
            File.WriteAllText(Path.Combine(packagePath, "ads.csv"), package.AdsCsv, Encoding.UTF8);
            File.WriteAllText(Path.Combine(packagePath, "keywords.csv"), package.KeywordsCsv, Encoding.UTF8);

            // Save images manifest if present
            if (package.HasImages)
            {
                File.WriteAllText(Path.Combine(packagePath, "images.csv"), package.ImagesCsv, Encoding.UTF8);
            }

            // Save metadata
            var metadataJson = JsonConvert.SerializeObject(package.Metadata, Formatting.Indented);
            File.WriteAllText(Path.Combine(packagePath, "metadata.json"), metadataJson, Encoding.UTF8);

            // Create README
            var readme = CreateReadme(package);
            File.WriteAllText(Path.Combine(packagePath, "README.txt"), readme, Encoding.UTF8);

            return packagePath;
        }

        private string CreateReadme(GoogleAdsEditorPackage package)
        {
            return $@"Google Ads Editor Export Package
Generated: {package.ExportDate:yyyy-MM-dd HH:mm:ss}
Generated by: {package.Metadata.GeneratedBy}

Package Contents:
- {package.Metadata.TotalCampaigns} Campaign(s)
- {package.Metadata.TotalAdGroups} Ad Group(s)
- {package.Metadata.TotalAds} Ad(s)
- {package.Metadata.TotalKeywords} Keyword(s)
- {package.Metadata.TotalImages} Image(s)

How to Import:
1. Open Google Ads Editor
2. File > Import > Select CSV files
3. Import in this order:
   a. campaigns.csv
   b. adgroups.csv
   c. ads.csv
   d. keywords.csv
4. Review and make changes
5. Post changes to Google Ads

Note: Make sure images are available in the same folder before importing ads.

For support, visit: https://github.com/UPB-FLL/google-ads-optimizer";
        }

        // Helper methods
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";

            if (field.Contains(CSV_DELIMITER) || field.Contains("\"") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        private string EscapeJsonField(object obj)
        {
            if (obj == null) return "";
            return EscapeCsvField(JsonConvert.SerializeObject(obj));
        }

        private string MapStatusToEditor(CampaignStatus status)
        {
            return status switch
            {
                CampaignStatus.Enabled => "Enabled",
                CampaignStatus.Paused => "Paused",
                CampaignStatus.Removed => "Removed",
                _ => "Unknown"
            };
        }

        private string FormatDate(DateTime? date)
        {
            return date?.ToString("yyyy-MM-dd") ?? "";
        }

        private string MapChannelType(AdvertisingChannelType type)
        {
            return type switch
            {
                AdvertisingChannelType.Search => "Search",
                AdvertisingChannelType.Display => "Display",
                AdvertisingChannelType.Shopping => "Shopping",
                AdvertisingChannelType.Video => "Video",
                AdvertisingChannelType.MultiChannel => "MultiChannel",
                _ => "Search" // Default
            };
        }
    }

    // Data models for export
    public class GoogleAdsEditorPackage
    {
        public string PackageName { get; set; }
        public DateTime ExportDate { get; set; }
        public string CampaignsCsv { get; set; }
        public string AdGroupsCsv { get; set; }
        public string AdsCsv { get; set; }
        public string KeywordsCsv { get; set; }
        public string ImagesCsv { get; set; }
        public bool HasImages { get; set; }
        public PackageMetadata Metadata { get; set; }
    }

    public class PackageMetadata
    {
        public int TotalCampaigns { get; set; }
        public int TotalAdGroups { get; set; }
        public int TotalAds { get; set; }
        public int TotalKeywords { get; set; }
        public int TotalImages { get; set; }
        public string GeneratedBy { get; set; }
    }

    public class CampaignPackageData
    {
        public IEnumerable<CampaignExportData> Campaigns { get; set; }
        public IEnumerable<AdGroupExportData> AdGroups { get; set; }
        public IEnumerable<TextAdExportData> TextAds { get; set; }
        public IEnumerable<KeywordExportData> Keywords { get; set; }
        public IEnumerable<ImageData> Images { get; set; }
    }
}