using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;
using MediaIngest.Worker.Watcher;
using MediaIngest.Workflow;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MediaIngest.Api;

public sealed class IngestApiApplication : IAsyncDisposable
{
    private readonly WebApplication webApplication;

    private IngestApiApplication(WebApplication webApplication, HttpClient httpClient)
    {
        this.webApplication = webApplication;
        HttpClient = httpClient;
    }

    public HttpClient HttpClient { get; }

    public static async Task<IngestApiApplication> StartAsync(
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        ConfigureServices(builder.Services, inputPath, outputPath);

        var app = builder.Build();
        MapEndpoints(app);

        await app.StartAsync(cancellationToken);

        var addresses = app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>();

        var address = addresses?.Addresses.Single()
            ?? throw new InvalidOperationException("The API host did not publish a server address.");

        return new IngestApiApplication(
            app,
            new HttpClient { BaseAddress = new Uri(address) });
    }

    public static Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateBuilder(args);
        var repoRoot = FindRepoRoot();
        var inputPath = builder.Configuration["Ingest:InputPath"]
            ?? Path.Combine(repoRoot, "input");
        var outputPath = builder.Configuration["Ingest:OutputPath"]
            ?? Path.Combine(repoRoot, "output");

        ConfigureServices(builder.Services, inputPath, outputPath);

        var app = builder.Build();
        MapEndpoints(app);

        return app.RunAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        await webApplication.DisposeAsync();
    }

    private static void ConfigureServices(IServiceCollection services, string inputPath, string outputPath)
    {
        services.AddSingleton(new IngestRuntimePaths(
            Path.GetFullPath(inputPath),
            Path.GetFullPath(outputPath)));
        services.AddSingleton<IngestMountScanner>();
        services.AddSingleton<ManifestReadinessGate>();
        services.AddSingleton<PackageWorkflowStarter>();
        services.AddSingleton<InMemoryIngestPersistenceStore>();
        services.AddSingleton<IIngestPersistenceStore>(serviceProvider =>
            serviceProvider.GetRequiredService<InMemoryIngestPersistenceStore>());
        services.AddSingleton<IOutboxMessagePublisher, LocalManifestTransferPublisher>();
        services.AddSingleton<OutboxDispatcher>();
        services.AddSingleton<IngestRuntimeService>();
    }

    private static void MapEndpoints(WebApplication app)
    {
        app.MapPost("/api/ingest/start", StartIngestAsync);
        app.MapGet("/api/ingest/status", GetStatus);
        app.MapGet("/api/workflows/{workflowInstanceId}/graph", GetWorkflowGraph);
    }

    private static async Task<IResult> StartIngestAsync(
        IngestRuntimeService runtimeService,
        CancellationToken cancellationToken)
    {
        var result = await runtimeService.StartAsync(cancellationToken);

        return result.HasConflict
            ? Results.Conflict(result.Response)
            : Results.Accepted(value: result.Response);
    }

    private static IngestStatusResponse GetStatus(IngestRuntimeService runtimeService)
    {
        return runtimeService.GetStatus();
    }

    private static IResult GetWorkflowGraph(
        string workflowInstanceId,
        IngestRuntimeService runtimeService)
    {
        var graph = runtimeService.GetWorkflowGraph(workflowInstanceId);

        return graph is null
            ? Results.NotFound()
            : Results.Ok(graph);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "MediaIngest.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
