# Guardian - AI Chat with Safety Features

A simple MVC web application demonstrating Bipins.AI's safety, validation, and resilience features.

## Features Demonstrated

- ✅ **Content Moderation**: All messages and responses are moderated using a mock content moderator
- ✅ **Request Validation**: FluentValidation ensures input quality and safety
- ✅ **Response Validation**: JSON Schema validates response structure
- ✅ **Resilience**: Polly retry policy handles transient failures
- ✅ **Safety Info**: Safety metadata included in responses

## Setup

1. Set your OpenAI API key in user secrets:
   ```bash
   dotnet user-secrets set "OpenAI:ApiKey" "your-api-key" --project samples/Bipins.AI.Guardian/Bipins.AI.Guardian.csproj
   ```

   Or set the `OPENAI_API_KEY` environment variable.

2. Run the application:
   ```bash
   dotnet run --project samples/Bipins.AI.Guardian/Bipins.AI.Guardian.csproj
   ```

3. Open your browser to `https://localhost:5001` or `http://localhost:5000`

## Usage

1. Enter a message in the text box
2. Click "Send Message"
3. View the response along with:
   - Content moderation status
   - Validation results
   - Retry information (if applicable)
   - Safety metadata

## Configuration

The app uses:
- **OpenAI** as the LLM provider (gpt-4o-mini by default)
- **Mock Content Moderator** for demonstration (replace with Azure Content Moderator in production)
- **FluentValidation** for request validation
- **NJsonSchema** for response validation
- **Polly** for resilience (retry and timeout policies)

## Notes

- The mock content moderator uses simple keyword-based detection
- In production, replace with Azure Content Moderator or another service
- All safety features are enabled by default
- Retry policy is configured with exponential backoff
