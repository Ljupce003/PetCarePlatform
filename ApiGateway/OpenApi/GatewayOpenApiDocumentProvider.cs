using System.Text.Json.Nodes;

namespace ApiGateway.OpenApi;

/// <summary>
/// Loads the owning service's OpenAPI document and changes only its server URL, so Swagger UI's
/// "Try it out" requests enter through YARP instead of bypassing the Gateway.
/// </summary>
public sealed class GatewayOpenApiDocumentProvider(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory)
{
    public async Task<JsonObject> GetAsync(
        GatewayOpenApiService service,
        CancellationToken cancellationToken)
    {
        var destinationAddress = configuration
            .GetSection($"ReverseProxy:Clusters:{service.ClusterId}:Destinations")
            .GetChildren()
            .Select(destination => destination["Address"])
            .FirstOrDefault(address => !string.IsNullOrWhiteSpace(address))
            ?? throw new InvalidOperationException(
                $"No destination address is configured for cluster '{service.ClusterId}'.");

        var documentUri = new Uri(
            new Uri(destinationAddress.EndsWith('/') ? destinationAddress : $"{destinationAddress}/"),
            "openapi/v1.json");

        using var client = httpClientFactory.CreateClient("gateway-openapi");
        using var response = await client.GetAsync(
            documentUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await JsonNode.ParseAsync(content, cancellationToken: cancellationToken) as JsonObject
            ?? throw new InvalidOperationException(
                $"{service.DisplayName} returned an invalid OpenAPI document.");

        document["servers"] = new JsonArray
        {
            new JsonObject
            {
                ["url"] = service.GatewayPrefix,
                ["description"] = "PetCare API Gateway"
            }
        };

        if (document["info"] is JsonObject info)
            info["title"] = $"{service.DisplayName} via API Gateway";

        return document;
    }
}
