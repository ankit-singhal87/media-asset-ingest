using System.Text;

namespace MediaIngest.Worker.Outbox;

public sealed class DaprOutboxMessagePublisher(HttpClient httpClient, string pubSubName) : IOutboxMessagePublisher
{
    private const string RawPayloadMetadataName = "rawPayload";

    public async Task PublishAsync(OutboxPublishRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            BuildPublishPath(pubSubName, request))
        {
            Content = new StringContent(request.Message.PayloadJson, Encoding.UTF8, "application/json")
        };

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new InvalidOperationException(
            $"Dapr publish failed for outbox message '{request.Message.MessageId}' to topic " +
            $"'{request.Message.Destination}' with status {(int)response.StatusCode}: {responseBody}");
    }

    private static string BuildPublishPath(string pubSubName, OutboxPublishRequest request)
    {
        var path = $"v1.0/publish/{Uri.EscapeDataString(pubSubName)}/{Uri.EscapeDataString(request.Message.Destination)}";

        var metadata = request.ApplicationProperties
            .Append(new KeyValuePair<string, string>(RawPayloadMetadataName, "true"))
            .OrderBy(property => property.Key, StringComparer.Ordinal)
            .Select(property =>
                $"metadata.{Uri.EscapeDataString(property.Key)}={Uri.EscapeDataString(property.Value)}");

        return $"{path}?{string.Join("&", metadata)}";
    }
}
