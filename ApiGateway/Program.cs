using System.Security.Claims;
using System.Text;
using ApiGateway.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetRequiredSection("Jwt");
var issuer = jwtSection["Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is required.");
var audience = jwtSection["Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is required.");
var signingKey = jwtSection["SigningKey"];
var useLegacyDevelopmentTokens = !builder.Environment.IsEnvironment("Docker") &&
                                 !string.IsNullOrWhiteSpace(signingKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        if (!useLegacyDevelopmentTokens)
        {
            options.Authority = (jwtSection["Authority"]
                ?? throw new InvalidOperationException("Jwt:Authority is required in Docker."))
                .TrimEnd('/');
            options.Audience = audience;
            options.RequireHttpsMetadata = jwtSection.GetValue("RequireHttpsMetadata", true);
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = useLegacyDevelopmentTokens
                ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!))
                : null,
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();
builder.Services.AddAuthorization();

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();


var app = builder.Build();

app.MapHealthChecks("/health");

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();

public partial class Program;
