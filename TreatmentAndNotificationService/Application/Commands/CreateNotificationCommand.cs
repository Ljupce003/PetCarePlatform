using TreatmentAndNotificationService.Domain.Enums;

namespace TreatmentAndNotificationService.Application.Commands;

public sealed record CreateNotificationCommand(
    Guid OwnerId, Guid PetId, NotificationType Type, string? Title, string? Message,
    DateTimeOffset ScheduledForUtc, string? SourceEventId);
