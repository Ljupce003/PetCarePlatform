using System.Text.Json;

namespace AppointmentService.Api.IntegrationTests;

/// <summary>
/// System.Net.Http.Json's ReadFromJsonAsync/GetFromJsonAsync use case-SENSITIVE property matching
/// by default. The API serializes responses as camelCase (ASP.NET Core MVC's default), while every
/// DTO here is PascalCase C#, so deserializing without this would silently leave every property at
/// its default value instead of throwing -- pass this explicitly to every ReadFromJsonAsync/
/// GetFromJsonAsync call in this test project.
/// </summary>
internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions CaseInsensitive = new(JsonSerializerDefaults.Web);
}
