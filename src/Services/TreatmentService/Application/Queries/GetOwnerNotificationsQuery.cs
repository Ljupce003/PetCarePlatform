namespace TreatmentAndNotificationService.Application.Queries;

public sealed record GetOwnerNotificationsQuery(Guid OwnerId);
public sealed record GetVeterinarianNotificationsQuery(Guid VeterinarianId);
