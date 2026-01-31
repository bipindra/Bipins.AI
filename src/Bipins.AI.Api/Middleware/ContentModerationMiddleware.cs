using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Bipins.AI.Safety;
using System.Text;
using System.Text.Json;

namespace Bipins.AI.Api.Middleware;

/// <summary>
/// Middleware for content moderation in ASP.NET Core.
/// </summary>
public class ContentModerationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IContentModerator _moderator;
    private readonly ContentModerationOptions _options;
    private readonly ILogger<ContentModerationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentModerationMiddleware"/> class.
    /// </summary>
    public ContentModerationMiddleware(
        RequestDelegate next,
        IContentModerator moderator,
        IOptions<ContentModerationOptions> options,
        ILogger<ContentModerationMiddleware> logger)
    {
        _next = next;
        _moderator = moderator;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Only moderate POST requests with JSON bodies
        if (context.Request.Method == "POST" && 
            context.Request.ContentType?.Contains("application/json") == true)
        {
            // Read and buffer the request body
            context.Request.EnableBuffering();
            var originalBodyStream = context.Response.Body;

            try
            {
                using var requestReader = new StreamReader(
                    context.Request.Body, 
                    Encoding.UTF8, 
                    leaveOpen: true);
                
                var requestBody = await requestReader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                // Extract text content from JSON
                var contentToModerate = ExtractTextFromJson(requestBody);

                if (!string.IsNullOrWhiteSpace(contentToModerate))
                {
                    var moderationResult = await _moderator.ModerateAsync(
                        contentToModerate, 
                        "text/plain", 
                        context.RequestAborted);

                    if (!moderationResult.IsSafe)
                    {
                        _logger.LogWarning(
                            "Unsafe content detected: {ViolationCount} violations, Categories: {Categories}",
                            moderationResult.Violations.Count,
                            string.Join(", ", moderationResult.Violations.Select(v => v.Category)));

                        // Check if we should block
                        var shouldBlock = moderationResult.Violations.Any(v => 
                            _options.BlockedCategories.Contains(v.Category) ||
                            v.Severity >= _options.MinimumSeverityToBlock);

                        if (shouldBlock)
                        {
                            if (_options.ThrowOnUnsafeContent)
                            {
                                throw new UnauthorizedAccessException(
                                    $"Content moderation failed: {string.Join(", ", moderationResult.Violations.Select(v => v.Category))}");
                            }

                            context.Response.StatusCode = 400;
                            context.Response.ContentType = "application/json";
                            
                            await context.Response.WriteAsJsonAsync(new
                            {
                                error = "Content moderation failed",
                                reason = "Unsafe content detected",
                                violations = moderationResult.Violations.Select(v => new
                                {
                                    category = v.Category.ToString(),
                                    severity = v.Severity.ToString(),
                                    reason = v.Reason
                                })
                            }, context.RequestAborted);

                            return;
                        }

                        // Add safety info to response headers
                        context.Response.Headers.Add("X-Content-Moderated", "true");
                        context.Response.Headers.Add("X-Content-Safety-Level", 
                            moderationResult.Violations.Max(v => v.Severity).ToString());
                    }
                }

                // Continue to next middleware
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in content moderation middleware");
                
                if (_options.ThrowOnUnsafeContent)
                {
                    throw;
                }

                // Continue on error (fail open)
                await _next(context);
            }
            finally
            {
                context.Request.Body.Position = 0;
            }
        }
        else
        {
            await _next(context);
        }
    }

    private static string ExtractTextFromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Try to extract common text fields
            var textFields = new[] { "content", "text", "message", "input", "query", "prompt" };
            var extractedText = new StringBuilder();

            ExtractTextRecursive(root, textFields, extractedText);

            return extractedText.ToString();
        }
        catch
        {
            return json; // Fallback to entire JSON as text
        }
    }

    private static void ExtractTextRecursive(JsonElement element, string[] textFields, StringBuilder sb)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (textFields.Contains(prop.Name.ToLowerInvariant()) && 
                        prop.Value.ValueKind == JsonValueKind.String)
                    {
                        if (sb.Length > 0) sb.Append(" ");
                        sb.Append(prop.Value.GetString());
                    }
                    else
                    {
                        ExtractTextRecursive(prop.Value, textFields, sb);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ExtractTextRecursive(item, textFields, sb);
                }
                break;

            case JsonValueKind.String:
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(element.GetString());
                break;
        }
    }
}
