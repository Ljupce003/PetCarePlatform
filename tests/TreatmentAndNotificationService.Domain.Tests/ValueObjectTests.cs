using TreatmentAndNotificationService.Domain.Common;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Domain.Tests;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Diagnosis_WhenMissing_Throws(string? value) =>
        Assert.Throws<DomainValidationException>(() => Diagnosis.Create(value));

    [Fact]
    public void Diagnosis_TrimsValue_AndRejectsOversizedValue()
    {
        Assert.Equal("allergy", Diagnosis.Create("  allergy  ").Value);
        Assert.Throws<DomainValidationException>(() => Diagnosis.Create(new string('d', Diagnosis.MaximumLength + 1)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TreatmentPlan_WhenMissing_Throws(string? value) =>
        Assert.Throws<DomainValidationException>(() => TreatmentPlan.Create(value));

    [Fact]
    public void TreatmentPlan_TrimsValue_AndRejectsOversizedValue()
    {
        Assert.Equal("rest", TreatmentPlan.Create("  rest  ").Value);
        Assert.Throws<DomainValidationException>(() => TreatmentPlan.Create(new string('p', TreatmentPlan.MaximumLength + 1)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VaccineName_WhenMissing_Throws(string? value) =>
        Assert.Throws<DomainValidationException>(() => VaccineName.Create(value));

    [Fact]
    public void VaccineName_TrimsValue_AndRejectsOversizedValue()
    {
        Assert.Equal("Rabies", VaccineName.Create("  Rabies  ").Value);
        Assert.Throws<DomainValidationException>(() => VaccineName.Create(new string('v', VaccineName.MaximumLength + 1)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SourceEventId_WhenMissing_Throws(string? value) =>
        Assert.Throws<DomainValidationException>(() => SourceEventId.Create(value));

    [Fact]
    public void SourceEventId_TrimsValue_AndUsesValueEquality()
    {
        var first = SourceEventId.Create(" appointment:123 ");
        var second = SourceEventId.Create("appointment:123");

        Assert.Equal("appointment:123", first.Value);
        Assert.Equal(first, second);
        Assert.Throws<DomainValidationException>(() =>
            SourceEventId.Create(new string('e', SourceEventId.MaximumLength + 1)));
    }

    [Fact]
    public void NotificationContent_TrimsBothValues()
    {
        var content = NotificationContent.Create(" Reminder ", " Visit tomorrow ");

        Assert.Equal("Reminder", content.Title);
        Assert.Equal("Visit tomorrow", content.Message);
    }

    [Theory]
    [InlineData(null, "message")]
    [InlineData("title", null)]
    [InlineData(" ", "message")]
    [InlineData("title", " ")]
    public void NotificationContent_WhenEitherValueIsMissing_Throws(string? title, string? message) =>
        Assert.Throws<DomainValidationException>(() => NotificationContent.Create(title, message));

    [Fact]
    public void NotificationContent_WhenEitherValueIsTooLong_Throws()
    {
        Assert.Throws<DomainValidationException>(() =>
            NotificationContent.Create(new string('t', NotificationContent.MaximumTitleLength + 1), "message"));
        Assert.Throws<DomainValidationException>(() =>
            NotificationContent.Create("title", new string('m', NotificationContent.MaximumMessageLength + 1)));
    }

    [Fact]
    public void VaccinationSchedule_WithValidDates_PreservesDates()
    {
        var administered = new DateOnly(2026, 8, 10);
        var nextDue = administered.AddYears(1);

        var schedule = VaccinationSchedule.Create(administered, nextDue);

        Assert.Equal(administered, schedule.AdministeredOn);
        Assert.Equal(nextDue, schedule.NextDueOn);
    }

    [Fact]
    public void VaccinationSchedule_AllowsNoNextDose()
    {
        var schedule = VaccinationSchedule.Create(new DateOnly(2026, 8, 10), null);

        Assert.Null(schedule.NextDueOn);
    }

    [Fact]
    public void VaccinationSchedule_RejectsMissingOrNonFutureNextDate()
    {
        var administered = new DateOnly(2026, 8, 10);

        Assert.Throws<DomainValidationException>(() => VaccinationSchedule.Create(default, null));
        Assert.Throws<DomainValidationException>(() => VaccinationSchedule.Create(administered, administered));
        Assert.Throws<DomainValidationException>(() => VaccinationSchedule.Create(administered, administered.AddDays(-1)));
    }
}
