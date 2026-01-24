using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Bipins.AI.Core.Vector;
using System.Collections.Generic;
using System.Linq;

namespace Bipins.AI.Benchmarks;

/// <summary>
/// Benchmarks for vector query operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class VectorQueryBenchmarks
{
    private readonly List<VectorRecord> _vectors;
    private readonly float[] _queryVector;

    public VectorQueryBenchmarks()
    {
        var random = new System.Random(42);
        _vectors = new List<VectorRecord>();
        
        // Generate 1000 vectors
        for (int i = 0; i < 1000; i++)
        {
            var vector = new float[1536];
            for (int j = 0; j < 1536; j++)
            {
                vector[j] = (float)(random.NextDouble() * 2 - 1);
            }
            
            _vectors.Add(new VectorRecord(
                Id: $"vector_{i}",
                Vector: new ReadOnlyMemory<float>(vector),
                Text: $"Text for vector {i}",
                Metadata: new Dictionary<string, object> { { "index", i } }));
        }

        // Generate query vector
        _queryVector = new float[1536];
        for (int i = 0; i < 1536; i++)
        {
            _queryVector[i] = (float)(random.NextDouble() * 2 - 1);
        }
    }

    [Benchmark]
    public void CosineSimilarity_1000Vectors()
    {
        var results = _vectors
            .Select(v => new
            {
                Record = v,
                Score = CosineSimilarity(_queryVector, v.Vector.Span.ToArray())
            })
            .OrderByDescending(x => x.Score)
            .Take(10)
            .ToList();
    }

    [Benchmark]
    public void CosineSimilarity_100Vectors()
    {
        var subset = _vectors.Take(100).ToList();
        var results = subset
            .Select(v => new
            {
                Record = v,
                Score = CosineSimilarity(_queryVector, v.Vector.Span.ToArray())
            })
            .OrderByDescending(x => x.Score)
            .Take(10)
            .ToList();
    }

    [Benchmark]
    public void CosineSimilarity_10000Vectors()
    {
        // Generate more vectors for this benchmark
        var random = new System.Random(42);
        var vectors = new List<VectorRecord>();
        
        for (int i = 0; i < 10000; i++)
        {
            var vector = new float[1536];
            for (int j = 0; j < 1536; j++)
            {
                vector[j] = (float)(random.NextDouble() * 2 - 1);
            }
            
            vectors.Add(new VectorRecord(
                Id: $"vector_{i}",
                Vector: new ReadOnlyMemory<float>(vector),
                Text: $"Text for vector {i}",
                Metadata: new Dictionary<string, object> { { "index", i } }));
        }

        var results = vectors
            .Select(v => new
            {
                Record = v,
                Score = CosineSimilarity(_queryVector, v.Vector.Span.ToArray())
            })
            .OrderByDescending(x => x.Score)
            .Take(10)
            .ToList();
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        float dotProduct = 0;
        float normA = 0;
        float normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
