using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;

namespace TreatmentAndNotificationService.Application.Models;

public record NotificationDto(
    Guid Id,
    Guid OwnerId,
    Guid PetId,
    NotificationType Type,
    string Title,
    string Message,
    DateTimeOffset ScheduledForUtc,
    NotificationStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SentAtUtc);