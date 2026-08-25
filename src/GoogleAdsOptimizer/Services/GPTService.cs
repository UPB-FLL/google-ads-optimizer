using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Newtonsoft.Json;

namespace GoogleAdsOptimizer.Services
{
    /// <summary>
    /// Service for integrating with OpenAI GPT API for ad generation and analysis
    /// </summary>
    public class GPTService : IDisposable
    {
        private OpenAIClient _openAIClient;
        private string _deploymentName;
        private bool _isDisposed;

        /// <summary>
        /// Initialize the GPT service with Azure OpenAI or OpenAI API
        /// </summary>
        public async Task InitializeAsync(string apiKey, string deploymentName = "gpt-4", bool isAzure = false, string endpoint = null)
        {
            try
            {
                if (isAzure && !string.IsNullOrEmpty(endpoint))
                {
                    _openAIClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
                }
                else
                {
                    _openAIClient = new OpenAIClient(apiKey);
                }

                _deploymentName = deploymentName;

                // Test connection with a simple completion
                await TestConnectionAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"GPT service initialization failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Test the GPT connection
        /// </summary>
        private async Task TestConnectionAsync()
        {
            var options = new ChatCompletionsOptions
            {
                Messages = { new ChatMessage(ChatRole.User, "Hello, are you working?") },
                MaxTokens = 10
            };

            await _openAIClient.GetChatCompletionsAsync(_deploymentName, options);
        }

        /// <summary>
        /// Generate ad copy using GPT based on brand information and campaign goals
        /// </summary>
        public async Task<List<GeneratedAd>> GenerateAdsAsync(AdGenerationRequest request)
        {
            var prompt = BuildAdGenerationPrompt(request);
            var ads = new List<GeneratedAd>();

            try
            {
                var options = new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatMessage(ChatRole.System, GetSystemPrompt()),
                        new ChatMessage(ChatRole.User, prompt)
                    },
                    MaxTokens = 2000,
                    Temperature = 0.8,
                    NucleusSamplingFactor = 0.9,
                    FrequencyPenalty = 0.5,
                    PresencePenalty = 0.5
                };

                var response = await _openAIClient.GetChatCompletionsAsync(_deploymentName, options);
                var generatedText = response.Value.Choices.First().Message.Content;

                // Parse the generated ads from the response
                ads = ParseGeneratedAds(generatedText, request);

                // Generate variations if requested
                if (request.NumberOfVariations > 1)
                {
                    for (int i = 1; i < request.NumberOfVariations; i++)
                    {
                        options.Temperature = 0.7 + (i * 0.1); // Vary temperature for diversity
                        var variationResponse = await _openAIClient.GetChatCompletionsAsync(_deploymentName, options);
                        var variationText = variationResponse.Value.Choices.First().Message.Content;
                        var variationAds = ParseGeneratedAds(variationText, request);
                        ads.AddRange(variationAds);
                    }
                }

                return ads.Take(request.NumberOfVariations * 3).ToList(); // Return up to 3 ads per variation
            }
            catch (Exception ex)
            {
                throw new Exception($"Ad generation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Analyze campaign performance and provide optimization suggestions
        /// </summary>
        public async Task<CampaignAnalysis> AnalyzeCampaignPerformanceAsync(CampaignPerformanceData data)
        {
            var prompt = BuildAnalysisPrompt(data);

            try
            {
                var options = new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatMessage(ChatRole.System, GetAnalysisSystemPrompt()),
                        new ChatMessage(ChatRole.User, prompt)
                    },
                    MaxTokens = 3000,
                    Temperature = 0.3, // Lower temperature for more analytical responses
                    ResponseFormat = ChatCompletionsResponseFormat.JsonObject
                };

                var response = await _openAIClient.GetChatCompletionsAsync(_deploymentName, options);
                var analysisJson = response.Value.Choices.First().Message.Content;

                return JsonConvert.DeserializeObject<CampaignAnalysis>(analysisJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"Campaign analysis failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Research brand information to generate brand-consistent ads
        /// </summary>
        public async Task<BrandResearch> ResearchBrandAsync(string brandName, string industry, string website = null)
        {
            var prompt = $@"Research the brand '{brandName}' in the {industry} industry.

Please analyze and provide:
1. Brand voice and personality (formal, casual, professional, playful, etc.)
2. Key brand values and messaging themes
3. Typical customer demographics and pain points
4. Competitive positioning
5. Suggested tone for marketing materials

{(string.IsNullOrEmpty(website) ? "" : $"Website context: {website}")}

Provide a structured analysis that will help create brand-consistent advertising copy.";

            try
            {
                var options = new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatMessage(ChatRole.System, "You are a brand research expert. Provide detailed, actionable brand analysis."),
                        new ChatMessage(ChatRole.User, prompt)
                    },
                    MaxTokens = 2500,
                    Temperature = 0.4
                };

                var response = await _openAIClient.GetChatCompletionsAsync(_deploymentName, options);
                var researchText = response.Value.Choices.First().Message.Content;

                return ParseBrandResearch(researchText);
            }
            catch (Exception ex)
            {
                throw new Exception($"Brand research failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate keyword suggestions based on campaign data and brand research
        /// </summary>
        public async Task<List<KeywordSuggestion>> GenerateKeywordsAsync(KeywordGenerationRequest request)
        {
            var prompt = BuildKeywordGenerationPrompt(request);

            try
            {
                var options = new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatMessage(ChatRole.System, "You are a Google Ads keyword expert. Generate high-performing keywords with proper match types."),
                        new ChatMessage(ChatRole.User, prompt)
                    },
                    MaxTokens = 2000,
                    Temperature = 0.6
                };

                var response = await _openAIClient.GetChatCompletionsAsync(_deploymentName, options);
                var keywordsText = response.Value.Choices.First().Message.Content;

                return ParseKeywordSuggestions(keywordsText);
            }
            catch (Exception ex)
            {
                throw new Exception($"Keyword generation failed: {ex.Message}", ex);
            }
        }

        // Private helper methods

        private string GetSystemPrompt()
        {
            return @"You are an expert Google Ads copywriter with deep knowledge of:
- Writing compelling, high-converting ad copy
- Following Google Ads policies and best practices
- Creating brand-consistent messaging
- Optimizing for different campaign objectives (conversions, clicks, brand awareness)

Your ads should:
- Be attention-grabbing and relevant
- Include clear value propositions
- Have strong calls-to-action
- Follow Google Ads character limits (headlines: 30 chars, descriptions: 90 chars)
- Be consistent with the provided brand information
- Highlight unique selling points

Always return ads in a structured format that can be easily parsed.";
        }

        private string GetAnalysisSystemPrompt()
        {
            return @"You are a Google Ads performance analyst. Analyze campaign data and provide:
1. Clear identification of what's working well
2. Specific issues that need attention
3. Actionable optimization recommendations
4. Prioritized suggestions based on impact

Be specific, data-driven, and practical. Focus on actionable insights.";
        }

        private string BuildAdGenerationPrompt(AdGenerationRequest request)
        {
            var prompt = $@"Generate Google Ads copy for:

Campaign: {request.CampaignName}
Product/Service: {request.ProductService}
Target Audience: {request.TargetAudience}
Key Benefits: {string.Join(", ", request.KeyBenefits)}
Campaign Goal: {request.CampaignGoal}

Brand Information:
- Brand Name: {request.BrandName}
- Brand Voice: {request.BrandVoice}
- Industry: {request.Industry}

Requirements:
- Generate {request.NumberOfVariations} different ad variations
- Each ad needs: 3 headlines (30 chars max), 2 descriptions (90 chars max)
- Include strong calls-to-action
- Highlight key benefits
- Match brand voice consistently
- Follow Google Ads policies

Please provide the ads in a structured format with clear labeling.";

            if (request.CompetitorInfo?.Any() == true)
            {
                prompt += $"\n\nCompetitor Information: {string.Join(", ", request.CompetitorInfo)}";
            }

            if (request.ExistingAds?.Any() == true)
            {
                prompt += $"\n\nFor context, here are some existing ads:\n{string.Join("\n", request.ExistingAds)}";
            }

            return prompt;
        }

        private string BuildAnalysisPrompt(CampaignPerformanceData data)
        {
            return $@"Analyze this Google Ads campaign performance:

Campaign: {data.CampaignName}
Period: {data.StartDate:yyyy-MM-dd} to {data.EndDate:yyyy-MM-dd}

Key Metrics:
- Impressions: {data.Impressions:N0}
- Clicks: {data.Clicks:N0}
- Cost: ${data.Cost:N2}
- Conversions: {data.Conversions:N1}
- CTR: {data.ClickThroughRate:F2}%
- CPC: ${data.CostPerClick:F2}
- CPA: ${data.CostPerAcquisition:F2}
- ROAS: {data.ReturnOnAdSpend:F2}

Top Performing Ads:
{string.Join("\n", data.TopAds.Select(ad => $"- {ad.Headline}: CTR {ad.CTR:F2}%, {ad.Conversions} conversions"))}

Underperforming Ads:
{string.Join("\n", data.UnderperformingAds.Select(ad => $"- {ad.Headline}: CTR {ad.CTR:F2}%, {ad.Conversions} conversions"))}

Budget: ${data.DailyBudget:F2}/day
Bid Strategy: {data.BidStrategy}

Please provide a detailed analysis covering:
1. What's working well (specific metrics and ads)
2. What needs improvement (with specific issues)
3. Prioritized recommendations (high/medium/low impact)
4. Suggested budget allocations
5. Ad copy suggestions

Return as JSON with this structure:
{{
  ""strengths"": [""specific strengths""],
  ""issues"": [""specific issues""],
  ""recommendations"": [
    {{""priority"": ""high"", ""action"": ""specific action"", ""impact"": ""expected impact""}},
    {{""priority"": ""medium"", ""action"": ""specific action"", ""impact"": ""expected impact""}}
  ],
  ""budgetSuggestions"": {{""allocation"": ""suggestion"", ""reasoning"": ""why""}},
  ""adSuggestions"": [""specific ad improvements""]
}}";
        }

        private string BuildKeywordGenerationPrompt(KeywordGenerationRequest request)
        {
            return $@"Generate {request.Count} keyword suggestions for:

Product/Service: {request.ProductService}
Target Audience: {request.TargetAudience}
Brand: {request.BrandName}
Industry: {request.Industry}

Current Keywords: {string.Join(", ", request.ExistingKeywords.Take(10))}
Competitor Keywords: {string.Join(", ", request.CompetitorKeywords.Take(5))}

Requirements:
- Mix of broad, phrase, and exact match types
- High commercial intent
- Relevance to product/service
- Different match types for core terms
- Long-tail variations

Format each as: keyword text [match type]
Prioritize by relevance and commercial intent.";
        }

        private List<GeneratedAd> ParseGeneratedAds(string generatedText, AdGenerationRequest request)
        {
            var ads = new List<GeneratedAd>();

            try
            {
                // Parse the GPT response - assuming it returns structured text
                var lines = generatedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var currentAd = new GeneratedAd { CampaignName = request.CampaignName };

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("Ad #") || trimmedLine.StartsWith("Variation"))
                    {
                        if (currentAd.HasContent())
                        {
                            ads.Add(currentAd);
                            currentAd = new GeneratedAd { CampaignName = request.CampaignName };
                        }
                    }
                    else if (trimmedLine.StartsWith("Headline 1:") || trimmedLine.StartsWith("H1:"))
                    {
                        currentAd.Headline1 = ExtractContent(trimmedLine);
                    }
                    else if (trimmedLine.StartsWith("Headline 2:") || trimmedLine.StartsWith("H2:"))
                    {
                        currentAd.Headline2 = ExtractContent(trimmedLine);
                    }
                    else if (trimmedLine.StartsWith("Headline 3:") || trimmedLine.StartsWith("H3:"))
                    {
                        currentAd.Headline3 = ExtractContent(trimmedLine);
                    }
                    else if (trimmedLine.StartsWith("Description:") || trimmedLine.StartsWith("D1:"))
                    {
                        currentAd.Description = ExtractContent(trimmedLine);
                    }
                    else if (trimmedLine.StartsWith("Description 2:") || trimmedLine.StartsWith("D2:"))
                    {
                        currentAd.Description2 = ExtractContent(trimmedLine);
                    }
                }

                if (currentAd.HasContent())
                {
                    ads.Add(currentAd);
                }
            }
            catch (Exception ex)
            {
                // If parsing fails, create a fallback ad
                ads.Add(new GeneratedAd
                {
                    CampaignName = request.CampaignName,
                    Headline1 = "Professional Service",
                    Headline2 = request.ProductService,
                    Headline3 = "Get Started Today",
                    Description = $"Quality {request.ProductService} for {request.TargetAudience}",
                    Description2 = "Call to action - Learn more now"
                });
            }

            return ads;
        }

        private BrandResearch ParseBrandResearch(string researchText)
        {
            // Simple parsing - in production, you'd want more sophisticated JSON parsing
            return new BrandResearch
            {
                BrandVoice = ExtractSection(researchText, "Brand Voice"),
                KeyValues = ExtractSection(researchText, "Key Values"),
                TargetDemographics = ExtractSection(researchText, "Target Demographics"),
                CompetitivePositioning = ExtractSection(researchText, "Competitive Positioning"),
                RecommendedTone = ExtractSection(researchText, "Recommended Tone"),
                RawAnalysis = researchText
            };
        }

        private List<KeywordSuggestion> ParseKeywordSuggestions(string keywordsText)
        {
            var suggestions = new List<KeywordSuggestion>();
            var lines = keywordsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                // Parse format: "keyword text [match type]"
                var matchStart = trimmedLine.LastIndexOf('[');
                var matchEnd = trimmedLine.LastIndexOf(']');

                if (matchStart > 0 && matchEnd > matchStart)
                {
                    var keyword = trimmedLine.Substring(0, matchStart).Trim();
                    var matchTypeStr = trimmedLine.Substring(matchStart + 1, matchEnd - matchStart - 1);

                    suggestions.Add(new KeywordSuggestion
                    {
                        Text = keyword,
                        MatchType = ParseMatchType(matchTypeStr),
                        Priority = CalculateKeywordPriority(keyword, matchTypeStr)
                    });
                }
            }

            return suggestions;
        }

        private string ExtractContent(string line)
        {
            var colonIndex = line.IndexOf(':');
            return colonIndex >= 0 ? line.Substring(colonIndex + 1).Trim() : line.Trim();
        }

        private string ExtractSection(string text, string sectionName)
        {
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(sectionName, StringComparison.OrdinalIgnoreCase))
                {
                    var result = lines[i].Substring(lines[i].IndexOf(':') + 1).Trim();
                    // Get next few lines if they're part of the same section
                    for (int j = i + 1; j < Math.Min(i + 3, lines.Length); j++)
                    {
                        if (!string.IsNullOrWhiteSpace(lines[j]) && !lines[j].Contains(':'))
                        {
                            result += " " + lines[j].Trim();
                        }
                        else
                        {
                            break;
                        }
                    }
                    return result;
                }
            }
            return "";
        }

        private KeywordMatchType ParseMatchType(string matchTypeStr)
        {
            return matchTypeStr.ToLower() switch
            {
                "broad" => KeywordMatchType.Broad,
                "phrase" => KeywordMatchType.Phrase,
                "exact" => KeywordMatchType.Exact,
                _ => KeywordMatchType.Phrase
            };
        }

        private int CalculateKeywordPriority(string keyword, string matchTypeStr)
        {
            // Simple priority calculation based on keyword characteristics
            var priority = 50;

            if (keyword.Split(' ').Length >= 3) priority += 20; // Long-tail bonus
            if (matchTypeStr.ToLower() == "exact") priority += 15;
            if (keyword.ToLower().Contains("best") || keyword.ToLower().Contains("top")) priority += 10;

            return Math.Min(100, priority);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _openAIClient?.Dispose();
                _isDisposed = true;
            }
        }
    }

    // Request/Response models
    public class AdGenerationRequest
    {
        public string CampaignName { get; set; }
        public string ProductService { get; set; }
        public string TargetAudience { get; set; }
        public List<string> KeyBenefits { get; set; }
        public string CampaignGoal { get; set; }
        public string BrandName { get; set; }
        public string BrandVoice { get; set; }
        public string Industry { get; set; }
        public List<string> CompetitorInfo { get; set; }
        public List<string> ExistingAds { get; set; }
        public int NumberOfVariations { get; set; } = 3;
    }

    public class GeneratedAd
    {
        public string CampaignName { get; set; }
        public string Headline1 { get; set; }
        public string Headline2 { get; set; }
        public string Headline3 { get; set; }
        public string Description { get; set; }
        public string Description2 { get; set; }
        public string DisplayUrl { get; set; }
        public string FinalUrl { get; set; }
        public List<string> ImageNames { get; set; } = new List<string>();

        public bool HasContent()
        {
            return !string.IsNullOrEmpty(Headline1) && !string.IsNullOrEmpty(Headline2);
        }
    }

    public class CampaignPerformanceData
    {
        public string CampaignName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long Impressions { get; set; }
        public long Clicks { get; set; }
        public double Cost { get; set; }
        public double Conversions { get; set; }
        public double ClickThroughRate { get; set; }
        public double CostPerClick { get; set; }
        public double CostPerAcquisition { get; set; }
        public double ReturnOnAdSpend { get; set; }
        public double DailyBudget { get; set; }
        public string BidStrategy { get; set; }
        public List<AdPerformanceData> TopAds { get; set; }
        public List<AdPerformanceData> UnderperformingAds { get; set; }
    }

    public class AdPerformanceData
    {
        public string Headline { get; set; }
        public double CTR { get; set; }
        public double Conversions { get; set; }
        public double Cost { get; set; }
    }

    public class CampaignAnalysis
    {
        public List<string> Strengths { get; set; }
        public List<string> Issues { get; set; }
        public List<Recommendation> Recommendations { get; set; }
        public BudgetSuggestion BudgetSuggestions { get; set; }
        public List<string> AdSuggestions { get; set; }
    }

    public class Recommendation
    {
        public string Priority { get; set; }
        public string Action { get; set; }
        public string Impact { get; set; }
    }

    public class BudgetSuggestion
    {
        public string Allocation { get; set; }
        public string Reasoning { get; set; }
    }

    public class BrandResearch
    {
        public string BrandVoice { get; set; }
        public string KeyValues { get; set; }
        public string TargetDemographics { get; set; }
        public string CompetitivePositioning { get; set; }
        public string RecommendedTone { get; set; }
        public string RawAnalysis { get; set; }
    }

    public class KeywordGenerationRequest
    {
        public string ProductService { get; set; }
        public string TargetAudience { get; set; }
        public string BrandName { get; set; }
        public string Industry { get; set; }
        public List<string> ExistingKeywords { get; set; }
        public List<string> CompetitorKeywords { get; set; }
        public int Count { get; set; } = 20;
    }

    public class KeywordSuggestion
    {
        public string Text { get; set; }
        public KeywordMatchType MatchType { get; set; }
        public int Priority { get; set; }
        public double? SuggestedBid { get; set; }
    }

    public enum KeywordMatchType
    {
        Broad,
        Phrase,
        Exact
    }
}