using Microsoft.AspNetCore.Builder;

namespace Bipins.AI.Api.Middleware;

/// <summary>
/// Extension methods for registering content moderation middleware.
/// </summary>
public static class ContentModerationMiddlewareExtensions
{
    /// <summary>
    /// Adds content moderation middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseContentModeration(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ContentModerationMiddleware>();
    }
}
