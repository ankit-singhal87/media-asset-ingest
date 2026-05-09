using MediaIngest.Contracts.Commands;

var executionClassValue = ReadSetting("Worker__ExecutionClass") ?? "light";
var executionClass = ExecutionClassProperties.FromPropertyValue(executionClassValue);
var interval = TimeSpan.FromMilliseconds(ReadPositiveInt("Worker__IntervalMilliseconds", 1000));

Console.WriteLine(
    $"Media ingest command runner started. executionClass={executionClass.ToPropertyValue()} interval={interval}");
Console.WriteLine(
    "Local review mode: command bus consumption is represented by library tests; " +
    "this host keeps the light/medium/heavy worker processes runnable in Compose.");

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) => cancellation.Cancel();

try
{
    while (!cancellation.IsCancellationRequested)
    {
        await Task.Delay(interval, cancellation.Token);
    }
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
}

static string? ReadSetting(string name)
{
    var value = Environment.GetEnvironmentVariable(name);

    return string.IsNullOrWhiteSpace(value) ? null : value;
}

static int ReadPositiveInt(string name, int defaultValue)
{
    var value = ReadSetting(name);
    if (value is null)
    {
        return defaultValue;
    }

    if (int.TryParse(value, out var parsed) && parsed > 0)
    {
        return parsed;
    }

    throw new InvalidOperationException($"{name} must be a positive integer.");
}
