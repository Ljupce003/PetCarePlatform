using System.ComponentModel;
using MCPServer.Clients;
using MCPServer.Contracts;
using ModelContextProtocol.Server;

namespace MCPServer.Tools;

[McpServerToolType]
public sealed class TreatmentTools
{

    private readonly TreatmentServiceClient _treatmentClient;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TreatmentTools(TreatmentServiceClient treatmentClient)
    {
        _treatmentClient = treatmentClient;
    }

    [McpServerTool(Name = "get_medical_history")]
    [Description("Gets the complete medical examination history for a pet, newest examination first.")]
    public Task<IReadOnlyList<MedicalExaminationResponse>> GetMedicalHistory(
        [Description("The unique ID of the pet.")] Guid petId,
        CancellationToken cancellationToken)
    {
        return _treatmentClient.GetMedicalHistoryAsync(petId, cancellationToken);
    }

    [McpServerTool(Name = "get_vaccination_history")]
    [Description("Gets every vaccination recorded for a pet, newest administration first.")]
    public Task<IReadOnlyList<VaccinationResponse>> GetVaccinationHistory(
        [Description("The unique ID of the pet.")] Guid petId,
        CancellationToken cancellationToken)
    {
        return _treatmentClient.GetVaccinationHistoryAsync(petId, cancellationToken);
    }

    [McpServerTool(Name = "get_next_vaccination")]
    [Description("Gets the nearest upcoming vaccination for a pet, or null when none is scheduled.")]
    public Task<VaccinationResponse?> GetNextVaccination(
        [Description("The unique ID of the pet.")] Guid petId,
        CancellationToken cancellationToken)
    {
        return _treatmentClient.GetNextVaccinationAsync(petId, cancellationToken);
    }

    [McpServerTool(Name = "record_medical_examination")]
    [Description("Records a completed medical examination. Requires a veterinarian or administrator token.")]
    public Task<MedicalExaminationResponse> RecordMedicalExamination(
        [Description("The examined pet ID.")] Guid petId,
        [Description("The pet owner ID.")] Guid ownerId,
        [Description("The veterinarian ID.")] Guid veterinarianId,
        [Description("The related appointment ID, when available.")] Guid? appointmentId,
        [Description("When the examination occurred.")] DateTimeOffset examinedAtUtc,
        [Description("The clinical diagnosis.")] string diagnosis,
        [Description("The prescribed treatment plan.")] string treatmentPlan,
        [Description("The prescribed medications, if any.")] IReadOnlyList<string>? medications = null,
        [Description("The next control time, if follow-up is required.")] DateTimeOffset? nextControlAtUtc = null,
        [Description("Additional clinical notes.")] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var request = new RecordMedicalExaminationRequest(
            petId, ownerId, veterinarianId, appointmentId, examinedAtUtc, diagnosis,
            treatmentPlan, medications, nextControlAtUtc, notes);

        return _treatmentClient.RecordMedicalExaminationAsync(request, cancellationToken);
    }

    [McpServerTool(Name = "record_vaccination")]
    [Description("Records an administered vaccine. Requires a veterinarian or administrator token.")]
    public Task<VaccinationResponse> RecordVaccination(
        [Description("The vaccinated pet ID.")] Guid petId,
        [Description("The pet owner ID.")] Guid ownerId,
        [Description("The veterinarian ID.")] Guid veterinarianId,
        [Description("The administered vaccine name.")] string vaccineName,
        [Description("The vaccine administration date.")] DateOnly administeredOn,
        [Description("The next due date, when another dose is required.")] DateOnly? nextDueOn = null,
        [Description("The vaccine batch number, when available.")] string? batchNumber = null,
        CancellationToken cancellationToken = default)
    {
        var request = new RecordVaccinationRequest(
            petId, ownerId, veterinarianId, vaccineName, administeredOn, nextDueOn, batchNumber);

        return _treatmentClient.RecordVaccinationAsync(request, cancellationToken);
    }
}
