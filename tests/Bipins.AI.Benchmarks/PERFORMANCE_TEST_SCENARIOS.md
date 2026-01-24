# Performance Test Scenarios

This document describes the performance test scenarios for the Bipins.AI platform.

## Test Categories

### 1. Micro-Benchmarks (BenchmarkDotNet)

These benchmarks measure individual component performance:

#### Chunking Benchmarks
- **FixedSize Chunking**: Tests fixed-size chunking with various text sizes
- **SentenceAware Chunking**: Tests sentence-aware chunking
- **Paragraph Chunking**: Tests paragraph-based chunking
- **MarkdownAware Chunking**: Tests markdown-aware chunking

**Run:**
```bash
dotnet run -c Release -- --filter "*ChunkingBenchmarks*"
```

#### Vector Query Benchmarks
- **Cosine Similarity**: Tests cosine similarity calculations
- **Top-K Retrieval**: Tests retrieval with different K values
- **Collection Size Impact**: Tests with 100, 1K, 10K vectors

**Run:**
```bash
dotnet run -c Release -- --filter "*VectorQueryBenchmarks*"
```

#### Ingestion Pipeline Benchmarks
- **Small Documents**: <10KB documents
- **Medium Documents**: 10KB-1MB documents
- **Large Documents**: >1MB documents
- **Batch Processing**: Multiple documents in parallel

**Run:**
```bash
dotnet run -c Release -- --filter "*IngestionPipelineBenchmarks*"
```

#### RAG Benchmarks
- **Retrieval**: Tests vector retrieval performance
- **Composition**: Tests RAG request composition
- **Full Pipeline**: End-to-end RAG pipeline

**Run:**
```bash
dotnet run -c Release -- --filter "*RagBenchmarks*"
```

#### API Endpoint Benchmarks
- **Ingest Text**: Tests text ingestion endpoint
- **Query**: Tests vector query endpoint
- **Chat**: Tests RAG chat endpoint
- **Health Check**: Tests health check endpoint

**Run:**
```bash
# Set environment variables first
export BIPINS_API_URL=http://localhost:5000
export BIPINS_API_KEY=your-api-key

dotnet run -c Release -- --filter "*ApiEndpointBenchmarks*"
```

### 2. Load Tests (k6)

These tests simulate real-world load scenarios:

#### Chat Endpoint Load Test
**Scenario**: Simulate multiple users chatting simultaneously

**Run:**
```bash
k6 run load-test-chat.js
```

**Configuration:**
- Ramp up: 0 → 5 users (30s)
- Sustained: 5 users (1m)
- Ramp up: 5 → 10 users (30s)
- Sustained: 10 users (1m)
- Ramp down: 10 → 0 users (30s)

**Thresholds:**
- 95% of requests < 5s
- Error rate < 10%

#### Ingestion Load Test
**Scenario**: Simulate batch document ingestion

**Run:**
```bash
k6 run load-test-ingest.js
```

**Configuration:**
- Ramp up: 0 → 10 users (30s)
- Sustained: 10 users (2m)
- Ramp down: 10 → 0 users (30s)

**Thresholds:**
- 95% of requests < 10s
- Error rate < 5%

### 3. Stress Tests

#### Maximum Throughput Test
**Goal**: Find the maximum requests per second the system can handle

**Run:**
```bash
k6 run --vus 50 --duration 5m load-test-chat.js
```

#### Memory Leak Test
**Goal**: Verify no memory leaks under sustained load

**Run:**
```bash
k6 run --vus 20 --duration 30m load-test-chat.js
```

Monitor memory usage throughout the test.

#### Concurrent Users Test
**Goal**: Test system behavior with high concurrency

**Run:**
```bash
k6 run --vus 100 --duration 10m load-test-chat.js
```

### 4. End-to-End Scenarios

#### Scenario 1: Document Ingestion Workflow
1. Ingest 100 documents (various sizes)
2. Query the vector store
3. Perform RAG chat queries
4. Measure end-to-end latency

**Expected Results:**
- Ingestion: < 10s per document (average)
- Query: < 100ms (p95)
- Chat: < 5s (p95)

#### Scenario 2: Multi-Tenant Isolation
1. Create 10 tenants
2. Ingest documents for each tenant
3. Query each tenant's data
4. Verify no cross-tenant data leakage

**Expected Results:**
- All queries return only tenant-specific data
- Performance consistent across tenants

#### Scenario 3: Cost Tracking
1. Perform various operations (ingest, chat, query)
2. Verify cost tracking accuracy
3. Check cost aggregation performance

**Expected Results:**
- Cost tracking: < 10ms overhead per operation
- Cost queries: < 100ms (p95)

## Performance Targets

### Latency Targets (p95)
- **Chunking**: < 10ms for 10KB text
- **Vector Queries**: < 50ms for 1K vectors, < 500ms for 10K vectors
- **Ingestion**: < 1s for small docs, < 10s for large docs
- **Chat**: < 5s
- **Query**: < 100ms
- **Health Check**: < 10ms

### Throughput Targets
- **Chunking**: > 10 MB/s
- **Vector Queries**: > 1,000 queries/second
- **Ingestion**: > 1 document/second
- **Chat**: > 0.2 requests/second (model-dependent)
- **API**: > 100 requests/second (overall)

### Resource Usage Targets
- **Memory**: < 2GB for typical workload
- **CPU**: < 80% utilization under normal load
- **Network**: Efficient use of bandwidth

## Running All Tests

### Full Benchmark Suite
```bash
cd tests/Bipins.AI.Benchmarks
dotnet run -c Release
```

### Full Load Test Suite
```bash
# Chat load test
k6 run load-test-chat.js

# Ingestion load test
k6 run load-test-ingest.js
```

### CI Integration
```yaml
- name: Run Performance Tests
  run: |
    cd tests/Bipins.AI.Benchmarks
    dotnet run -c Release
    
    # Run load tests (if k6 is installed)
    k6 run load-test-chat.js
    k6 run load-test-ingest.js
```

## Monitoring

### Key Metrics to Monitor
- **Latency**: p50, p95, p99 percentiles
- **Throughput**: Requests per second
- **Error Rate**: Percentage of failed requests
- **Resource Usage**: CPU, memory, network
- **Cost**: Per-operation costs

### Tools
- **BenchmarkDotNet**: For micro-benchmarks
- **k6**: For load testing
- **Application Insights**: For production monitoring
- **Prometheus/Grafana**: For metrics visualization

## Performance Regression Detection

### Baseline Comparison
Compare current results against baseline:
```bash
dotnet run -c Release -- --baseline
```

### Automated Alerts
Set up alerts for:
- Latency degradation > 20%
- Throughput reduction > 20%
- Error rate increase > 5%
- Memory usage increase > 50%

## Continuous Performance Testing

### Daily Benchmarks
Run benchmarks daily and compare against baseline:
```bash
dotnet run -c Release > results/$(date +%Y%m%d).txt
```

### Weekly Load Tests
Run comprehensive load tests weekly:
```bash
k6 run --out json=results/load-test-$(date +%Y%m%d).json load-test-chat.js
```

### Performance Budgets
Enforce performance budgets in CI:
- No regression > 10% in benchmark results
- All load tests must pass thresholds
- Memory usage must not increase > 20%
