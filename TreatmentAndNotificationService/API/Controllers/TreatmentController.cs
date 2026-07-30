using Microsoft.AspNetCore.Mvc;
using TreatmentAndNotificationService.Application;
using TreatmentAndNotificationService.Application.Services;

namespace TreatmentAndNotificationService.API.Controllers;

[ApiController]
public class TreatmentController : ControllerBase
{
    private readonly ITreatmentApplicationService _treatmentApplicationService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TreatmentController(ITreatmentApplicationService treatmentApplicationService)
    {
        _treatmentApplicationService = treatmentApplicationService;
    }
}