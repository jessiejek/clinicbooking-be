using System.Globalization;
using ClinicApp.Application.Features.Doctors.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Doctors.Validators;

public sealed class UpsertSchedulesDtoValidator : AbstractValidator<UpsertSchedulesDto>
{
    private static readonly HashSet<string> AllowedDays = Enum.GetNames<DayOfWeek>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public UpsertSchedulesDtoValidator()
    {
        RuleFor(x => x.Schedules)
            .NotNull();

        RuleForEach(x => x.Schedules).ChildRules(schedule =>
        {
            schedule.RuleFor(x => x.DayOfWeek)
                .NotEmpty()
                .Must(day => !string.IsNullOrWhiteSpace(day) && AllowedDays.Contains(day.Trim()))
                .WithMessage("DayOfWeek must be one of Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, or Saturday.");

            schedule.RuleFor(x => x.StartTime)
                .NotEmpty()
                .Must(BeValidTime)
                .WithMessage("StartTime must use HH:mm format.");

            schedule.RuleFor(x => x.EndTime)
                .NotEmpty()
                .Must(BeValidTime)
                .WithMessage("EndTime must use HH:mm format.");

            schedule.RuleFor(x => x)
                .Must(x => IsStartBeforeEnd(x.StartTime, x.EndTime))
                .WithMessage("StartTime must be earlier than EndTime.");
        });

        RuleFor(x => x.Schedules)
            .Must(HaveUniqueDays)
            .WithMessage("Schedules must not contain duplicate DayOfWeek entries.");
    }

    private static bool BeValidTime(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static bool IsStartBeforeEnd(string? startTime, string? endTime)
    {
        if (!BeValidTime(startTime) || !BeValidTime(endTime))
        {
            return false;
        }

        var start = TimeOnly.ParseExact(startTime!, "HH:mm", CultureInfo.InvariantCulture);
        var end = TimeOnly.ParseExact(endTime!, "HH:mm", CultureInfo.InvariantCulture);
        return start < end;
    }

    private static bool HaveUniqueDays(List<DoctorScheduleInputDto>? schedules)
    {
        if (schedules is null)
        {
            return true;
        }

        return schedules
            .Select(x => x.DayOfWeek?.Trim() ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == schedules.Count;
    }
}
