using Bipins.AI.Core.Models;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class EmbeddingTests
{
    [Fact]
    public void EmbeddingRequest_WithSingleInput_CreatesSuccessfully()
    {
        var inputs = new List<string> { "Hello world" };
        var request = new EmbeddingRequest(inputs);

        Assert.Single(request.Inputs);
        Assert.Equal("Hello world", request.Inputs[0]);
    }

    [Fact]
    public void EmbeddingRequest_WithMultipleInputs_CreatesSuccessfully()
    {
        var inputs = new List<string> { "Text 1", "Text 2", "Text 3" };
        var request = new EmbeddingRequest(inputs);

        Assert.Equal(3, request.Inputs.Count);
    }

    [Fact]
    public void EmbeddingRequest_WithMetadata_IncludesMetadata()
    {
        var inputs = new List<string> { "Hello" };
        var metadata = new Dictionary<string, object> { { "modelId", "text-embedding-ada-002" } };
        var request = new EmbeddingRequest(inputs, Metadata: metadata);

        Assert.NotNull(request.Metadata);
        Assert.Equal("text-embedding-ada-002", request.Metadata["modelId"]);
    }

    [Fact]
    public void EmbeddingResponse_WithVectors_CreatesSuccessfully()
    {
        var vectors = new List<ReadOnlyMemory<float>>
        {
            new float[] { 0.1f, 0.2f, 0.3f }.AsMemory(),
            new float[] { 0.4f, 0.5f, 0.6f }.AsMemory()
        };

        var usage = new Usage(10, 0, 10);
        var response = new EmbeddingResponse(vectors, usage, "text-embedding-ada-002");

        Assert.Equal(2, response.Vectors.Count);
        Assert.Equal(3, response.Vectors[0].Length);
        Assert.Equal("text-embedding-ada-002", response.ModelId);
        Assert.NotNull(response.Usage);
    }

    [Fact]
    public void EmbeddingResponse_WithMinimalProperties_CreatesSuccessfully()
    {
        var vectors = new List<ReadOnlyMemory<float>>
        {
            new float[] { 0.1f, 0.2f }.AsMemory()
        };

        var response = new EmbeddingResponse(vectors);

        Assert.Single(response.Vectors);
        Assert.Null(response.Usage);
        Assert.Null(response.ModelId);
    }
}
