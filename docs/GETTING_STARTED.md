# Getting Started with Bipins.AI

This guide will help you get started with the Bipins.AI platform, from installation to your first RAG-powered chat.

## Prerequisites

- .NET 8.0 SDK or later
- A vector database (Qdrant, Pinecone, Weaviate, Milvus, or PgVector)
- An LLM provider API key (OpenAI, Anthropic, Azure OpenAI, or AWS Bedrock)

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/Bipins.AI.git
cd Bipins.AI
```

### 2. Configure the API

Create an `appsettings.json` file in `src/Bipins.AI.Api/`:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "DefaultChatModelId": "gpt-3.5-turbo",
    "DefaultEmbeddingModelId": "text-embedding-ada-002"
  },
  "Qdrant": {
    "Endpoint": "http://localhost:6333",
    "DefaultCollectionName": "default",
    "VectorSize": 1536
  },
  "Jwt": {
    "Secret": "your-jwt-secret-key",
    "Issuer": "Bipins.AI",
    "Audience": "Bipins.AI"
  }
}
```

### 3. Run the API

```bash
cd src/Bipins.AI.Api
dotnet run
```

The API will be available at `http://localhost:5000`.

## Quick Start

### Step 1: Create a Tenant

First, create a tenant using the admin API:

```bash
curl -X POST http://localhost:5000/v1/tenants \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "my-tenant",
    "name": "My Tenant",
    "quotas": {
      "maxDocuments": 10000,
      "maxStorageBytes": 10000000000,
      "maxRequestsPerDay": 100000
    }
  }'
```

### Step 2: Ingest Your First Document

```bash
curl -X POST http://localhost:5000/v1/ingest/text \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "my-tenant",
    "docId": "doc1",
    "text": "Machine learning is a subset of artificial intelligence that enables systems to learn and improve from experience without being explicitly programmed."
  }'
```

### Step 3: Chat with Your Data

```bash
curl -X POST http://localhost:5000/v1/chat \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "my-tenant",
    "payload": {
      "messages": [
        {
          "role": "User",
          "content": "What is machine learning?"
        }
      ]
    }
  }'
```

## Next Steps

- Read the [API Reference](API_REFERENCE.md) for detailed endpoint documentation
- Check out the [Integration Guides](API_REFERENCE.md#integration-guides) for code examples
- Explore [Advanced Features](API_REFERENCE.md) like streaming, tools, and structured output

## Troubleshooting

### Common Issues

1. **Authentication Errors**: Ensure your API key or JWT token is valid
2. **Vector Store Connection**: Verify your vector database is running and accessible
3. **Quota Exceeded**: Check your tenant quotas using `/v1/tenants/{tenantId}`
4. **Rate Limiting**: Implement exponential backoff for 429 responses

For more help, see the [API Reference](API_REFERENCE.md) or open an issue on GitHub.
