using TreatmentAndNotificationService.Service;

namespace TreatmentAndNotificationService.Web.Controllers;

public class TreatmentController
{
    private readonly ITreatmentService _treatmentService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TreatmentController(ITreatmentService treatmentService)
    {
        _treatmentService = treatmentService;
    }
}