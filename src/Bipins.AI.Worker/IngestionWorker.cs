using System.Threading.Channels;
using Bipins.AI;
using Bipins.AI.Ingestion;
using Bipins.AI.Worker.Ingestion;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Worker;

/// <summary>
/// Background service for processing ingestion jobs from a queue.
/// </summary>
public class IngestionWorker : BackgroundService
{
    private readonly ILogger<IngestionWorker> _logger;
    private readonly IngestionPipeline _pipeline;
    private readonly Channel<IngestionJob> _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionWorker"/> class.
    /// </summary>
    public IngestionWorker(
        ILogger<IngestionWorker> logger,
        IngestionPipeline pipeline)
    {
        _logger = logger;
        _pipeline = pipeline;

        // Create in-memory channel (stubbed for v1)
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<IngestionJob>(options);
    }

    /// <summary>
    /// Enqueues an ingestion job.
    /// </summary>
    public async Task EnqueueAsync(IngestionJob job, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(job, cancellationToken);
        _logger.LogInformation("Enqueued ingestion job for {SourceUri}", job.SourceUri);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ingestion worker started");

        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing ingestion job for {SourceUri}", job.SourceUri);

                var result = await _pipeline.IngestAsync(
                    job.SourceUri,
                    job.Options,
                    job.ChunkOptions,
                    stoppingToken);

                _logger.LogInformation(
                    "Completed ingestion job: {ChunksIndexed} chunks indexed, {VectorsCreated} vectors created",
                    result.ChunksIndexed,
                    result.VectorsCreated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ingestion job for {SourceUri}", job.SourceUri);
            }
        }

        _logger.LogInformation("Ingestion worker stopped");
    }
}
