# TODO Items Breakdown

This document breaks down each TODO item from the README into actionable steps and sub-tasks.

## 1. Add support for additional LLM providers (Anthropic Claude, Azure OpenAI, AWS Bedrock)

### Steps:

#### For Anthropic Claude:
1. Create new project: `Bipins.AI.Connectors.Llm.Anthropic`
2. Add NuGet package reference to Anthropic SDK (or implement HTTP client)
3. Create `AnthropicOptions` class with configuration properties (ApiKey, BaseUrl, DefaultModelId)
4. Implement `IChatModel` interface:
   - Create `AnthropicChatModel` class
   - Map `ChatRequest` to Anthropic API format
   - Handle Anthropic-specific message format (system/user/assistant)
   - Map Anthropic response to `ChatResponse`
   - Handle errors and exceptions
5. Implement `IEmbeddingModel` interface (if Anthropic supports embeddings):
   - Create `AnthropicEmbeddingModel` class
   - Map embedding requests/responses
6. Create `AnthropicServiceCollectionExtensions`:
   - Add `AddAnthropic()` extension method
   - Register options and services
7. Add configuration support in `Program.cs` files
8. Write unit tests for the connector
9. Update README with usage examples

#### For Azure OpenAI:
1. Create new project: `Bipins.AI.Connectors.Llm.AzureOpenAI`
2. Create `AzureOpenAiOptions` class (Endpoint, ApiKey, DeploymentName, ApiVersion)
3. Implement `IChatModel`:
   - Reuse OpenAI format but with Azure endpoint
   - Handle Azure-specific authentication (API key in header)
   - Support different deployment names per tenant
4. Implement `IEmbeddingModel` for Azure OpenAI embeddings
5. Create extension method `AddAzureOpenAI()`
6. Add configuration support
7. Write unit tests
8. Update documentation

#### For AWS Bedrock:
1. Create new project: `Bipins.AI.Connectors.Llm.Bedrock`
2. Add AWS SDK NuGet packages (AWSSDK.BedrockRuntime)
3. Create `BedrockOptions` class (Region, AccessKey, SecretKey, ModelId)
4. Implement `IChatModel`:
   - Use AWS Bedrock Runtime API
   - Map to Bedrock model-specific formats (Claude, Llama, etc.)
   - Handle AWS authentication (IAM roles or credentials)
5. Implement `IEmbeddingModel` for Bedrock embedding models
6. Create extension method `AddBedrock()`
7. Add configuration support
8. Write unit tests
9. Update documentation

---

## 2. Add support for additional vector databases (Pinecone, Weaviate, Milvus)

### Steps:

#### For Pinecone:
1. Create new project: `Bipins.AI.Connectors.Vector.Pinecone`
2. Add Pinecone SDK NuGet package
3. Create `PineconeOptions` class (ApiKey, Environment, IndexName)
4. Implement `IVectorStore`:
   - `UpsertAsync`: Map to Pinecone upsert API
   - `QueryAsync`: Map to Pinecone query API with metadata filtering
   - `DeleteAsync`: Map to Pinecone delete API
5. Implement filter translation:
   - Create `PineconeFilterTranslator` to convert `VectorFilter` to Pinecone filter format
6. Create `PineconeServiceCollectionExtensions` with `AddPinecone()` method
7. Add configuration support
8. Write unit tests
9. Update documentation

#### For Weaviate:
1. Create new project: `Bipins.AI.Connectors.Vector.Weaviate`
2. Add Weaviate client NuGet package
3. Create `WeaviateOptions` class (Endpoint, ApiKey, ClassName, Schema)
4. Implement `IVectorStore`:
   - Handle Weaviate GraphQL API for queries
   - Use REST API for upserts
   - Support Weaviate schema and class management
5. Implement filter translation for Weaviate `where` filters
6. Create extension method `AddWeaviate()`
7. Add configuration support
8. Write unit tests
9. Update documentation

#### For Milvus:
1. Create new project: `Bipins.AI.Connectors.Vector.Milvus`
2. Add Milvus .NET SDK NuGet package
3. Create `MilvusOptions` class (Endpoint, CollectionName, VectorFieldName)
4. Implement `IVectorStore`:
   - Use Milvus gRPC client
   - Handle collection creation and schema
   - Map to Milvus search/insert/delete operations
5. Implement filter translation for Milvus expression filters
6. Create extension method `AddMilvus()`
7. Add configuration support
8. Write unit tests
9. Update documentation

---

## 3. Implement streaming responses for chat endpoints

### Steps:

1. Add streaming support to `IChatModel` interface:
   - Add `IAsyncEnumerable<ChatResponseChunk> GenerateStreamAsync()` method
2. Create `ChatResponseChunk` class for streaming chunks
3. Update `OpenAiChatModel` to support streaming:
   - Use Server-Sent Events (SSE) or streaming API
   - Parse streaming JSON responses
   - Yield chunks as they arrive
4. Update API endpoint `/v1/chat`:
   - Add streaming parameter/header
   - Return `IAsyncEnumerable` or use `StreamingJson` response
   - Handle cancellation tokens properly
5. Update `IRagComposer` if needed to support streaming context
6. Add streaming support to other LLM providers (Anthropic, Azure, Bedrock)
7. Write unit tests for streaming functionality
8. Update API documentation with streaming examples
9. Add client examples showing how to consume streaming responses

---

## 4. Add batch ingestion support for multiple documents

### Steps:

1. Update `IIndexer` interface:
   - Add `Task<BatchIndexResult> BatchIndexAsync()` method
2. Create `BatchIndexRequest` class:
   - List of documents with paths/URIs
   - Batch size configuration
   - Parallel processing options
3. Create `BatchIndexResult` class:
   - Success/failure counts per document
   - Aggregate statistics
   - Individual document results
4. Update `IngestionPipeline`:
   - Add `BatchIngestAsync()` method
   - Process documents in parallel (configurable concurrency)
   - Handle errors per document without failing entire batch
5. Update API endpoint:
   - Add `POST /v1/ingest/batch` endpoint
   - Accept array of documents
   - Return batch results
6. Add progress tracking/callbacks for long-running batches
7. Write unit tests for batch processing
8. Write integration tests
9. Update documentation with batch ingestion examples

---

## 5. Implement document versioning and update capabilities

### Steps:

1. Update `Chunk` model:
   - Add `Version` property
   - Add `DocumentVersion` property
   - Add `LastModified` timestamp
2. Update `IndexOptions`:
   - Add `VersionId` property (already exists, verify usage)
   - Add `UpdateMode` enum (Create, Update, Upsert)
3. Update `IIndexer`:
   - Add version tracking logic
   - Implement update detection (compare document hashes or timestamps)
   - Support deleting old versions when updating
4. Update `IVectorStore`:
   - Add version filtering to queries
   - Support versioned upserts
   - Add method to delete by version
5. Update `IngestionPipeline`:
   - Check for existing document versions
   - Handle update vs. create logic
   - Maintain version history
6. Update API endpoints:
   - Add version parameter to ingestion
   - Add endpoint to list document versions
   - Add endpoint to retrieve specific version
7. Add database/storage for version metadata (if not using vector DB metadata)
8. Write unit tests for versioning
9. Write integration tests
10. Update documentation

---

## 6. Add support for structured output/function calling

### Steps:

1. Review existing `ToolDefinition` and `ToolCall` models (may already exist)
2. Update `ChatRequest`:
   - Ensure `Tools` property is properly supported
   - Add structured output schema support
3. Update `IChatModel` implementations:
   - Ensure all providers support tool/function calling
   - Map tool definitions to provider-specific formats
   - Parse tool calls from responses
4. Create `StructuredOutputSchema` class:
   - JSON schema definition
   - Type mapping
5. Add structured output support to `ChatResponse`:
   - Parse structured JSON from model responses
   - Validate against schema
6. Update API endpoint:
   - Accept tools/function definitions in request
   - Return structured outputs
7. Create examples for common use cases:
   - Function calling examples
   - Structured data extraction
8. Write unit tests
9. Update API documentation

---

## 7. Implement rate limiting and throttling policies

### Steps:

1. Create `IRateLimiter` interface:
   - `Task<bool> TryAcquireAsync(string key, int limit, TimeSpan window)`
2. Implement rate limiter:
   - Create `MemoryRateLimiter` (in-memory, single instance)
   - Create `DistributedRateLimiter` (Redis-based for multi-instance)
3. Create `RateLimitPolicy` class:
   - Per-tenant limits
   - Per-endpoint limits
   - Per-model limits
4. Update `IPolicyProvider`:
   - Add rate limiting policy retrieval
5. Create middleware for API:
   - `RateLimitMiddleware` to check limits
   - Return 429 status when exceeded
   - Add rate limit headers to responses
6. Add configuration:
   - Rate limit settings in appsettings.json
   - Per-tenant configuration support
7. Integrate with pipeline execution:
   - Apply rate limits before model calls
   - Handle rate limit exceptions
8. Add metrics/logging for rate limit events
9. Write unit tests
10. Update documentation

---

## 8. Add comprehensive unit test coverage

### Steps:

1. Audit existing test coverage:
   - Identify gaps in current tests
   - List all classes/interfaces needing tests
2. Create test projects structure:
   - Organize tests by feature area
   - Set up test fixtures and helpers
3. Add tests for Core models:
   - Test all model classes
   - Test serialization/deserialization
4. Add tests for Runtime components:
   - Pipeline runner
   - Model router
   - Policy provider
   - Cache implementations
5. Add tests for Ingestion:
   - Document loader
   - Chunker implementations
   - Indexer
6. Add tests for Connectors:
   - Mock HTTP clients for LLM providers
   - Test error handling
   - Test response mapping
7. Add tests for API:
   - Endpoint tests
   - Authentication tests
   - Error handling tests
8. Set up code coverage reporting:
   - Configure coverlet
   - Set coverage thresholds
   - Add to CI/CD
9. Achieve target coverage (e.g., 80%+)
10. Document testing patterns and conventions

---

## 9. Add integration tests for all connectors

### Steps:

1. Set up integration test infrastructure:
   - Test fixtures for each provider
   - Mock servers or test accounts
   - Configuration for test environments
2. Create OpenAI integration tests:
   - Test chat completion
   - Test embeddings
   - Test error scenarios
3. Create Qdrant integration tests:
   - Test upsert operations
   - Test query operations
   - Test delete operations
   - Test filter translation
4. Create tests for new providers (as they're added):
   - Anthropic integration tests
   - Azure OpenAI integration tests
   - Bedrock integration tests
   - Pinecone integration tests
   - Weaviate integration tests
   - Milvus integration tests
5. Create end-to-end integration tests:
   - Full ingestion pipeline
   - RAG query flow
   - Multi-provider scenarios
6. Set up test data and fixtures
7. Add integration test documentation
8. Configure CI/CD to run integration tests
9. Add test environment setup scripts

---

## 10. Implement authentication and authorization improvements

### Steps:

1. Review current `BasicAuthenticationHandler`:
   - Identify security gaps
   - Plan improvements
2. Add JWT token support:
   - Create `JwtAuthenticationHandler`
   - Add token validation
   - Support multiple authentication schemes
3. Implement role-based authorization:
   - Define roles (Admin, User, ReadOnly)
   - Add authorization policies
   - Apply to endpoints
4. Add API key authentication:
   - Create `ApiKeyAuthenticationHandler`
   - Support per-tenant API keys
   - Store keys securely (hashed)
5. Add OAuth2/OIDC support:
   - Integrate with identity providers
   - Support SSO
6. Implement tenant isolation:
   - Ensure data separation
   - Validate tenant access
7. Add audit logging:
   - Log authentication events
   - Log authorization failures
8. Update API documentation:
   - Document authentication methods
   - Provide examples
9. Write security tests
10. Perform security review

---

## 11. Add support for custom chunking strategies

### Steps:

1. Review current `IChunker` interface:
   - Assess extensibility
2. Create `ChunkingStrategy` enum/base class:
   - Define strategy types
3. Create strategy implementations:
   - `FixedSizeChunker` (existing)
   - `SentenceAwareChunker`
   - `ParagraphChunker`
   - `SemanticChunker` (using embeddings)
   - `MarkdownAwareChunker` (existing, enhance)
4. Update `ChunkOptions`:
   - Add strategy selection
   - Add strategy-specific parameters
5. Create `IChunkingStrategyFactory`:
   - Strategy registration
   - Strategy selection logic
6. Update `IngestionPipeline`:
   - Use strategy factory
   - Support per-document strategies
7. Add configuration:
   - Strategy selection in appsettings
   - Per-tenant strategy configuration
8. Write unit tests for each strategy
9. Update documentation with strategy examples

---

## 12. Implement metadata filtering enhancements

### Steps:

1. Review current `VectorFilter` implementation:
   - Assess current capabilities
2. Enhance `VectorFilter`:
   - Add support for complex queries (AND/OR/NOT)
   - Add range queries (numeric, date)
   - Add text search in metadata
   - Add array/collection operators
3. Update filter translators:
   - Enhance `QdrantFilterTranslator`
   - Update translators for other vector DBs
4. Create filter builder API:
   - Fluent API for building filters
   - Type-safe filter construction
5. Update `RetrieveRequest`:
   - Enhance filter support
   - Add filter examples
6. Update API endpoint:
   - Accept complex filters in query
   - Validate filter syntax
7. Write unit tests for filter translation
8. Write integration tests
9. Update API documentation with filter examples

---

## 13. Add monitoring and alerting capabilities

### Steps:

1. Set up OpenTelemetry (may already exist):
   - Verify current implementation
   - Enhance if needed
2. Add metrics collection:
   - Request counts
   - Latency metrics
   - Error rates
   - Token usage metrics
3. Add health checks:
   - Create health check endpoints
   - Check LLM provider connectivity
   - Check vector DB connectivity
4. Integrate with monitoring systems:
   - Prometheus metrics export
   - Application Insights integration
   - Custom dashboards
5. Add structured logging:
   - Enhance existing logging
   - Add correlation IDs
   - Add performance logging
6. Create alerting rules:
   - High error rates
   - High latency
   - Provider failures
7. Add distributed tracing:
   - Enhance OpenTelemetry spans
   - Add custom spans for operations
8. Create monitoring documentation
9. Set up monitoring infrastructure

---

## 14. Create Docker images for API and Worker services

### Steps:

1. Create Dockerfile for API:
   - Multi-stage build
   - Optimize image size
   - Set up proper user permissions
2. Create Dockerfile for Worker:
   - Similar to API but for worker service
3. Create .dockerignore files:
   - Exclude unnecessary files
4. Add docker-compose.yml updates:
   - Include API service
   - Include Worker service
   - Configure networking
5. Test Docker builds locally
6. Create GitHub Actions workflow:
   - Build Docker images
   - Push to container registry
7. Add image versioning/tagging strategy
8. Document Docker usage
9. Test in production-like environment

---

## 15. Add Kubernetes deployment manifests

### Steps:

1. Create namespace definition
2. Create ConfigMap for configuration
3. Create Secrets for sensitive data
4. Create Deployment for API:
   - Replicas configuration
   - Resource limits
   - Health checks
   - Environment variables
5. Create Deployment for Worker:
   - Similar to API
6. Create Service definitions:
   - API service (LoadBalancer/ClusterIP)
   - Worker service if needed
7. Create Ingress:
   - Route external traffic
   - SSL/TLS configuration
8. Add HorizontalPodAutoscaler:
   - Auto-scaling configuration
9. Create PersistentVolumeClaims if needed
10. Add monitoring integration:
    - ServiceMonitor for Prometheus
11. Test deployment locally (minikube/kind)
12. Document deployment process
13. Create Helm chart (optional)

---

## 16. Implement distributed caching support

### Steps:

1. Review current `ICache` interface:
   - Assess for distributed caching needs
2. Create `IDistributedCache` abstraction:
   - Extend or replace `ICache`
3. Implement Redis cache:
   - Create `RedisCache` implementation
   - Add Redis connection management
   - Handle serialization
4. Add cache configuration:
   - Redis connection string
   - Cache TTL settings
   - Key prefix configuration
5. Update cache usage:
   - Replace `MemoryCache` with distributed cache where needed
   - Add cache key strategies
6. Add cache invalidation:
   - Per-tenant invalidation
   - Pattern-based invalidation
7. Add cache metrics:
   - Hit/miss rates
   - Latency metrics
8. Write unit tests
9. Write integration tests with Redis
10. Update documentation

---

## 17. Add support for multi-tenant isolation

### Steps:

1. Review current tenant handling:
   - Identify isolation gaps
2. Enhance data isolation:
   - Ensure vector store queries are tenant-scoped
   - Add tenant validation at all layers
3. Update `IndexOptions`:
   - Ensure tenant ID is always required
   - Validate tenant format
4. Update `IVectorStore`:
   - Add tenant filtering to all operations
   - Ensure queries are tenant-isolated
5. Update API endpoints:
   - Extract tenant from authentication
   - Validate tenant access
6. Add tenant management:
   - Create tenant registration
   - Tenant configuration per tenant
7. Add tenant-level quotas:
   - Document limits
   - Request limits
   - Storage limits
8. Add tenant-level monitoring:
   - Per-tenant metrics
   - Per-tenant logging
9. Write security tests:
   - Test tenant isolation
   - Test cross-tenant access prevention
10. Update documentation

---

## 18. Create comprehensive API documentation

### Steps:

1. Enhance Swagger/OpenAPI:
   - Add detailed descriptions
   - Add request/response examples
   - Add error response documentation
2. Create API reference documentation:
   - Document all endpoints
   - Document request/response schemas
   - Document authentication
3. Create getting started guide:
   - Quick start tutorial
   - Common use cases
   - Code examples
4. Create integration guides:
   - Client SDK examples
   - cURL examples
   - Postman collection
5. Add API versioning documentation
6. Create changelog for API changes
7. Add troubleshooting guide
8. Create interactive API explorer
9. Publish documentation (GitHub Pages, ReadTheDocs, etc.)

---

## 19. Add performance benchmarking suite

### Steps:

1. Create benchmark project:
   - Use BenchmarkDotNet
   - Set up test scenarios
2. Create benchmarks for core operations:
   - Embedding generation benchmarks
   - Vector query benchmarks
   - Chunking benchmarks
3. Create end-to-end benchmarks:
   - Full ingestion pipeline
   - RAG query flow
4. Add load testing:
   - Use k6 or similar
   - Test API endpoints under load
   - Test concurrent ingestion
5. Create performance test scenarios:
   - Small document ingestion
   - Large document ingestion
   - High query volume
6. Set up performance monitoring:
   - Track benchmark results over time
   - Compare provider performance
7. Document performance characteristics:
   - Expected throughput
   - Latency expectations
   - Resource usage
8. Add performance regression testing to CI
9. Create performance optimization guide

---

## 20. Implement cost tracking and reporting

### Steps:

1. Create cost tracking models:
   - `CostRecord` class
   - Track tokens used, API calls, storage
2. Add cost tracking to `IChatModel`:
   - Track token usage from responses
   - Calculate costs based on provider pricing
3. Add cost tracking to `IEmbeddingModel`:
   - Track embedding token usage
4. Add cost tracking to vector operations:
   - Track storage costs
   - Track query costs (if applicable)
5. Create cost repository:
   - Store cost records
   - Aggregate by tenant, time period
6. Add cost reporting:
   - Per-tenant cost reports
   - Time-series cost data
   - Cost breakdown by operation type
7. Create cost API endpoints:
   - Get cost for tenant
   - Get cost breakdown
   - Export cost data
8. Add cost alerts:
   - Budget thresholds
   - Unusual spending alerts
9. Create cost dashboard (optional)
10. Write unit tests
11. Update documentation
