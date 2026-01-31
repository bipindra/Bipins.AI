using Bipins.AI;
using Bipins.AI.Guardian.Models;
using Bipins.AI.Guardian.Validators;
using Bipins.AI.Resilience;
using Bipins.AI.Safety;
using Bipins.AI.Safety.Middleware;
using Bipins.AI.Validation;
using Bipins.AI.Validation.FluentValidation;
using Bipins.AI.Validation.JsonSchema;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Bipins.AI with OpenAI
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"] 
    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
    ?? throw new InvalidOperationException("OpenAI API key is required. Set it in user secrets or environment variable OPENAI_API_KEY");

builder.Services
    .AddBipinsAI()
    .AddOpenAI(options =>
    {
        options.ApiKey = openAiApiKey;
        options.BaseUrl = builder.Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
        options.DefaultChatModelId = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4o-mini";
        options.DefaultEmbeddingModelId = builder.Configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
    })
    .AddContentModeration(options =>
    {
        options.Enabled = true;
        options.MinimumSeverityToBlock = SafetySeverity.Medium;
        options.FilterUnsafeContent = false;
        options.ThrowOnUnsafeContent = false;
        options.BlockedCategories = new List<SafetyCategory> { SafetyCategory.PromptInjection, SafetyCategory.SelfHarm };
    })
    .AddResilience(options =>
    {
        options.Retry = new RetryOptions
        {
            MaxRetries = 2,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffStrategy = BackoffStrategy.Exponential,
            MaxDelay = TimeSpan.FromSeconds(2)
        };
        options.Timeout = new TimeoutOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    })
    .AddValidation()
    .AddFluentValidation()
    .AddJsonSchemaValidation()
    .UseContentModerationMiddleware();

// Register mock content moderator (for demo - use Azure Content Moderator in production)
builder.Services.AddSingleton<Bipins.AI.Safety.IContentModerator, Bipins.AI.Guardian.Services.MockContentModerator>();
builder.Services.AddSingleton<Bipins.AI.Safety.Middleware.ILLMProviderMiddleware>(sp =>
{
    var moderator = sp.GetRequiredService<Bipins.AI.Safety.IContentModerator>();
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ContentModerationOptions>>();
    var logger = sp.GetService<ILogger<Bipins.AI.Safety.Middleware.ContentModerationLLMMiddleware>>();
    return new Bipins.AI.Safety.Middleware.ContentModerationLLMMiddleware(moderator, options, logger);
});

// Register FluentValidation validators
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<ChatRequestModelValidator>();

// Register FluentValidation validator for ChatRequestModel
builder.Services.AddSingleton<IRequestValidator<ChatRequestModel>>(sp =>
{
    var validator = sp.GetRequiredService<IValidator<ChatRequestModel>>();
    var logger = sp.GetService<ILogger<FluentValidationValidator<ChatRequestModel>>>();
    return new FluentValidationValidator<ChatRequestModel>(validator, logger);
});

// Register JSON Schema validator for string responses
builder.Services.AddSingleton<IResponseValidator<string>>(sp =>
{
    var logger = sp.GetService<ILogger<JsonSchemaValidator>>();
    return new JsonSchemaValidator(logger);
});

// Register resilience policy
builder.Services.AddSingleton<IResiliencePolicy>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResilienceOptions>>().Value;
    var logger = sp.GetService<ILogger<PollyResiliencePolicy>>();
    return new PollyResiliencePolicy(options, logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
