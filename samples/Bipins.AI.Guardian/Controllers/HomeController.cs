using Bipins.AI.Core.Models;
using Bipins.AI.Guardian.Models;
using Bipins.AI.Providers;
using Bipins.AI.Resilience;
using Bipins.AI.Validation;
using Bipins.AI.Validation.FluentValidation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Bipins.AI.Guardian.Controllers;

public class HomeController : Controller
{
    private readonly ILLMProvider _llmProvider;
    private readonly IResiliencePolicy _resiliencePolicy;
    private readonly IRequestValidator<ChatRequestModel>? _requestValidator;
    private readonly IResponseValidator<string>? _responseValidator;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ILLMProvider llmProvider,
        IResiliencePolicy? resiliencePolicy,
        IRequestValidator<ChatRequestModel>? requestValidator,
        IResponseValidator<string>? responseValidator,
        ILogger<HomeController> logger)
    {
        _llmProvider = llmProvider;
        _resiliencePolicy = resiliencePolicy ?? throw new InvalidOperationException("IResiliencePolicy is required");
        _requestValidator = requestValidator;
        _responseValidator = responseValidator;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new ChatRequestModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Chat(ChatRequestModel model, CancellationToken cancellationToken)
    {
        var responseModel = new ChatResponseModel();

        // Validate request with FluentValidation
        if (_requestValidator != null)
        {
            var validationResult = await _requestValidator.ValidateAsync(model, cancellationToken);
            if (!validationResult.IsValid)
            {
                responseModel.ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return View("Index", model);
            }
        }

        // Model validation
        if (!ModelState.IsValid)
        {
            responseModel.ValidationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return View("Index", model);
        }

        try
        {
            int retryCount = 0;

            // Execute with resilience policy (retry logic)
            var chatResponse = await _resiliencePolicy.ExecuteAsync(async () =>
            {
                retryCount++;
                var request = new ChatRequest(new[]
                {
                    new Message(MessageRole.System, "You are a helpful assistant. Keep responses concise."),
                    new Message(MessageRole.User, model.Message)
                });

                return await _llmProvider.ChatAsync(request, cancellationToken);
            }, cancellationToken);

            responseModel.Response = chatResponse.Content ?? string.Empty;
            responseModel.HasRetry = retryCount > 1;
            responseModel.RetryCount = retryCount;

            // Check safety info
            if (chatResponse.Safety != null)
            {
                responseModel.IsModerated = chatResponse.Safety.Flagged;
                if (chatResponse.Safety.Categories != null && chatResponse.Safety.Categories.Count > 0)
                {
                    responseModel.SafetyLevel = string.Join(", ", chatResponse.Safety.Categories.Keys);
                }
            }

            // Validate response with JSON Schema
            if (_responseValidator != null && !string.IsNullOrEmpty(responseModel.Response))
            {
                var schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""content"": { ""type"": ""string"", ""minLength"": 1 }
                    },
                    ""required"": [""content""]
                }";

                var responseJson = System.Text.Json.JsonSerializer.Serialize(new { content = responseModel.Response });
                var validationResult = await _responseValidator.ValidateAsync(responseJson, schema, cancellationToken);
                if (!validationResult.IsValid)
                {
                    responseModel.ValidationErrors.AddRange(validationResult.Errors.Select(e => $"Response validation: {e.ErrorMessage}"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chat");
            responseModel.Error = ex.Message;
        }

        ViewBag.Response = responseModel;
        return View("Index", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new Models.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
