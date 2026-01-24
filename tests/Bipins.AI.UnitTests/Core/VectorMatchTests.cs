using Bipins.AI.Core.Vector;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class VectorMatchTests
{
    [Fact]
    public void VectorMatch_WithAllProperties_CreatesSuccessfully()
    {
        var vector = new float[] { 0.1f, 0.2f, 0.3f }.AsMemory();
        var metadata = new Dictionary<string, object> { { "docId", "doc1" } };
        var record = new VectorRecord("match_1", vector, "Text content", metadata);
        var match = new VectorMatch(record, 0.95f);

        Assert.Equal("match_1", match.Record.Id);
        Assert.Equal(3, match.Record.Vector.Length);
        Assert.Equal(0.95f, match.Score);
        Assert.Equal("Text content", match.Record.Text);
        Assert.NotNull(match.Record.Metadata);
        Assert.Equal("doc1", match.Record.Metadata["docId"]);
    }

    [Fact]
    public void VectorMatch_WithMinimalProperties_CreatesSuccessfully()
    {
        var vector = new float[] { 0.1f, 0.2f }.AsMemory();
        var record = new VectorRecord("match_2", vector, "");
        var match = new VectorMatch(record, 0.85f);

        Assert.Equal("match_2", match.Record.Id);
        Assert.Equal(0.85f, match.Score);
    }

    [Fact]
    public void VectorMatch_WithZeroScore_HandlesCorrectly()
    {
        var vector = new float[] { 0.1f }.AsMemory();
        var record = new VectorRecord("zero_score", vector, "");
        var match = new VectorMatch(record, 0.0f);

        Assert.Equal(0.0f, match.Score);
    }

    [Fact]
    public void VectorMatch_WithPerfectScore_HandlesCorrectly()
    {
        var vector = new float[] { 0.1f }.AsMemory();
        var record = new VectorRecord("perfect", vector, "");
        var match = new VectorMatch(record, 1.0f);

        Assert.Equal(1.0f, match.Score);
    }
}
