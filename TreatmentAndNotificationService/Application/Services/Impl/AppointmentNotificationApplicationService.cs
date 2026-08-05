using Shared.AppointmentEvents;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Infrastructure.Persistence;

namespace TreatmentAndNotificationService.Application.Services.Impl;

public class AppointmentNotificationApplicationService : IAppointmentNotificationApplicationService
{

    private readonly INotificationRepository _notificationRepository;
    private readonly TreatmentDbContext _context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AppointmentNotificationApplicationService( INotificationRepository notificationRepository, TreatmentDbContext context)
    {
        _notificationRepository = notificationRepository;
        _context = context;
    }

    public async Task HandleAsync(AppointmentScheduledEvent message, CancellationToken ct)
    {
        var source = $"appointment-scheduled:{message.EventId}";
        if (await _notificationRepository.SourceExists(source, ct)) return;
        
        var scheduled = message.StartsAtUtc.AddDays(-1);
        if (scheduled < DateTimeOffset.UtcNow) scheduled = DateTimeOffset.UtcNow;

        var notification = new Notification(message.OwnerId, message.PetId,
            NotificationType.AppointmentScheduled, "Appointment scheduled",
            $"Your veterinary appointment is scheduled for {message.StartsAtUtc:yyyy-MM-dd HH:mm} UTC.",
            scheduled, source);
        
        
        await _notificationRepository.AddNotification(notification, ct);

        await _context.SaveChangesAsync(ct);
    }

    public async Task HandleAsync(AppointmentCancelledEvent message, CancellationToken ct)
    {
        var source = $"appointment-cancelled:{message.EventId}";
        if (await _notificationRepository.SourceExists(source, ct)) return;

        var notification = new Notification(
            message.OwnerId,
            message.PetId,
            NotificationType.AppointmentCancelled, 
            "Appointment cancelled",
            $"Appointment {message.AppointmentId} was cancelled. {message.CancellationReason}",
            DateTimeOffset.UtcNow,
            source);
        
        await _notificationRepository.AddNotification(notification, ct);
        await _context.SaveChangesAsync(ct);    
    }

    public async Task HandleAsync(AppointmentRescheduledEvent message, CancellationToken ct)
    {
        var source = $"appointment-rescheduled:{message.EventId}";
        if (await _notificationRepository.SourceExists(source, ct)) return;

        var notification = new Notification(
            message.OwnerId, 
            message.PetId,
            NotificationType.AppointmentRescheduled, 
            "Appointment rescheduled",
            $"Your appointment was moved to {message.StartsAtUtc:yyyy-MM-dd HH:mm} UTC.",
            DateTimeOffset.UtcNow, 
            source);
        
        
        await _notificationRepository.AddNotification(notification, ct);
        await _context.SaveChangesAsync(ct);    
        
    }
    
}