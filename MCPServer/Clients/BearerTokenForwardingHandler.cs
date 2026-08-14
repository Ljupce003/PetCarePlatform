namespace MCPServer.Clients;

public sealed class BearerTokenForwardingHandler(
    IHttpContextAccessor contextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authorization = contextAccessor.HttpContext?
            .Request.Headers.Authorization
            .ToString();

        if (!string.IsNullOrWhiteSpace(authorization) && request.Headers.Authorization is null)
        {
            request.Headers.TryAddWithoutValidation(
                "Authorization",
                authorization);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
