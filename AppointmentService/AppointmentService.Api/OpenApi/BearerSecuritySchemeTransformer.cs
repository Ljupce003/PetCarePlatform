using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppointmentService.Api.OpenApi;

/// <summary>
/// Microsoft.AspNetCore.OpenApi (used instead of Swashbuckle's SwaggerGen — see Program.cs)
/// doesn't automatically declare a security scheme, so Swagger UI wouldn't show an "Authorize"
/// button without this. Adds the "Bearer" HTTP scheme at the document level; see
/// <see cref="AuthorizeOperationTransformer"/> for applying it per-endpoint.
/// </summary>
public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(scheme => scheme.Name == "Bearer"))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the accessToken from POST /auth/login or /auth/token (no \"Bearer \" prefix needed)."
            }
        };
    }
}

/// <summary>
/// Adds the Bearer security requirement to every operation except ones marked
/// <see cref="AllowAnonymousAttribute"/> (<c>/auth/login</c>, <c>/auth/token</c>), so Swagger UI
/// only shows a lock icon on endpoints that actually need a token.
/// </summary>
public sealed class AuthorizeOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var isAnonymous = context.Description.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
        if (isAnonymous || context.Document is null)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });

        return Task.CompletedTask;
    }
}
