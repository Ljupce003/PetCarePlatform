using System.Net;
using System.Net.Http.Json;
using MCPServer.Clients;
using MCPServer.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.IntegrationTests;

public sealed class McpServerFactory : WebApplicationFactory<Program>
{
    public CapturingTreatmentHandler TreatmentHandler { get; } = new();
    public CapturingPetHandler PetHandler { get; } = new();
    public CapturingAppointmentHandler AppointmentHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(TreatmentHandler);
            services.AddSingleton(PetHandler);
            services.AddSingleton(AppointmentHandler);
            services
                .AddHttpClient<TreatmentServiceClient>(client =>
                {
                    client.BaseAddress = new Uri("http://treatment-service.test/");
                    client.Timeout = TimeSpan.FromSeconds(5);
                })
                .ConfigurePrimaryHttpMessageHandler<CapturingTreatmentHandler>();
            services
                .AddHttpClient<PetServiceClient>(client =>
                {
                    client.BaseAddress = new Uri("http://pet-service.test/");
                    client.Timeout = TimeSpan.FromSeconds(5);
                })
                .ConfigurePrimaryHttpMessageHandler<CapturingPetHandler>();
            services
                .AddHttpClient<AppointmentServiceClient>(client =>
                {
                    client.BaseAddress = new Uri("http://appointment-service.test/");
                    client.Timeout = TimeSpan.FromSeconds(5);
                })
                .ConfigurePrimaryHttpMessageHandler<CapturingAppointmentHandler>();
        });
    }
}

public sealed class CapturingPetHandler : HttpMessageHandler
{
    private static readonly Guid PetId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public string? LastPath { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastPath = request.RequestUri?.PathAndQuery;
        var pet = new PetResponse(
            PetId, "Milo", "Dog", "Labrador", new DateOnly(2022, 5, 1), 24.5m,
            "CHIP-123", ["Pollen"], [], OwnerId);

        if (request.RequestUri?.AbsolutePath == $"/pets/{PetId:D}")
            return Json(HttpStatusCode.OK, pet);
        if (request.RequestUri?.AbsolutePath == $"/owners/{OwnerId:D}/pets")
            return Json(HttpStatusCode.OK, new[] { pet });

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static Task<HttpResponseMessage> Json<T>(HttpStatusCode status, T value) =>
        Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(value) });
}

public sealed class CapturingAppointmentHandler : HttpMessageHandler
{
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public string? LastPath { get; private set; }
    public string? LastAuthorization { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastPath = request.RequestUri?.PathAndQuery;
        LastAuthorization = request.Headers.Authorization?.ToString();

        if (request.RequestUri?.AbsolutePath == "/veterinarians")
        {
            var veterinarians = new[]
            {
                new VeterinarianResponse(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    "Dr. Ana", "Surgery", true),
                new VeterinarianResponse(
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    "Dr. Mark", "Surgery", false)
            };
            return Json(HttpStatusCode.OK, veterinarians);
        }

        if (request.RequestUri?.AbsolutePath == "/appointments/upcoming")
        {
            var appointments = new[]
            {
                new AppointmentResponse(
                    Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                    OwnerId,
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    DateTimeOffset.Parse("2026-08-20T08:00:00Z"),
                    DateTimeOffset.Parse("2026-08-20T08:30:00Z"),
                    "Annual checkup",
                    AppointmentStatusResponse.Scheduled,
                    null,
                    DateTimeOffset.Parse("2026-08-14T08:00:00Z"))
            };
            return Json(HttpStatusCode.OK, appointments);
        }

        if (request.RequestUri?.AbsolutePath == "/clinics")
        {
            var clinics = new[]
            {
                new ClinicResponse(
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    "Central Vet Clinic", "Skopje", "Bul. Ilinden 1")
            };
            return Json(HttpStatusCode.OK, clinics);
        }

        if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == "/slots")
        {
            var slots = new[]
            {
                new AvailableSlotResponse(
                    Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "Dr. Ana", "Surgery",
                    Guid.Parse("33333333-3333-3333-3333-333333333333"), "Central Vet Clinic",
                    DateTimeOffset.Parse("2026-08-18T09:00:00Z"),
                    DateTimeOffset.Parse("2026-08-18T09:30:00Z"))
            };
            return Json(HttpStatusCode.OK, slots);
        }

        if (request.RequestUri?.AbsolutePath == "/veterinarians/available")
        {
            var results = new[]
            {
                new OpenAppointmentSlotsResponse(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "Dr. Ana", "Surgery",
                    Guid.Parse("33333333-3333-3333-3333-333333333333"), "Central Vet Clinic",
                    [
                        new AvailableSlotSummaryResponse(
                            Guid.Parse("66666666-6666-6666-6666-666666666666"),
                            DateTimeOffset.Parse("2026-08-18T09:00:00Z"),
                            DateTimeOffset.Parse("2026-08-18T09:30:00Z"))
                    ])
            };
            return Json(HttpStatusCode.OK, results);
        }

        if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == "/slots")
        {
            var created = new AvailableSlotResponse(
                Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Dr. Ana", "Surgery",
                Guid.Parse("33333333-3333-3333-3333-333333333333"), "Central Vet Clinic",
                DateTimeOffset.Parse("2026-08-18T14:00:00Z"),
                DateTimeOffset.Parse("2026-08-18T14:30:00Z"));
            return Json(HttpStatusCode.Created, created);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static Task<HttpResponseMessage> Json<T>(HttpStatusCode status, T value) =>
        Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(value) });
}

public sealed class CapturingTreatmentHandler : HttpMessageHandler
{
    private static readonly Guid ExaminationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public string? LastPath { get; private set; }
    public string? LastAuthorization { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastPath = request.RequestUri?.PathAndQuery;
        LastAuthorization = request.Headers.Authorization?.ToString();

        if (request.Method == HttpMethod.Get &&
            request.RequestUri?.AbsolutePath.StartsWith("/api/treatments/pet/", StringComparison.Ordinal) == true)
        {
            IReadOnlyList<MedicalExaminationResponse> examinations =
            [
                new(
                    ExaminationId,
                    Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    null,
                    DateTimeOffset.Parse("2026-08-14T08:00:00Z"),
                    "Healthy",
                    "No treatment required",
                    [],
                    null,
                    null)
            ];

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(examinations)
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
