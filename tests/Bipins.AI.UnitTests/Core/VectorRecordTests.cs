using Bipins.AI.Core.Vector;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class VectorRecordTests
{
    [Fact]
    public void VectorRecord_WithAllProperties_CreatesSuccessfully()
    {
        var vector = new float[] { 0.1f, 0.2f, 0.3f }.AsMemory();
        var metadata = new Dictionary<string, object> { { "tenantId", "tenant1" }, { "docId", "doc1" } };
        var record = new VectorRecord("record_1", vector, "Text content", metadata);

        Assert.Equal("record_1", record.Id);
        Assert.Equal(3, record.Vector.Length);
        Assert.Equal("Text content", record.Text);
        Assert.NotNull(record.Metadata);
        Assert.Equal("tenant1", record.Metadata["tenantId"]);
        Assert.Equal("doc1", record.Metadata["docId"]);
    }

    [Fact]
    public void VectorRecord_WithMinimalProperties_CreatesSuccessfully()
    {
        var vector = new float[] { 0.1f, 0.2f }.AsMemory();
        var record = new VectorRecord("record_2", vector, "");

        Assert.Equal("record_2", record.Id);
        Assert.Equal(2, record.Vector.Length);
        Assert.Equal("", record.Text);
        Assert.Null(record.Metadata);
    }

    [Fact]
    public void VectorRecord_WithEmptyVector_HandlesCorrectly()
    {
        var vector = Array.Empty<float>().AsMemory();
        var record = new VectorRecord("empty", vector, "");

        Assert.Equal(0, record.Vector.Length);
    }
}
