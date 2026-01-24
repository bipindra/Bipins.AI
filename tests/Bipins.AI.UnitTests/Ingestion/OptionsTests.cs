using Bipins.AI.Core.Ingestion;
using Xunit;

namespace Bipins.AI.UnitTests.Ingestion;

public class OptionsTests
{
    [Fact]
    public void ChunkOptions_WithAllProperties_CreatesSuccessfully()
    {
        var options = new ChunkOptions(500, 50);

        Assert.Equal(500, options.MaxSize);
        Assert.Equal(50, options.Overlap);
    }

    [Fact]
    public void ChunkOptions_WithDefaultValues_CreatesSuccessfully()
    {
        var options = new ChunkOptions(1000, 0);

        Assert.Equal(1000, options.MaxSize);
        Assert.Equal(0, options.Overlap);
    }

    [Fact]
    public void IndexOptions_WithAllProperties_CreatesSuccessfully()
    {
        var options = new IndexOptions("tenant-1", "doc-1", "v1", "collection-1", UpdateMode.Update, true);

        Assert.Equal("tenant-1", options.TenantId);
        Assert.Equal("doc-1", options.DocId);
        Assert.Equal("v1", options.VersionId);
        Assert.Equal("collection-1", options.CollectionName);
        Assert.Equal(UpdateMode.Update, options.UpdateMode);
        Assert.True(options.DeleteOldVersions);
    }

    [Fact]
    public void IndexOptions_WithMinimalProperties_CreatesSuccessfully()
    {
        var options = new IndexOptions("tenant-1");

        Assert.Equal("tenant-1", options.TenantId);
        Assert.Null(options.DocId);
        Assert.Null(options.VersionId);
        Assert.Null(options.CollectionName);
    }
}
