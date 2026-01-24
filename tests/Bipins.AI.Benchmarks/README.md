# Bipins.AI Performance Benchmarks

This project contains performance benchmarks for the Bipins.AI platform using BenchmarkDotNet.

## Running Benchmarks

### Run all benchmarks:
```bash
dotnet run -c Release
```

### Run specific benchmark:
```bash
dotnet run -c Release -- --filter "*ChunkingBenchmarks*"
```

## Benchmark Categories

### Chunking Benchmarks
- Tests different chunking strategies (FixedSize, SentenceAware, Paragraph, MarkdownAware)
- Tests with different text sizes (1KB, 10KB, 100KB)
- Measures memory allocation and execution time

### Vector Query Benchmarks
- Tests cosine similarity calculations
- Tests with different vector collection sizes (100, 1000, 10000)
- Measures query performance for top-K retrieval

### Ingestion Pipeline Benchmarks
- Tests full ingestion pipeline with different document sizes
- Measures end-to-end performance
- Tests small, medium, and large document ingestion

## Results

Benchmark results are saved in the `BenchmarkDotNet.Artifacts` directory after each run.

## Performance Characteristics

### Expected Throughput
- **Chunking**: ~10-50 MB/s depending on strategy
- **Vector Queries**: ~1000-10000 queries/second depending on collection size
- **Ingestion**: ~1-10 documents/second depending on document size

### Latency Expectations
- **Chunking**: <10ms for 10KB text
- **Vector Queries**: <50ms for 1000 vectors, <500ms for 10000 vectors
- **Ingestion**: <1s for small documents, <10s for large documents

### Resource Usage
- **Memory**: Chunking uses ~2-5x input size in memory
- **CPU**: Vector queries are CPU-bound, benefit from parallelization

## Performance Optimization Guide

1. **Chunking**: Use FixedSize for speed, MarkdownAware for quality
2. **Vector Queries**: Use approximate nearest neighbor (ANN) for large collections
3. **Ingestion**: Batch processing improves throughput
4. **Caching**: Cache embeddings and chunked text when possible
