namespace MCPServer.Clients;

internal static class DownstreamResponse
{
    public static async Task EnsureSuccessAsync(
        string serviceName,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"{serviceName} returned HTTP {(int)response.StatusCode} ({response.StatusCode}). {body}",
            inner: null,
            response.StatusCode);
    }
}
