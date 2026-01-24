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

### Health Check

#### GET /health

Health check endpoint.

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

## Error Responses

All error responses follow this format:

```json
{
  "error": "Error message here"
}
```

**Common Status Codes:**
- `400 Bad Request` - Invalid request format or missing required fields
- `401 Unauthorized` - Authentication required or invalid credentials
- `403 Forbidden` - Insufficient permissions or quota exceeded
- `404 Not Found` - Resource not found
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error

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

## Getting Started

1. **Set up authentication**: Configure your API keys or JWT tokens
2. **Create a tenant**: Use the `/v1/tenants` endpoint (Admin only)
3. **Ingest documents**: Use `/v1/ingest/text` or `/v1/ingest/batch`
4. **Query with RAG**: Use `/v1/chat` for RAG-powered conversations
5. **Monitor usage**: Check tenant quotas and usage via `/v1/tenants/{tenantId}`

## Support

For more information, see:
- [README.md](../README.md)
- [Implementation Status](IMPLEMENTATION_STATUS.md)
- Swagger UI at `/swagger` (when running in development mode)
