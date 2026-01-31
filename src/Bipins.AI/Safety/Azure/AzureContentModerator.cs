using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Bipins.AI.Safety.Azure;

/// <summary>
/// Azure Cognitive Services Content Moderator implementation.
/// </summary>
public class AzureContentModerator : IContentModerator
{
    private readonly AzureContentModeratorOptions _options;
    private readonly ILogger<AzureContentModerator>? _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureContentModerator"/> class.
    /// </summary>
    public AzureContentModerator(
        IOptions<AzureContentModeratorOptions> options,
        ILogger<AzureContentModerator>? logger = null,
        HttpClient? httpClient = null)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        
        if (string.IsNullOrEmpty(_options.Endpoint))
        {
            throw new InvalidOperationException("Azure Content Moderator endpoint is required.");
        }

        if (string.IsNullOrEmpty(_options.SubscriptionKey))
        {
            throw new InvalidOperationException("Azure Content Moderator subscription key is required.");
        }

        _httpClient.BaseAddress = new Uri(_options.Endpoint);
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _options.SubscriptionKey);
    }

    /// <inheritdoc />
    public async Task<ModerationResult> ModerateAsync(
        string content, 
        string contentType = "text/plain", 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new ModerationResult(
                IsSafe: true,
                SafetyInfo: new Core.Models.SafetyInfo(Flagged: false),
                Violations: Array.Empty<SafetyViolation>());
        }

        try
        {
            var requestUri = $"/contentmoderator/moderate/v1.0/ProcessText/Screen";
            
            var requestContent = new StringContent(content, System.Text.Encoding.UTF8, contentType);
            requestContent.Headers.Add("PII", _options.DetectPII ? "true" : "false");
            requestContent.Headers.Add("classify", "true");

            var response = await _httpClient.PostAsync(requestUri, requestContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var jsonDoc = JsonDocument.Parse(responseJson);
            var root = jsonDoc.RootElement;

            var flagged = root.TryGetProperty("Terms", out var termsProp) && termsProp.ValueKind != JsonValueKind.Null
                       || root.TryGetProperty("Classification", out var classificationProp) 
                          && classificationProp.TryGetProperty("ReviewRecommended", out var reviewProp) 
                          && reviewProp.GetBoolean();

            var violations = new List<SafetyViolation>();
            var categories = new Dictionary<string, bool>();

            // Parse Terms (profanity, etc.)
            if (root.TryGetProperty("Terms", out var terms) && terms.ValueKind != JsonValueKind.Array)
            {
                foreach (var term in terms.EnumerateArray())
                {
                    var termValue = term.TryGetProperty("Term", out var termProp) ? termProp.GetString() : null;
                    var index = term.TryGetProperty("Index", out var indexProp) ? indexProp.GetInt32() : (int?)null;
                    
                    violations.Add(new SafetyViolation(
                        Category: SafetyCategory.Profanity,
                        Severity: SafetySeverity.Medium,
                        Confidence: 0.8,
                        StartIndex: index,
                        EndIndex: index.HasValue && termValue != null ? index.Value + termValue.Length : null,
                        Reason: $"Profanity detected: {termValue}"));
                    
                    categories["profanity"] = true;
                }
            }

            // Parse Classification
            if (root.TryGetProperty("Classification", out var classification))
            {
                if (classification.TryGetProperty("ReviewRecommended", out var reviewRecommended) && reviewRecommended.GetBoolean())
                {
                    var category1 = classification.TryGetProperty("Category1", out var cat1) ? cat1.GetProperty("Score").GetDouble() : 0.0;
                    var category2 = classification.TryGetProperty("Category2", out var cat2) ? cat2.GetProperty("Score").GetDouble() : 0.0;
                    var category3 = classification.TryGetProperty("Category3", out var cat3) ? cat3.GetProperty("Score").GetDouble() : 0.0;

                    if (category1 > _options.MinimumConfidence)
                    {
                        violations.Add(new SafetyViolation(
                            Category: SafetyCategory.Sexual,
                            Severity: DetermineSeverity(category1),
                            Confidence: category1,
                            Reason: "Sexually explicit content detected"));
                        categories["sexual"] = true;
                    }

                    if (category2 > _options.MinimumConfidence)
                    {
                        violations.Add(new SafetyViolation(
                            Category: SafetyCategory.Hate,
                            Severity: DetermineSeverity(category2),
                            Confidence: category2,
                            Reason: "Hate speech detected"));
                        categories["hate"] = true;
                    }

                    if (category3 > _options.MinimumConfidence)
                    {
                        violations.Add(new SafetyViolation(
                            Category: SafetyCategory.Violence,
                            Severity: DetermineSeverity(category3),
                            Confidence: category3,
                            Reason: "Violent content detected"));
                        categories["violence"] = true;
                    }
                }
            }

            // Parse PII if detected
            if (_options.DetectPII && root.TryGetProperty("PII", out var pii))
            {
                if (pii.TryGetProperty("Email", out var email) && email.GetArrayLength() > 0
                    || pii.TryGetProperty("IPA", out var ipa) && ipa.GetArrayLength() > 0
                    || pii.TryGetProperty("Phone", out var phone) && phone.GetArrayLength() > 0
                    || pii.TryGetProperty("Address", out var address) && address.GetArrayLength() > 0)
                {
                    violations.Add(new SafetyViolation(
                        Category: SafetyCategory.PII,
                        Severity: SafetySeverity.Medium,
                        Confidence: 0.9,
                        Reason: "Personally Identifiable Information detected"));
                    categories["pii"] = true;
                }
            }

            var isSafe = !flagged && violations.Count == 0;
            var maxConfidence = violations.Count > 0 ? violations.Max(v => v.Confidence) : 1.0;

            _logger?.LogDebug(
                "Content moderation result: Safe={IsSafe}, Violations={ViolationCount}, Confidence={Confidence}",
                isSafe, violations.Count, maxConfidence);

            return new ModerationResult(
                IsSafe: isSafe,
                SafetyInfo: new Core.Models.SafetyInfo(Flagged: flagged, Categories: categories),
                Violations: violations,
                Confidence: maxConfidence);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during content moderation");
            
            // Fail open or closed based on configuration
            if (_options.FailClosed)
            {
                throw;
            }

            // Fail open - assume safe if moderation fails
            return new ModerationResult(
                IsSafe: true,
                SafetyInfo: new Core.Models.SafetyInfo(Flagged: false),
                Violations: Array.Empty<SafetyViolation>(),
                Confidence: 0.0);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsSafeAsync(
        string content, 
        string contentType = "text/plain", 
        CancellationToken cancellationToken = default)
    {
        var result = await ModerateAsync(content, contentType, cancellationToken);
        return result.IsSafe;
    }

    private static SafetySeverity DetermineSeverity(double score)
    {
        return score switch
        {
            >= 0.8 => SafetySeverity.Critical,
            >= 0.6 => SafetySeverity.High,
            >= 0.4 => SafetySeverity.Medium,
            _ => SafetySeverity.Low
        };
    }
}

/// <summary>
/// Options for Azure Content Moderator.
/// </summary>
public class AzureContentModeratorOptions
{
    /// <summary>
    /// Azure Content Moderator endpoint (e.g., "https://your-region.api.cognitive.microsoft.com").
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure Content Moderator subscription key.
    /// </summary>
    public string SubscriptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether to detect PII (Personally Identifiable Information).
    /// </summary>
    public bool DetectPII { get; set; } = true;

    /// <summary>
    /// Minimum confidence score to flag content (0.0 - 1.0).
    /// </summary>
    public double MinimumConfidence { get; set; } = 0.5;

    /// <summary>
    /// Whether to fail closed (throw exception) or fail open (return safe) on moderation errors.
    /// </summary>
    public bool FailClosed { get; set; } = false;
}
