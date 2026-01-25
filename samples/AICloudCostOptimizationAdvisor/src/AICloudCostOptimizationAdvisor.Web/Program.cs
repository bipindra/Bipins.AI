using Bipins.AI.Core;
using Bipins.AI.Providers.OpenAI;
using AICloudCostOptimizationAdvisor.Shared.Services;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add memory cache for storing analysis results
builder.Services.AddMemoryCache();

// Add HTTP client factory for Terraform fetching
builder.Services.AddHttpClient();

// Configure Bipins.AI with OpenAI provider
builder.Services
    .AddBipinsAI()
    .AddOpenAI(o =>
    {
        o.ApiKey = builder.Configuration.GetValue<string>("OpenAI:ApiKey") 
                   ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                   ?? string.Empty; // Allow app to run without API key (AI features will be disabled)
        
        o.BaseUrl = builder.Configuration.GetValue<string>("OpenAI:BaseUrl") 
                    ?? Environment.GetEnvironmentVariable("OPENAI_BASE_URL") 
                    ?? "https://api.openai.com/v1";
        
        o.DefaultChatModelId = builder.Configuration.GetValue<string>("OpenAI:DefaultChatModelId") 
                               ?? Environment.GetEnvironmentVariable("OPENAI_DEFAULT_CHAT_MODEL_ID") 
                               ?? "gpt-4o-mini";
        
        o.DefaultEmbeddingModelId = builder.Configuration.GetValue<string>("OpenAI:DefaultEmbeddingModelId") 
                                    ?? Environment.GetEnvironmentVariable("OPENAI_DEFAULT_EMBEDDING_MODEL_ID") 
                                    ?? "text-embedding-ada-002";
        
        o.TimeoutSeconds = builder.Configuration.GetValue<int>("OpenAI:TimeoutSeconds", 60);
        o.MaxRetries = builder.Configuration.GetValue<int>("OpenAI:MaxRetries", 3);
    });

// Register application services
builder.Services.AddScoped<ITerraformParserService, TerraformParserService>();
builder.Services.AddScoped<ICloudCostCalculatorService, CloudCostCalculatorService>();
builder.Services.AddScoped<IAICostAnalysisService, AICostAnalysisService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
