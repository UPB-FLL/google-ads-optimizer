# Google Ads Optimizer 🚀

A comprehensive Windows desktop application for optimizing Google Ads campaigns through AI-powered analysis, ad generation, and Google Ads Editor export capabilities.

## 🎯 Project Overview

This native Windows application helps advertisers maximize their Google Ads ROI through:

- **Performance Analysis**: Deep analysis of campaigns, ad groups, ads, and keywords
- **AI-Powered Insights**: GPT-4 integration for intelligent recommendations
- **Ad Generation**: Create brand-consistent ad copy automatically
- **Editor Export**: Generate Google Ads Editor-compatible CSV files with image support

## ✨ Key Features

### 📊 Campaign Analysis
- Real-time performance metrics (CTR, CPC, CPA, ROAS)
- Identify top-performing and underperforming ads
- Keyword effectiveness scoring
- Budget utilization analysis
- AI-powered optimization recommendations

### 🤖 AI-Powered Optimization
- **GPT-4 Integration**: Generate ad copy that matches your brand voice
- **Brand Research**: Analyze your brand to create consistent messaging
- **Keyword Generation**: Get high-performing keyword suggestions
- **Performance Insights**: Get actionable recommendations from AI analysis

### 📤 Google Ads Editor Export
- **Campaign Export**: Complete campaign data in Editor-compatible format
- **Image Support**: Include image assets with proper manifest files
- **Multiple Formats**: Campaigns, ad groups, ads, keywords, and images
- **Batch Export**: Export multiple campaigns at once
- **Image Handling**: Automatic image manifest creation for Editor import

### 🔧 Technical Features
- Native Windows .NET 8.0 WPF application
- Secure credential storage (Windows Credential Manager)
- Offline analysis capability
- Auto-update functionality
- MSI installer for easy deployment

## 🏗️ Architecture

**Technology Stack**:
- .NET 8.0 WPF (Windows Presentation Foundation)
- Google Ads .NET Library (v20.1.0)
- Azure OpenAI Service (GPT-4)
- MaterialDesign Themes for modern UI

**Core Components**:
- `GoogleAdsService` - Google Ads API integration
- `GPTService` - OpenAI GPT-4 integration
- `CampaignAnalyzer` - Performance analysis engine
- `AdCopyGenerator` - AI ad generation
- `GoogleAdsExportService` - Editor format export

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed technical documentation.

## 🚀 Getting Started

### Prerequisites

- Windows 10/11
- Google Ads API Developer Token
- Google Ads Manager Account
- OpenAI API Key (for AI features)
- .NET 8.0 Runtime (included in installer)

### Installation

1. **Download the installer** (when available)
   - Download `GoogleAdsOptimizer.msi` from releases
   - Run the installer with administrator privileges
   - Follow the installation wizard

2. **First-time setup**
   - Launch Google Ads Optimizer
   - Go to Settings > API Configuration
   - Enter your Google Ads credentials
   - Configure OpenAI API key for AI features
   - Test connection to verify setup

### Configuration

#### Google Ads API Credentials

You'll need:
- **Developer Token**: Get from [Google Ads API Center](https://ads.google.com/aw/apicenter)
- **Client ID & Secret**: Create OAuth 2.0 credentials
- **Refresh Token**: Generate through OAuth flow
- **Customer ID**: Your Google Ads customer ID (XXX-XXX-XXXX format)

#### OpenAI API (Optional)

For AI-powered features:
- **API Key**: Get from [OpenAI Platform](https://platform.openai.com/)
- **Deployment**: Specify GPT-4 deployment (or use default)
- **Endpoint**: Use Azure OpenAI or standard OpenAI

## 📖 Usage

### 1. Connect Your Google Ads Account
```
Settings → API Configuration → Enter credentials → Test Connection
```

### 2. Analyze Campaign Performance
```
Dashboard → Select Campaign → Set Date Range → Analyze
```

### 3. Generate AI Ads
```
AI Ad Generator → Enter Product/Service → Target Audience → Generate Ads
```

### 4. Export to Google Ads Editor
```
Campaign Analysis → Export → Select Format → Download Package
```

### 5. Import into Google Ads Editor
```
Google Ads Editor → File → Import → Select CSV files → Post Changes
```

## 🔒 Security

- API credentials stored securely in Windows Credential Manager
- OAuth 2.0 authentication for Google Ads
- Encrypted local storage for sensitive data
- No data logging or transmission to third parties
- OpenAI API calls contain no PII or sensitive information

## 📝 Export Format

The application generates Google Ads Editor-compatible CSV files:

**Campaigns**: `campaigns.csv`
- Campaign configuration, budget, bid strategy
- Status, dates, targeting settings

**Ad Groups**: `adgroups.csv`
- Ad group structure, bids, types
- Status and targeting information

**Ads**: `ads.csv`
- Expanded text ads with headlines/descriptions
- Final URLs, display URLs, tracking templates
- Image references (prefixed with "Image:")

**Keywords**: `keywords.csv`
- Keyword text, match types, bids
- Quality scores, performance data

**Images**: `images.csv`
- Image manifest with file paths and dimensions
- Format: `ImageName,FilePath,Width,Height`

### Import Order
1. campaigns.csv
2. adgroups.csv
3. ads.csv
4. keywords.csv
5. images.csv (if applicable)

## 🛠️ Development

### Building from source

```bash
# Clone repository
git clone https://github.com/UPB-FLL/google-ads-optimizer.git

# Open solution in Visual Studio 2022
# Requires .NET 8.0 SDK

# Build
dotnet build GoogleAdsOptimizer.sln --configuration Release

# Run
dotnet run --project src/GoogleAdsOptimizer/GoogleAdsOptimizer.csproj
```

### Creating MSI Installer

```bash
# Requires WiX Toolset v3.11+
cd build
candle.exe GoogleAdsOptimizer.wxs
light.exe -out GoogleAdsOptimizer.msi GoogleAdsOptimizer.wixobj
```

## 🗺️ Roadmap

### Current Version (v1.0)
- ✅ Google Ads API integration
- ✅ Campaign performance analysis
- ✅ AI-powered ad generation
- ✅ Google Ads Editor export
- ✅ Image support for display ads
- ✅ Brand research integration

### Planned Features
- ⏳ Automated bid management
- ⏳ A/B testing framework
- ⏳ Competitor analysis
- ⏳ Multi-account management
- ⏳ Scheduled reports
- ⏳ Budget optimization automation

## 🤝 Contributing

Contributions are welcome! Please read our contributing guidelines before submitting PRs.

## 📄 License

MIT License - see LICENSE file for details

## 🆘 Support

- **Documentation**: [ARCHITECTURE.md](ARCHITECTURE.md)
- **Issues**: [GitHub Issues](https://github.com/UPB-FLL/google-ads-optimizer/issues)
- **Discussions**: [GitHub Discussions](https://github.com/UPB-FLL/google-ads-optimizer/discussions)

## 🙏 Acknowledgments

- Google Ads .NET Library by Google
- MaterialDesignInXamlToolkit
- Azure OpenAI Service
- WiX Toolset for installer creation

---

**Note**: This project is actively under development. Features and documentation are subject to change as we approach the v1.0 release.