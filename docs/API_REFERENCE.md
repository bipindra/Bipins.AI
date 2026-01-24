# Bipins.AI API Reference

## Overview

The Bipins.AI API provides a comprehensive platform for RAG (Retrieval-Augmented Generation) with support for multiple LLM providers and vector databases. All endpoints require authentication and support multi-tenant isolation.

## Base URL

```
http://localhost:5000
```

## Authentication

The API supports three authentication methods:

### 1. Basic Authentication

```http
Authorization: Basic base64(username:password)
```

### 2. JWT Bearer Token

```http
Authorization: Bearer <jwt-token>
```

### 3. API Key

```http
X-API-Key: <api-key>
```

## Endpoints

### Ingestion Endpoints

#### POST /v1/ingest/text

Ingests text content into the vector store.

**Request Body:**
```json
{
  "tenantId": "tenant1",
  "docId": "doc1",
  "text": "Your text content here...",
  "versionId": "v1.0.0",
  "updateMode": "Upsert",
  "deleteOldVersions": false
}
```

**Response:**
```json
{
  "status": "Success",
  "resultType": "ingest",
  "data": {
    "chunksIndexed": 5,
    "vectorsCreated": 5,
    "errors": []
  }
}
```

**Status Codes:**
- `200 OK` - Successfully ingested
- `400 Bad Request` - Invalid request (e.g., missing text)
- `403 Forbidden` - Quota exceeded for tenant
- `401 Unauthorized` - Authentication required

#### POST /v1/ingest/batch

Ingests multiple documents in batch.

**Request Body:**
```json
{
  "tenantId": "tenant1",
  "sourceUris": ["file:///path/to/doc1.txt", "file:///path/to/doc2.txt"],
  "texts": ["Text content 1", "Text content 2"],
  "maxConcurrency": 5
}
```

**Response:**
```json
{
  "status": "Success",
  "resultType": "ingest.batch",
  "data": {
    "results": [...],
    "errors": [],
    "totalChunksIndexed": 10,
    "totalVectorsCreated": 10
  }
}
```

### Chat Endpoints

#### POST /v1/chat

Chat with RAG support.

**Request Body:**
```json
{
  "tenantId": "tenant1",
  "correlationId": "corr1",
  "inputType": "chat",
  "payload": {
    "messages": [
      {
        "role": "User",
        "content": "What is machine learning?"
      }
    ],
    "tools": [
      {
        "name": "get_weather",
        "description": "Get the current weather",
        "parameters": {
          "type": "object",
          "properties": {
            "location": {
              "type": "string"
            }
          }
        }
      }
    ],
    "structuredOutput": {
      "schema": {
        "type": "object",
        "properties": {
          "answer": {
            "type": "string"
          }
        }
      },
      "responseFormat": "json_schema"
    },
    "temperature": 0.7,
    "maxTokens": 1000
  }
}
```

**Response:**
```json
{
  "status": "Success",
  "resultType": "chat",
  "data": {
    "content": "Machine learning is...",
    "modelId": "gpt-3.5-turbo",
    "usage": {
      "promptTokens": 100,
      "completionTokens": 50,
      "totalTokens": 150
    },
    "toolCalls": [],
    "structuredOutput": {
      "answer": "Machine learning is a subset of AI..."
    }
  },
  "citations": [
    {
      "sourceUri": "doc1",
      "docId": "doc1",
      "chunkId": "chunk_0",
      "text": "Machine learning is...",
      "score": 0.95
    }
  ],
  "telemetry": {
    "modelId": "gpt-3.5-turbo",
    "tokensUsed": 150,
    "latencyMs": 1200,
    "providerName": "OpenAI"
  }
}
```

#### POST /v1/chat/stream

Streaming chat endpoint (Server-Sent Events).

**Request Body:** Same as `/v1/chat`

**Response:** Server-Sent Events stream
```
data: {"content": "Machine", "isComplete": false}
data: {"content": " learning", "isComplete": false}
data: {"content": " is...", "isComplete": true, "usage": {...}}
data: [DONE]
```

### Query Endpoints

#### POST /v1/query

Query the vector store directly.

**Request Body:**
```json
{
  "query": "What is machine learning?",
  "topK": 5
}
```

**Response:**
```json
{
  "status": "Success",
  "resultType": "query",
  "data": {
    "chunks": [
      {
        "chunk": {
          "id": "chunk_0",
          "text": "Machine learning is...",
          "metadata": {}
        },
        "score": 0.95,
        "sourceUri": "doc1",
        "docId": "doc1"
      }
    ]
  },
  "citations": [...]
}
```

### Document Version Management

#### GET /v1/documents/{docId}/versions

List all versions of a document.

**Response:**
```json
{
  "status": "Success",
  "resultType": "document.versions",
  "data": [
    {
      "docId": "doc1",
      "versionId": "v1.0.0",
      "createdAt": "2024-01-01T00:00:00Z",
      "chunkCount": 10,
      "metadata": {}
    }
  ]
}
```

#### GET /v1/documents/{docId}/versions/{versionId}

Get a specific version of a document.

**Response:**
```json
{
  "status": "Success",
  "resultType": "document.version",
  "data": {
    "docId": "doc1",
    "versionId": "v1.0.0",
    "createdAt": "2024-01-01T00:00:00Z",
    "chunkCount": 10,
    "metadata": {}
  }
}
```

### Tenant Management

#### GET /v1/tenants/{tenantId}

Get tenant information (Admin only).

**Response:**
```json
{
  "status": "Success",
  "resultType": "tenant",
  "data": {
    "tenantId": "tenant1",
    "name": "Tenant 1",
    "createdAt": "2024-01-01T00:00:00Z",
    "quotas": {
      "maxDocuments": 10000,
      "maxStorageBytes": 10000000000,
      "maxRequestsPerDay": 100000,
      "maxTokensPerRequest": 100000
    },
    "metadata": {}
  }
}
```

#### POST /v1/tenants

Register a new tenant (Admin only).

**Request Body:**
```json
{
  "tenantId": "tenant1",
  "name": "Tenant 1",
  "quotas": {
    "maxDocuments": 10000,
    "maxStorageBytes": 10000000000,
    "maxRequestsPerDay": 100000,
    "maxTokensPerRequest": 100000
  },
  "metadata": {}
}
```

#### PUT /v1/tenants/{tenantId}

Update tenant information (Admin only).

**Request Body:** Same as POST, but all fields are optional.

### Cost Tracking Endpoints

#### GET /v1/costs/{tenantId}

Get cost summary for a tenant within a time range.

**Query Parameters:**
- `startTime` (optional): Start time in ISO 8601 format (default: 30 days ago)
- `endTime` (optional): End time in ISO 8601 format (default: now)

**Example Request:**
```
GET /v1/costs/tenant1?startTime=2024-01-01T00:00:00Z&endTime=2024-01-31T23:59:59Z
```

**Response:**
```json
{
  "status": "Success",
  "resultType": "cost.summary",
  "data": {
    "totalCost": 125.50,
    "totalTokens": 150000,
    "totalRequests": 1000,
    "costByProvider": {
      "OpenAI": 100.00,
      "VectorStore": 25.50
    },
    "costByOperation": {
      "Chat": 100.00,
      "Ingestion": 25.50
    },
    "period": {
      "startTime": "2024-01-01T00:00:00Z",
      "endTime": "2024-01-31T23:59:59Z"
    }
  }
}
```

**Status Codes:**
- `200 OK` - Success
- `401 Unauthorized` - Authentication required
- `404 Not Found` - Tenant not found

#### GET /v1/costs/{tenantId}/records

Get detailed cost records for a tenant within a time range.

**Query Parameters:**
- `startTime` (optional): Start time in ISO 8601 format (default: 30 days ago)
- `endTime` (optional): End time in ISO 8601 format (default: now)

**Response:**
```json
{
  "status": "Success",
  "resultType": "cost.records",
  "data": [
    {
      "id": "cost-1",
      "tenantId": "tenant1",
      "operationType": "Chat",
      "provider": "OpenAI",
      "modelId": "gpt-3.5-turbo",
      "tokensUsed": 150,
      "promptTokens": 100,
      "completionTokens": 50,
      "cost": 0.0002,
      "timestamp": "2024-01-15T10:30:00Z"
    }
  ]
}
```

### Health Check

#### GET /health

Health check endpoint. Returns the health status of the API and its dependencies.

**Response:**
```json
{
  "status": "Healthy",
  "checks": {
    "vector_store": "Healthy",
    "chat_model": "Healthy"
  }
}
```

**Status Codes:**
- `200 OK` - All checks healthy
- `503 Service Unavailable` - One or more checks unhealthy

## Error Responses

All error responses follow a consistent format. The structure depends on the error type:

### Standard Error Response

```json
{
  "error": "Error message here"
}
```

### Detailed Error Response (for validation errors)

```json
{
  "error": "Validation failed",
  "details": [
    {
      "field": "text",
      "message": "The text field is required"
    },
    {
      "field": "tenantId",
      "message": "Invalid tenant ID format"
    }
  ]
}
```

### Common Status Codes

- `400 Bad Request` - Invalid request format or missing required fields
  ```json
  {
    "error": "text is required"
  }
  ```

- `401 Unauthorized` - Authentication required or invalid credentials
  ```json
  {
    "error": "Authentication required"
  }
  ```

- `403 Forbidden` - Insufficient permissions or quota exceeded
  ```json
  {
    "error": "Quota exceeded for tenant"
  }
  ```

- `404 Not Found` - Resource not found
  ```json
  {
    "error": "Tenant tenant1 not found"
  }
  ```

- `429 Too Many Requests` - Rate limit exceeded
  ```json
  {
    "error": "Rate limit exceeded",
    "retryAfter": 60
  }
  ```

- `500 Internal Server Error` - Server error
  ```json
  {
    "error": "An internal server error occurred"
  }
  ```

## Rate Limiting

The API implements rate limiting per tenant and endpoint. When rate limited, the response includes:

```json
{
  "error": "Rate limit exceeded",
  "message": "Too many requests. Please try again later.",
  "retryAfter": 60
}
```

The `Retry-After` header indicates the number of seconds to wait before retrying.

## Examples

### cURL Examples

#### Ingest Text
```bash
curl -X POST http://localhost:5000/v1/ingest/text \
  -H "Authorization: Basic base64(username:password)" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "tenant1",
    "docId": "doc1",
    "text": "Your text content here..."
  }'
```

#### Chat
```bash
curl -X POST http://localhost:5000/v1/chat \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "tenant1",
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

### C# Example

```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

var request = new
{
    tenantId = "tenant1",
    payload = new
    {
        messages = new[]
        {
            new { role = "User", content = "What is machine learning?" }
        }
    }
};

var response = await client.PostAsJsonAsync(
    "http://localhost:5000/v1/chat",
    request);
var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
```

## API Versioning

The API uses URL-based versioning. The current version is `v1`. All endpoints are prefixed with `/v1/`.

### Version Strategy

- **Current Version**: `v1` - Stable API
- **Future Versions**: New versions will be introduced as `/v2/`, `/v3/`, etc.
- **Backward Compatibility**: Breaking changes will result in a new version number
- **Deprecation**: Deprecated endpoints will be announced at least 6 months in advance

### Version Headers

You can also specify the API version using the `X-API-Version` header:

```http
X-API-Version: v1
```

## Multi-Tenant Isolation

All API endpoints enforce tenant isolation. The tenant ID can be provided in one of the following ways:

1. **From Authentication Token**: Extracted from JWT claims or Basic auth
2. **From Request Body**: Explicitly provided in the request payload
3. **From Header**: Via `X-Tenant-Id` header (for Basic auth)

**Important**: All vector queries automatically filter by tenant ID to ensure complete data isolation.

## Getting Started

### Step 1: Set Up Authentication

Choose one of the authentication methods:

**Option A: API Key**
```bash
export API_KEY="your-api-key"
```

**Option B: JWT Token**
```bash
export JWT_TOKEN="your-jwt-token"
```

**Option C: Basic Auth**
```bash
export USERNAME="your-username"
export PASSWORD="your-password"
```

### Step 2: Create a Tenant (Admin Only)

```bash
curl -X POST http://localhost:5000/v1/tenants \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "my-tenant",
    "name": "My Tenant",
    "quotas": {
      "maxDocuments": 10000,
      "maxStorageBytes": 10000000000,
      "maxRequestsPerDay": 100000,
      "maxTokensPerRequest": 100000
    }
  }'
```

### Step 3: Ingest Documents

```bash
curl -X POST http://localhost:5000/v1/ingest/text \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "my-tenant",
    "docId": "doc1",
    "text": "Your document content here..."
  }'
```

### Step 4: Query with RAG

```bash
curl -X POST http://localhost:5000/v1/chat \
  -H "Authorization: Bearer $JWT_TOKEN" \
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

### Step 5: Monitor Usage

```bash
# Check tenant information
curl -X GET http://localhost:5000/v1/tenants/my-tenant \
  -H "Authorization: Bearer $JWT_TOKEN"

# Check costs
curl -X GET "http://localhost:5000/v1/costs/my-tenant?startTime=2024-01-01T00:00:00Z&endTime=2024-01-31T23:59:59Z" \
  -H "Authorization: Bearer $JWT_TOKEN"
```

## Integration Guides

### Python Integration

```python
import requests

BASE_URL = "http://localhost:5000"
API_KEY = "your-api-key"

headers = {
    "X-API-Key": API_KEY,
    "Content-Type": "application/json"
}

# Ingest text
response = requests.post(
    f"{BASE_URL}/v1/ingest/text",
    headers=headers,
    json={
        "tenantId": "my-tenant",
        "docId": "doc1",
        "text": "Your text content here..."
    }
)

# Chat with RAG
response = requests.post(
    f"{BASE_URL}/v1/chat",
    headers=headers,
    json={
        "tenantId": "my-tenant",
        "payload": {
            "messages": [
                {"role": "User", "content": "What is machine learning?"}
            ]
        }
    }
)
print(response.json())
```

### JavaScript/TypeScript Integration

```typescript
const BASE_URL = 'http://localhost:5000';
const API_KEY = 'your-api-key';

async function ingestText(tenantId: string, docId: string, text: string) {
  const response = await fetch(`${BASE_URL}/v1/ingest/text`, {
    method: 'POST',
    headers: {
      'X-API-Key': API_KEY,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      tenantId,
      docId,
      text,
    }),
  });
  return response.json();
}

async function chat(tenantId: string, message: string) {
  const response = await fetch(`${BASE_URL}/v1/chat`, {
    method: 'POST',
    headers: {
      'X-API-Key': API_KEY,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      tenantId,
      payload: {
        messages: [{ role: 'User', content: message }],
      },
    }),
  });
  return response.json();
}
```

### .NET Integration

```csharp
using System.Net.Http.Json;

var client = new HttpClient();
client.BaseAddress = new Uri("http://localhost:5000");
client.DefaultRequestHeaders.Add("X-API-Key", "your-api-key");

// Ingest text
var ingestRequest = new
{
    tenantId = "my-tenant",
    docId = "doc1",
    text = "Your text content here..."
};

var ingestResponse = await client.PostAsJsonAsync("/v1/ingest/text", ingestRequest);

// Chat with RAG
var chatRequest = new
{
    tenantId = "my-tenant",
    payload = new
    {
        messages = new[]
        {
            new { role = "User", content = "What is machine learning?" }
        }
    }
};

var chatResponse = await client.PostAsJsonAsync("/v1/chat", chatRequest);
var result = await chatResponse.Content.ReadFromJsonAsync<ChatResponse>();
```

## Best Practices

1. **Always specify tenantId**: Even if extracted from auth, explicitly provide it in requests for clarity
2. **Handle rate limits**: Implement exponential backoff when receiving 429 responses
3. **Use batch ingestion**: For multiple documents, use `/v1/ingest/batch` instead of multiple `/v1/ingest/text` calls
4. **Monitor costs**: Regularly check `/v1/costs/{tenantId}` to track usage
5. **Use streaming**: For better UX, use `/v1/chat/stream` for chat endpoints
6. **Validate tenant IDs**: Ensure tenant IDs match the pattern `^[a-zA-Z0-9_-]+$` and are max 100 characters

## Support

For more information, see:
- [README.md](../README.md)
- [Implementation Status](IMPLEMENTATION_STATUS.md)
- Swagger UI at `/swagger` (when running in development mode)

## Changelog

### v1.0.0 (Current)
- Initial API release
- Multi-tenant support
- RAG capabilities
- Cost tracking
- Document versioning
- Streaming chat support
