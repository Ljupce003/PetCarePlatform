using System.Security.Claims;
using ApiGateway.Security;
using Microsoft.Extensions.Configuration;

namespace ApiGateway.IntegrationTests;

public sealed class KeycloakRoleClaimsTransformationTests
{
    private readonly KeycloakRoleClaimsTransformation _transformation = new(
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ClientId"] = "api-gateway"
            })
            .Build());

    [Fact]
    public async Task RealmRoles_AreConvertedToAspNetRoleClaims()
    {
        var principal = AuthenticatedPrincipal(
            new Claim("realm_access", """{"roles":["owner","veterinarian"]}"""));

        await _transformation.TransformAsync(principal);

        Assert.True(principal.IsInRole("owner"));
        Assert.True(principal.IsInRole("veterinarian"));
    }

    [Fact]
    public async Task CurrentClientResourceRoles_AreConvertedToAspNetRoleClaims()
    {
        var principal = AuthenticatedPrincipal(
            new Claim(
                "resource_access",
                """{"api-gateway":{"roles":["service"]},"different-client":{"roles":["admin"]}}"""));

        await _transformation.TransformAsync(principal);

        Assert.True(principal.IsInRole("service"));
        Assert.False(principal.IsInRole("admin"));
    }

    [Fact]
    public async Task RepeatedTransformation_DoesNotDuplicateRoleClaims()
    {
        var principal = AuthenticatedPrincipal(
            new Claim(ClaimTypes.Role, "owner"),
            new Claim("realm_access", """{"roles":["owner"]}"""));

        await _transformation.TransformAsync(principal);
        await _transformation.TransformAsync(principal);

        Assert.Single(principal.Claims, claim =>
            claim.Type == ClaimTypes.Role && claim.Value == "owner");
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("{\"roles\":\"not-an-array\"}")]
    public async Task InvalidOrMissingRoleJson_IsIgnored(string realmAccess)
    {
        var principal = AuthenticatedPrincipal(new Claim("realm_access", realmAccess));

        var transformed = await _transformation.TransformAsync(principal);

        Assert.Same(principal, transformed);
        Assert.DoesNotContain(principal.Claims, claim => claim.Type == ClaimTypes.Role);
    }

    [Fact]
    public async Task UnauthenticatedIdentity_IsNotModified()
    {
        var identity = new ClaimsIdentity(
            [new Claim("realm_access", """{"roles":["admin"]}""")]);
        var principal = new ClaimsPrincipal(identity);

        await _transformation.TransformAsync(principal);

        Assert.False(principal.IsInRole("admin"));
    }

    private static ClaimsPrincipal AuthenticatedPrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Test", ClaimTypes.Name, ClaimTypes.Role));
}
