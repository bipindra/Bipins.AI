using Bipins.AI.Core.Ingestion;
using Bipins.AI.Core.Models;
using Bipins.AI.Ingestion;
using Bipins.AI.Runtime.Routing;
using Bipins.AI.Vector;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Ingestion;

public class IndexerUpdateRegressionTests
{
    private static DefaultIndexer CreateIndexer(RecordingStore store)
    {
        var model = new Mock<IEmbeddingModel>();
        model.Setup(m => m.EmbedAsync(It.IsAny<EmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddingResponse(new[] { new ReadOnlyMemory<float>(new float[] { 1, 2, 3 }) }, null, "test"));
        var router = new Mock<IModelRouter>();
        router.Setup(r => r.SelectEmbeddingModelAsync(It.IsAny<string>(), It.IsAny<EmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model.Object);
        return new DefaultIndexer(NullLogger<DefaultIndexer>.Instance, router.Object, store);
    }

    private static IndexOptions UpdateOptions(string? version = "v2") =>
        new("tenant-a", "doc-a", version, UpdateMode: UpdateMode.Update, DeleteOldVersions: true);

    [Fact]
    public async Task FailedUpsert_PreservesOldRecords_AndNeverDeletes()
    {
        var store = new RecordingStore { FailUpsert = true };
        store.AddOld("shared-chunk-id");
        var result = await CreateIndexer(store).IndexAsync(new[] { new Chunk("shared-chunk-id", "new", 0, 3) }, UpdateOptions());
        Assert.NotEmpty(result.Errors!);
        Assert.Equal("old", store.Records["shared-chunk-id"].Text);
        Assert.Equal(0, store.DeletedCount);
        Assert.Equal(new[] { "upsert" }, store.Events);
    }

    [Fact]
    public async Task Cleanup_PagesPastTenThousand_UsesActualDimensions_AndPreservesOtherTenants()
    {
        var store = new RecordingStore();
        for (var i = 0; i < 10001; i++) store.AddOld("old-" + i);
        store.AddOld("other-tenant", "tenant-b");
        store.AddOld("other-doc", doc: "doc-b");
        var result = await CreateIndexer(store).IndexAsync(new[] { new Chunk("new", "new", 0, 3) }, UpdateOptions());
        Assert.Null(result.Errors);
        Assert.Equal(10001, store.DeletedCount);
        Assert.Equal(3, store.Records.Count);
        Assert.Equal("upsert", store.Events[0]);
        Assert.All(store.QueryDimensions, d => Assert.Equal(3, d));
        Assert.Contains("other-tenant", store.Records.Keys);
        Assert.Contains("other-doc", store.Records.Keys);
        Assert.Contains(store.Records.Values, r => r.VersionId == "v2");
    }

    [Fact]
    public async Task CleanupFailure_ReportsPartialResult_AndPreservesReplacement()
    {
        var store = new RecordingStore { FailDelete = true };
        store.AddOld("old");
        var result = await CreateIndexer(store).IndexAsync(new[] { new Chunk("new", "new", 0, 3) }, UpdateOptions());
        Assert.NotEmpty(result.Errors!);
        Assert.Equal(1, result.VectorsCreated);
        Assert.Equal(2, store.Records.Count);
    }

    [Fact]
    public async Task MissingVersion_GeneratesVersion_AndCannotDeleteReplacement()
    {
        var store = new RecordingStore();
        store.AddOld("old");
        var result = await CreateIndexer(store).IndexAsync(new[] { new Chunk("new", "new", 0, 3) }, UpdateOptions(null));
        Assert.Null(result.Errors);
        var record = Assert.Single(store.Records.Values);
        Assert.False(string.IsNullOrWhiteSpace(record.VersionId));
    }

    [Fact]
    public async Task Metadata_CannotOverrideTenantOrVersion()
    {
        var store = new RecordingStore();
        var chunk = new Chunk("new", "new", 0, 3, new Dictionary<string, object>
        {
            ["tenantId"] = "tenant-b", ["docId"] = "doc-b", ["versionId"] = "v1"
        });
        var result = await CreateIndexer(store).IndexAsync(new[] { chunk }, UpdateOptions());
        Assert.Null(result.Errors);
        var record = Assert.Single(store.Records.Values);
        Assert.Equal("tenant-a", record.Metadata!["tenantId"]);
        Assert.Equal("doc-a", record.Metadata["docId"]);
        Assert.Equal("v2", record.Metadata["versionId"]);
    }

    [Fact]
    public async Task Cancellation_IsPropagatedWithoutDeleting()
    {
        var store = new RecordingStore { CancelUpsert = true };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateIndexer(store).IndexAsync(new[] { new Chunk("new", "new", 0, 3) }, UpdateOptions(), cancellation.Token));
        Assert.Equal(0, store.DeletedCount);
    }

    private sealed class RecordingStore : IVectorStore
    {
        public Dictionary<string, VectorRecord> Records { get; } = new();
        public List<string> Events { get; } = new();
        public List<int> QueryDimensions { get; } = new();
        public int DeletedCount { get; private set; }
        public bool FailUpsert { get; init; }
        public bool FailDelete { get; init; }
        public bool CancelUpsert { get; init; }

        public void AddOld(string id, string tenant = "tenant-a", string doc = "doc-a") =>
            Records[id] = new VectorRecord(id, new float[] { 1, 2, 3 }, "old",
                new Dictionary<string, object> { ["tenantId"] = tenant, ["docId"] = doc, ["versionId"] = "v1" },
                DocId: doc, TenantId: tenant, VersionId: "v1");

        public Task UpsertAsync(VectorUpsertRequest request, CancellationToken cancellationToken = default)
        {
            Events.Add("upsert");
            if (CancelUpsert) cancellationToken.ThrowIfCancellationRequested();
            if (FailUpsert) throw new IOException("Write failed");
            foreach (var record in request.Records) Records[record.Id] = record;
            return Task.CompletedTask;
        }

        public Task<VectorQueryResponse> QueryAsync(VectorQueryRequest request, CancellationToken cancellationToken = default)
        {
            Events.Add("query");
            QueryDimensions.Add(request.QueryVector.Length);
            return Task.FromResult(new VectorQueryResponse(Records.Values
                .Where(r => Matches(r, request.Filter!)).Take(request.TopK).Select(r => new VectorMatch(r, 1)).ToList()));
        }

        private static bool Matches(VectorRecord record, VectorFilter filter) => filter switch
        {
            VectorFilterAnd and => and.Filters.All(f => Matches(record, f)),
            VectorFilterPredicate p => p.PredicateValue.Operator == FilterOperator.Eq
                ? Equals(record.Metadata![p.PredicateValue.Field], p.PredicateValue.Value)
                : !Equals(record.Metadata![p.PredicateValue.Field], p.PredicateValue.Value),
            _ => throw new NotSupportedException()
        };

        public Task DeleteAsync(VectorDeleteRequest request, CancellationToken cancellationToken = default)
        {
            Events.Add("delete");
            if (FailDelete) throw new IOException("Delete failed");
            foreach (var id in request.Ids)
                if (Records.Remove(id)) DeletedCount++;
            return Task.CompletedTask;
        }
    }
}
