using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Exceptions;

namespace AppointmentService.Application.Queries;

public sealed record GetUpcomingAppointmentsQuery(Guid OwnerId);

public static class GetUpcomingAppointmentsValidator
{
    public static void Validate(GetUpcomingAppointmentsQuery query)
    {
        if (query.OwnerId == Guid.Empty)
        {
            throw new ValidationException(["OwnerId is required."]);
        }
    }
}

public sealed class GetUpcomingAppointmentsHandler(IAppointmentRepository appointments)
{
    public async Task<IReadOnlyList<AppointmentDto>> HandleAsync(GetUpcomingAppointmentsQuery query, CancellationToken cancellationToken)
    {
        GetUpcomingAppointmentsValidator.Validate(query);

        return (await appointments.GetUpcomingByOwnerAsync(query.OwnerId, cancellationToken))
            .Select(appointment => appointment.ToDto())
            .ToList();
    }
}
