using Bipins.AI.Core.Ingestion;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class IngestionTests
{
    [Fact]
    public void ChunkOptions_DefaultValues_AreCorrect()
    {
        var options = new ChunkOptions();

        Assert.Equal(1000, options.MaxSize);
        Assert.Equal(200, options.Overlap);
        Assert.Equal(ChunkStrategy.FixedSize, options.Strategy);
    }

    [Fact]
    public void ChunkOptions_CustomValues_AreSet()
    {
        var options = new ChunkOptions(
            MaxSize: 500,
            Overlap: 50,
            Strategy: ChunkStrategy.MarkdownAware);

        Assert.Equal(500, options.MaxSize);
        Assert.Equal(50, options.Overlap);
        Assert.Equal(ChunkStrategy.MarkdownAware, options.Strategy);
    }

    [Fact]
    public void IndexOptions_RequiredProperties_AreSet()
    {
        var options = new IndexOptions("tenant1");

        Assert.Equal("tenant1", options.TenantId);
        Assert.Null(options.DocId);
        Assert.Null(options.VersionId);
        Assert.Null(options.CollectionName);
        Assert.Equal(UpdateMode.Upsert, options.UpdateMode);
        Assert.False(options.DeleteOldVersions);
    }

    [Fact]
    public void IndexOptions_AllProperties_AreSet()
    {
        var options = new IndexOptions(
            TenantId: "tenant1",
            DocId: "doc1",
            VersionId: "v1",
            CollectionName: "collection1",
            UpdateMode: UpdateMode.Upsert,
            DeleteOldVersions: true);

        Assert.Equal("tenant1", options.TenantId);
        Assert.Equal("doc1", options.DocId);
        Assert.Equal("v1", options.VersionId);
        Assert.Equal("collection1", options.CollectionName);
        Assert.Equal(UpdateMode.Upsert, options.UpdateMode);
        Assert.True(options.DeleteOldVersions);
    }

    [Theory]
    [InlineData("tenant1", true)]
    [InlineData("tenant-1", true)]
    [InlineData("tenant_1", true)]
    [InlineData("Tenant1", true)]
    [InlineData("TENANT1", true)]
    [InlineData("tenant123", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    [InlineData("tenant with spaces", false)]
    [InlineData("tenant@special", false)]
    [InlineData("tenant.with.dots", false)]
    public void TenantValidator_IsValid_ValidatesFormat(string? tenantId, bool expected)
    {
        var result = TenantValidator.IsValid(tenantId);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TenantValidator_IsValid_RejectsLongTenantId()
    {
        var longTenantId = new string('a', 101);
        var result = TenantValidator.IsValid(longTenantId);

        Assert.False(result);
    }

    [Fact]
    public void TenantValidator_IsValid_AcceptsMaxLengthTenantId()
    {
        var maxLengthTenantId = new string('a', 100);
        var result = TenantValidator.IsValid(maxLengthTenantId);

        Assert.True(result);
    }

    [Fact]
    public void TenantValidator_ValidateOrThrow_ValidTenantId_DoesNotThrow()
    {
        TenantValidator.ValidateOrThrow("tenant1");
        // Should not throw
    }

    [Fact]
    public void TenantValidator_ValidateOrThrow_InvalidTenantId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TenantValidator.ValidateOrThrow("invalid tenant"));
    }

    [Fact]
    public void TenantValidator_ValidateOrThrow_NullTenantId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TenantValidator.ValidateOrThrow(null));
    }

    [Fact]
    public void Chunk_Creation_SetsProperties()
    {
        var metadata = new Dictionary<string, object> { { "key", "value" } };
        var chunk = new Chunk(
            Id: "chunk1",
            Text: "chunk text",
            StartIndex: 0,
            EndIndex: 10,
            Metadata: metadata);

        Assert.Equal("chunk1", chunk.Id);
        Assert.Equal("chunk text", chunk.Text);
        Assert.Equal(0, chunk.StartIndex);
        Assert.Equal(10, chunk.EndIndex);
        Assert.Equal(metadata, chunk.Metadata);
    }

    [Fact]
    public void Chunk_WithoutMetadata_CreatesChunk()
    {
        var chunk = new Chunk("chunk1", "text", 0, 4);

        Assert.Equal("chunk1", chunk.Id);
        Assert.Equal("text", chunk.Text);
        Assert.Null(chunk.Metadata);
    }

    [Fact]
    public void Chunk_EndIndex_GreaterThanStartIndex()
    {
        var chunk = new Chunk("chunk1", "text", 0, 4);

        Assert.True(chunk.EndIndex > chunk.StartIndex);
    }
}
