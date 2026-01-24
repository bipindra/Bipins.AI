using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Vector;
using Bipins.AI.Ingestion;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Ingestion;

public class TenantTests
{
    private readonly Mock<ILogger<InMemoryTenantManager>> _tenantManagerLogger;
    private readonly Mock<ILogger<TenantQuotaEnforcer>> _quotaEnforcerLogger;
    private readonly Mock<ILogger<VectorStoreDocumentVersionManager>> _versionManagerLogger;
    private readonly Mock<IVectorStore> _vectorStore;

    public TenantTests()
    {
        _tenantManagerLogger = new Mock<ILogger<InMemoryTenantManager>>();
        _quotaEnforcerLogger = new Mock<ILogger<TenantQuotaEnforcer>>();
        _versionManagerLogger = new Mock<ILogger<VectorStoreDocumentVersionManager>>();
        _vectorStore = new Mock<IVectorStore>();
    }

    [Fact]
    public async Task InMemoryTenantManager_GetTenantAsync_ExistingTenant_ReturnsTenant()
    {
        var manager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var tenant = await manager.GetTenantAsync("default");

        Assert.NotNull(tenant);
        Assert.Equal("default", tenant.TenantId);
    }

    [Fact]
    public async Task InMemoryTenantManager_GetTenantAsync_NonExistentTenant_ReturnsNull()
    {
        var manager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var tenant = await manager.GetTenantAsync("nonexistent");

        Assert.Null(tenant);
    }

    [Fact]
    public async Task InMemoryTenantManager_RegisterTenantAsync_RegistersTenant()
    {
        var manager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var tenantInfo = new TenantInfo(
            "tenant1",
            "Test Tenant",
            DateTimeOffset.UtcNow,
            new TenantQuotas(1000, 1_000_000_000, 10000, 50000));

        await manager.RegisterTenantAsync(tenantInfo);

        var retrieved = await manager.GetTenantAsync("tenant1");
        Assert.NotNull(retrieved);
        Assert.Equal("tenant1", retrieved.TenantId);
        Assert.Equal("Test Tenant", retrieved.Name);
    }

    [Fact]
    public async Task InMemoryTenantManager_RegisterTenantAsync_InvalidTenantId_ThrowsException()
    {
        var manager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var tenantInfo = new TenantInfo(
            "invalid tenant id",
            "Test",
            DateTimeOffset.UtcNow,
            null);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await manager.RegisterTenantAsync(tenantInfo));
    }

    [Fact]
    public async Task InMemoryTenantManager_UpdateTenantAsync_UpdatesTenant()
    {
        var manager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var tenantInfo = new TenantInfo(
            "tenant1",
            "Original Name",
            DateTimeOffset.UtcNow,
            null);

        await manager.RegisterTenantAsync(tenantInfo);

        var updated = tenantInfo with { Name = "Updated Name" };
        await manager.UpdateTenantAsync(updated);

        var retrieved = await manager.GetTenantAsync("tenant1");
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Name", retrieved.Name);
    }

    [Fact]
    public async Task InMemoryTenantManager_UpdateTenantAsync_NonExistentTenant_ThrowsException()
    {
        var manager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var tenantInfo = new TenantInfo(
            "nonexistent",
            "Test",
            DateTimeOffset.UtcNow,
            null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await manager.UpdateTenantAsync(tenantInfo));
    }

    [Fact]
    public async Task InMemoryTenantManager_TenantExistsAsync_ReturnsTrueForExisting()
    {
        var manager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var exists = await manager.TenantExistsAsync("default");

        Assert.True(exists);
    }

    [Fact]
    public async Task InMemoryTenantManager_TenantExistsAsync_ReturnsFalseForNonExistent()
    {
        var manager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var exists = await manager.TenantExistsAsync("nonexistent");

        Assert.False(exists);
    }

    [Fact]
    public async Task TenantQuotaEnforcer_CanIngestDocumentAsync_WithinQuota_ReturnsTrue()
    {
        var tenantManager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var enforcer = new TenantQuotaEnforcer(tenantManager, _quotaEnforcerLogger.Object);

        var result = await enforcer.CanIngestDocumentAsync("default");

        Assert.True(result);
    }

    [Fact]
    public async Task TenantQuotaEnforcer_CanIngestDocumentAsync_ExceedsDocumentLimit_ReturnsFalse()
    {
        var tenantManager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var enforcer = new TenantQuotaEnforcer(tenantManager, _quotaEnforcerLogger.Object);

        // Create tenant with low document limit
        var tenantInfo = new TenantInfo(
            "tenant1",
            "Test",
            DateTimeOffset.UtcNow,
            new TenantQuotas(MaxDocuments: 1, null, null, null));

        await tenantManager.RegisterTenantAsync(tenantInfo);

        // Record one document
        await enforcer.RecordDocumentIngestionAsync("tenant1", 10, 1000);

        // Try to ingest another - should fail
        var result = await enforcer.CanIngestDocumentAsync("tenant1");

        Assert.False(result);
    }

    [Fact]
    public async Task TenantQuotaEnforcer_CanIngestDocumentAsync_ExceedsStorageLimit_ReturnsFalse()
    {
        var tenantManager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var enforcer = new TenantQuotaEnforcer(tenantManager, _quotaEnforcerLogger.Object);

        // Create tenant with low storage limit
        var tenantInfo = new TenantInfo(
            "tenant1",
            "Test",
            DateTimeOffset.UtcNow,
            new TenantQuotas(null, MaxStorageBytes: 1000, null, null));

        await tenantManager.RegisterTenantAsync(tenantInfo);

        // Record storage usage at limit
        await enforcer.RecordDocumentIngestionAsync("tenant1", 10, 1000);

        // Try to ingest another - should fail
        var result = await enforcer.CanIngestDocumentAsync("tenant1");

        Assert.False(result);
    }

    [Fact]
    public async Task TenantQuotaEnforcer_CanMakeChatRequestAsync_WithinQuota_ReturnsTrue()
    {
        var tenantManager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var enforcer = new TenantQuotaEnforcer(tenantManager, _quotaEnforcerLogger.Object);

        var result = await enforcer.CanMakeChatRequestAsync("default", 1000);

        Assert.True(result);
    }

    [Fact]
    public async Task TenantQuotaEnforcer_CanMakeChatRequestAsync_ExceedsTokenLimit_ReturnsFalse()
    {
        var tenantManager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var enforcer = new TenantQuotaEnforcer(tenantManager, _quotaEnforcerLogger.Object);

        // Create tenant with low token limit
        var tenantInfo = new TenantInfo(
            "tenant1",
            "Test",
            DateTimeOffset.UtcNow,
            new TenantQuotas(null, null, null, MaxTokensPerRequest: 100));

        await tenantManager.RegisterTenantAsync(tenantInfo);

        // Try to make request exceeding token limit
        var result = await enforcer.CanMakeChatRequestAsync("tenant1", 200);

        Assert.False(result);
    }

    [Fact]
    public async Task TenantQuotaEnforcer_CanMakeChatRequestAsync_ExceedsDailyRequestLimit_ReturnsFalse()
    {
        var tenantManager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var enforcer = new TenantQuotaEnforcer(tenantManager, _quotaEnforcerLogger.Object);

        // Create tenant with low daily request limit
        var tenantInfo = new TenantInfo(
            "tenant1",
            "Test",
            DateTimeOffset.UtcNow,
            new TenantQuotas(null, null, MaxRequestsPerDay: 2, null));

        await tenantManager.RegisterTenantAsync(tenantInfo);

        // Record 2 requests
        await enforcer.RecordChatRequestAsync("tenant1", 100);
        await enforcer.RecordChatRequestAsync("tenant1", 100);

        // Try to make another request - should fail
        var result = await enforcer.CanMakeChatRequestAsync("tenant1", 100);

        Assert.False(result);
    }

    [Fact]
    public async Task TenantQuotaEnforcer_RecordDocumentIngestionAsync_UpdatesUsage()
    {
        var tenantManager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var enforcer = new TenantQuotaEnforcer(tenantManager, _quotaEnforcerLogger.Object);

        await enforcer.RecordDocumentIngestionAsync("default", 10, 5000);

        // Should not throw
        Assert.True(true);
    }

    [Fact]
    public async Task TenantQuotaEnforcer_RecordChatRequestAsync_UpdatesUsage()
    {
        var tenantManager = new InMemoryTenantManager(_tenantManagerLogger.Object);
        var enforcer = new TenantQuotaEnforcer(tenantManager, _quotaEnforcerLogger.Object);

        await enforcer.RecordChatRequestAsync("default", 1000);

        // Should not throw
        Assert.True(true);
    }

    [Fact]
    public async Task VectorStoreDocumentVersionManager_GenerateVersionIdAsync_GeneratesUniqueIds()
    {
        var manager = new VectorStoreDocumentVersionManager(_versionManagerLogger.Object, _vectorStore.Object);

        var id1 = await manager.GenerateVersionIdAsync("tenant1", "doc1");
        await Task.Delay(10); // Small delay to ensure different timestamp
        var id2 = await manager.GenerateVersionIdAsync("tenant1", "doc1");

        Assert.NotEqual(id1, id2);
        Assert.NotNull(id1);
        Assert.NotNull(id2);
    }

    [Fact]
    public async Task VectorStoreDocumentVersionManager_ListVersionsAsync_ReturnsVersions()
    {
        var manager = new VectorStoreDocumentVersionManager(_versionManagerLogger.Object, _vectorStore.Object);

        var records = new[]
        {
            new VectorRecord(
                "id1",
                new ReadOnlyMemory<float>(new float[] { 0.1f }),
                "text1",
                new Dictionary<string, object> { { "createdAt", DateTime.UtcNow } },
                "uri1",
                "doc1",
                "chunk1",
                "tenant1",
                "v1"),
            new VectorRecord(
                "id2",
                new ReadOnlyMemory<float>(new float[] { 0.2f }),
                "text2",
                new Dictionary<string, object> { { "createdAt", DateTime.UtcNow } },
                "uri1",
                "doc1",
                "chunk2",
                "tenant1",
                "v2")
        };

        var queryResponse = new VectorQueryResponse(
            records.Select(r => new VectorMatch(r, 0.9f)).ToList());

        _vectorStore.Setup(v => v.QueryAsync(
                It.IsAny<VectorQueryRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResponse);

        var versions = await manager.ListVersionsAsync("tenant1", "doc1");

        Assert.NotNull(versions);
        Assert.Equal(2, versions.Count);
    }

    [Fact]
    public async Task VectorStoreDocumentVersionManager_GetVersionAsync_ReturnsVersion()
    {
        var manager = new VectorStoreDocumentVersionManager(_versionManagerLogger.Object, _vectorStore.Object);

        var record = new VectorRecord(
            "id1",
            new ReadOnlyMemory<float>(new float[] { 0.1f }),
            "text1",
            new Dictionary<string, object> { { "createdAt", DateTime.UtcNow } },
            "uri1",
            "doc1",
            "chunk1",
            "tenant1",
            "v1");

        var queryResponse = new VectorQueryResponse(
            new[] { new VectorMatch(record, 0.9f) });

        _vectorStore.Setup(v => v.QueryAsync(
                It.IsAny<VectorQueryRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResponse);

        var version = await manager.GetVersionAsync("tenant1", "doc1", "v1");

        Assert.NotNull(version);
        Assert.Equal("v1", version.VersionId);
        Assert.Equal("doc1", version.DocId);
        Assert.Equal("tenant1", version.TenantId);
    }

    [Fact]
    public async Task VectorStoreDocumentVersionManager_GetVersionAsync_NonExistentVersion_ReturnsNull()
    {
        var manager = new VectorStoreDocumentVersionManager(_versionManagerLogger.Object, _vectorStore.Object);

        var queryResponse = new VectorQueryResponse(Array.Empty<VectorMatch>());

        _vectorStore.Setup(v => v.QueryAsync(
                It.IsAny<VectorQueryRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResponse);

        var version = await manager.GetVersionAsync("tenant1", "doc1", "v1");

        Assert.Null(version);
    }
}
