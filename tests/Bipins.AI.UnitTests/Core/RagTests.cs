using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Rag;
using Bipins.AI.Core.Vector;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class RagTests
{
    [Fact]
    public void RetrieveRequest_Creation_SetsProperties()
    {
        var filter = new VectorFilterPredicate(new FilterPredicate("field1", FilterOperator.Eq, "value1"));
        var request = new RetrieveRequest(
            Query: "test query",
            TenantId: "tenant1",
            TopK: 10,
            Filter: filter,
            CollectionName: "collection1");

        Assert.Equal("test query", request.Query);
        Assert.Equal("tenant1", request.TenantId);
        Assert.Equal(10, request.TopK);
        Assert.Equal(filter, request.Filter);
        Assert.Equal("collection1", request.CollectionName);
    }

    [Fact]
    public void RetrieveRequest_DefaultTopK_IsFive()
    {
        var request = new RetrieveRequest("query", "tenant1");

        Assert.Equal(5, request.TopK);
    }

    [Fact]
    public void RetrieveRequest_WithoutFilter_CreatesRequest()
    {
        var request = new RetrieveRequest("query", "tenant1", TopK: 3);

        Assert.Equal("query", request.Query);
        Assert.Equal("tenant1", request.TenantId);
        Assert.Equal(3, request.TopK);
        Assert.Null(request.Filter);
        Assert.Null(request.CollectionName);
    }

    [Fact]
    public void RetrieveResult_Creation_SetsProperties()
    {
        var chunks = new[]
        {
            new RagChunk(
                new Chunk("chunk1", "text1", 0, 5),
                0.95f,
                "uri1",
                "doc1")
        };
        var queryVector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f });

        var result = new RetrieveResult(chunks, queryVector, 10);

        Assert.Equal(chunks, result.Chunks);
        Assert.Equal(queryVector, result.QueryVector);
        Assert.Equal(10, result.TotalMatches);
    }

    [Fact]
    public void RetrieveResult_EmptyChunks_CreatesResult()
    {
        var queryVector = new ReadOnlyMemory<float>(new float[] { 0.1f });
        var result = new RetrieveResult(Array.Empty<RagChunk>(), queryVector, 0);

        Assert.Empty(result.Chunks);
        Assert.Equal(0, result.TotalMatches);
    }

    [Fact]
    public void RagChunk_Creation_SetsProperties()
    {
        var chunk = new Chunk("chunk1", "text", 0, 4);
        var ragChunk = new RagChunk(
            Chunk: chunk,
            Score: 0.95f,
            SourceUri: "uri1",
            DocId: "doc1");

        Assert.Equal(chunk, ragChunk.Chunk);
        Assert.Equal(0.95f, ragChunk.Score);
        Assert.Equal("uri1", ragChunk.SourceUri);
        Assert.Equal("doc1", ragChunk.DocId);
    }

    [Fact]
    public void RagChunk_WithoutOptionalProperties_CreatesChunk()
    {
        var chunk = new Chunk("chunk1", "text", 0, 4);
        var ragChunk = new RagChunk(chunk, 0.8f);

        Assert.Equal(chunk, ragChunk.Chunk);
        Assert.Equal(0.8f, ragChunk.Score);
        Assert.Null(ragChunk.SourceUri);
        Assert.Null(ragChunk.DocId);
    }

    [Fact]
    public void RagChunk_Score_IsBetweenZeroAndOne()
    {
        var chunk = new Chunk("chunk1", "text", 0, 4);
        var ragChunk = new RagChunk(chunk, 0.5f);

        Assert.True(ragChunk.Score >= 0.0f && ragChunk.Score <= 1.0f);
    }
}
