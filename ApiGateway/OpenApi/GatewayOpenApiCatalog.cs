namespace ApiGateway.OpenApi;

public sealed record GatewayOpenApiService(
    string Id,
    string DisplayName,
    string ClusterId,
    string GatewayPrefix);

public static class GatewayOpenApiCatalog
{
    public static readonly IReadOnlyDictionary<string, GatewayOpenApiService> Services =
        new Dictionary<string, GatewayOpenApiService>(StringComparer.OrdinalIgnoreCase)
        {
            ["pet"] = new("pet", "Pet Service", "pet-cluster", "/pet"),
            ["appointment"] = new("appointment", "Appointment Service", "appointment-cluster", "/appointment"),
            ["treatment"] = new("treatment", "Treatment & Notification Service", "treatment-cluster", "/treatment")
        };
}
