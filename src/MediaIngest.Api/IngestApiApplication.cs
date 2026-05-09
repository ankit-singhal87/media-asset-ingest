using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;
using MediaIngest.Worker.Watcher;
using MediaIngest.Workflow;
using MediaIngest.Workflow.Orchestrator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
        IReadOnlyDictionary<string, string?>? configuration = null,
        CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        if (configuration is not null)
        {
            builder.Configuration.AddInMemoryCollection(configuration);
        }

        ConfigureServices(builder.Services, builder.Configuration, inputPath, outputPath);

        var app = builder.Build();
        MapEndpoints(app);
        await CreateSchemaOnStartupAsync(app.Services, builder.Configuration, cancellationToken);

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

        ConfigureServices(builder.Services, builder.Configuration, inputPath, outputPath);

        var app = builder.Build();
        MapEndpoints(app);
        CreateSchemaOnStartupAsync(app.Services, builder.Configuration, cancellationToken)
            .GetAwaiter()
            .GetResult();

        return app.RunAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        await webApplication.DisposeAsync();
    }

    private static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        string inputPath,
        string outputPath)
    {
        services.AddSingleton(new IngestRuntimePaths(
            Path.GetFullPath(inputPath),
            Path.GetFullPath(outputPath)));
        services.AddSingleton<IngestMountScanner>();
        services.AddSingleton<ManifestReadinessGate>();
        services.AddSingleton<PackageWorkflowStarter>();
        services.AddSingleton(WorkflowGraphProjector.CreateDefault());
        ConfigurePersistence(services, configuration);
        services.AddSingleton<IOutboxMessagePublisher, LocalManifestTransferPublisher>();
        services.AddSingleton<OutboxDispatcher>();
        services.AddSingleton<IngestRuntimeService>();
    }

    private static void ConfigurePersistence(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Persistence:Provider"];
        if (string.IsNullOrWhiteSpace(provider) || string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<InMemoryIngestPersistenceStore>();
            services.AddSingleton<IIngestPersistenceStore>(serviceProvider =>
                serviceProvider.GetRequiredService<InMemoryIngestPersistenceStore>());
            return;
        }

        if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration["Persistence:ConnectionString"]
                ?? configuration.GetConnectionString("MediaIngest");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Persistence:ConnectionString or ConnectionStrings:MediaIngest is required when Persistence:Provider is Postgres.");
            }

            services.AddSingleton<IIngestPersistenceStore>(_ =>
                PostgresIngestPersistenceStore.Create(connectionString));
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported persistence provider '{provider}'. Use InMemory or Postgres.");
    }

    private static async Task CreateSchemaOnStartupAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!bool.TryParse(configuration["Persistence:CreateSchemaOnStartup"], out var createSchemaOnStartup) ||
            !createSchemaOnStartup)
        {
            return;
        }

        var store = services.GetRequiredService<IIngestPersistenceStore>();
        if (store is not PostgresIngestPersistenceStore postgresStore)
        {
            return;
        }

        await postgresStore.CreateSchemaAsync(cancellationToken);
    }

    private static void MapEndpoints(WebApplication app)
    {
        app.MapPost("/api/ingest/start", StartIngestAsync);
        app.MapGet("/api/ingest/status", GetStatus);
        app.MapGet("/api/workflows", ListWorkflows);
        app.MapGet("/api/workflows/{workflowInstanceId}/graph", GetWorkflowGraph);
        app.MapGet("/api/workflows/{workflowInstanceId}/nodes/{nodeId}", GetWorkflowNodeDetails);
    }

    private static async Task<IResult> StartIngestAsync(
        IngestRuntimeService runtimeService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await runtimeService.StartAsync(cancellationToken);

        return result.HasConflict
            ? Problem(
                httpContext,
                StatusCodes.Status409Conflict,
                "Ingest start conflict",
                "One or more ready packages could not be started because output already exists.")
            : Results.Accepted(value: result.Response);
    }

    private static Task<IngestStatusResponse> GetStatus(
        IngestRuntimeService runtimeService,
        CancellationToken cancellationToken)
    {
        return runtimeService.GetStatusAsync(cancellationToken);
    }

    private static Task<WorkflowListResponse> ListWorkflows(
        IngestRuntimeService runtimeService,
        CancellationToken cancellationToken)
    {
        return runtimeService.ListWorkflowsAsync(cancellationToken);
    }

    private static async Task<IResult> GetWorkflowGraph(
        string workflowInstanceId,
        IngestRuntimeService runtimeService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var graph = await runtimeService.GetWorkflowGraphAsync(workflowInstanceId, cancellationToken);

        return graph is null
            ? Problem(
                httpContext,
                StatusCodes.Status404NotFound,
                "Workflow graph not found",
                $"Workflow graph '{workflowInstanceId}' was not found.")
            : Results.Ok(graph);
    }

    private static async Task<IResult> GetWorkflowNodeDetails(
        string workflowInstanceId,
        string nodeId,
        IngestRuntimeService runtimeService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var details = await runtimeService.GetWorkflowNodeDetailsAsync(
            workflowInstanceId,
            nodeId,
            cancellationToken);

        return details is null
            ? Problem(
                httpContext,
                StatusCodes.Status404NotFound,
                "Workflow node details not found",
                $"Workflow node '{nodeId}' was not found in workflow '{workflowInstanceId}'.")
            : Results.Ok(details);
    }

    private static IResult Problem(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail)
    {
        return Results.Problem(
            title: title,
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = httpContext.TraceIdentifier
            });
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "MediaIngest.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
