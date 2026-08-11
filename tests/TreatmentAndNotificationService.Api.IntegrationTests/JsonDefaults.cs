using System.Text.Json;
using System.Text.Json.Serialization;

namespace TreatmentAndNotificationService.Api.IntegrationTests;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions CaseInsensitive = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
