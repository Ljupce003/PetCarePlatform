using System.Net;
using MCPServer.Contracts;

namespace MCPServer.Clients;

public class TreatmentServiceClient
{
    private readonly HttpClient _httpClient;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TreatmentServiceClient(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }
    
    public async Task<IReadOnlyList<MedicalExaminationResponse>>
        GetMedicalHistoryAsync(
            Guid petId,
            CancellationToken cancellationToken)
    {
        var result = await _httpClient.GetFromJsonAsync<
            List<MedicalExaminationResponse>>(
            $"api/treatments/pet/{petId:D}",
            cancellationToken);

        return result ?? [];
    }

    public async Task<IReadOnlyList<VaccinationResponse>>
        GetVaccinationHistoryAsync(
            Guid petId,
            CancellationToken cancellationToken)
    {
        var result = await _httpClient.GetFromJsonAsync<
            List<VaccinationResponse>>(
            $"api/vaccinations/pet/{petId:D}",
            cancellationToken);

        return result ?? [];
    }

    public async Task<VaccinationResponse?> GetNextVaccinationAsync(
        Guid petId,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"api/vaccinations/pet/{petId:D}/next",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<VaccinationResponse>(
                cancellationToken: cancellationToken);
    }

    public async Task<MedicalExaminationResponse>
        RecordMedicalExaminationAsync(
            RecordMedicalExaminationRequest request,
            CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/treatments",
            request,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content
                   .ReadFromJsonAsync<MedicalExaminationResponse>(
                       cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException(
                   "Treatment Service returned an empty response.");
    }

    public async Task<VaccinationResponse> RecordVaccinationAsync(
        RecordVaccinationRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/vaccinations",
            request,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content
                   .ReadFromJsonAsync<VaccinationResponse>(
                       cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException(
                   "Treatment Service returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        throw new HttpRequestException(
            $"Treatment Service returned HTTP " +
            $"{(int)response.StatusCode} ({response.StatusCode}). " +
            $"{responseBody}",
            inner: null,
            response.StatusCode);
    }
}
