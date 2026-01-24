using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Bipins.AI.Benchmarks;

/// <summary>
/// Benchmarks for API endpoint performance.
/// Note: These benchmarks require a running API instance.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class ApiEndpointBenchmarks
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public ApiEndpointBenchmarks()
    {
        _baseUrl = Environment.GetEnvironmentVariable("BIPINS_API_URL") ?? "http://localhost:5000";
        _apiKey = Environment.GetEnvironmentVariable("BIPINS_API_KEY") ?? "test-key";
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromMinutes(5)
        };
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
    }

    [GlobalSetup]
    public async Task Setup()
    {
        // Ensure API is running
        try
        {
            var response = await _httpClient.GetAsync("/health");
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("API is not available. Please start the API first.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot connect to API at {_baseUrl}. Please ensure the API is running.", ex);
        }
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    [Arguments(10000)]
    public async Task IngestText(int textSize)
    {
        var text = GenerateText(textSize);
        var request = new
        {
            tenantId = "benchmark-tenant",
            docId = $"doc-{Guid.NewGuid()}",
            text = text
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/ingest/text", request);
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    public async Task QueryVectorStore()
    {
        var request = new
        {
            query = "What is machine learning?",
            topK = 5
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/query", request);
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    public async Task ChatWithRAG()
    {
        var request = new
        {
            tenantId = "benchmark-tenant",
            payload = new
            {
                messages = new[]
                {
                    new { role = "User", content = "What is machine learning?" }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/chat", request);
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    public async Task HealthCheck()
    {
        var response = await _httpClient.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    private static string GenerateText(int size)
    {
        var sb = new StringBuilder(size);
        var words = new[] { "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog" };
        var random = new Random(42);
        
        while (sb.Length < size)
        {
            sb.Append(words[random.Next(words.Length)]);
            sb.Append(' ');
        }
        
        return sb.ToString().Substring(0, Math.Min(size, sb.Length));
    }
}
