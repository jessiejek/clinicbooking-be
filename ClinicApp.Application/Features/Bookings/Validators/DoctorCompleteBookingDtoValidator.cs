using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class DoctorCompleteBookingDtoValidator : AbstractValidator<DoctorCompleteBookingDto>
{
    public DoctorCompleteBookingDtoValidator()
    {
        RuleFor(x => x.FinalAmount)
            .NotNull()
            .When(x => !x.IsProfessionalFeeWaived)
            .WithMessage("FinalAmount is required when professional fee is not waived.");

        RuleFor(x => x.FinalAmount)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.FinalAmount.HasValue)
            .WithMessage("FinalAmount must be greater than or equal to 0.");

        RuleFor(x => x.ProfessionalFeeWaivedReason)
            .NotEmpty()
            .When(x => x.IsProfessionalFeeWaived)
            .WithMessage("ProfessionalFeeWaivedReason is required when professional fee is waived.");

        RuleFor(x => x.ProfessionalFeeWaivedReason)
            .MaximumLength(500)
            .When(x => x.ProfessionalFeeWaivedReason is not null);

        RuleFor(x => x.GeneralNotes)
            .MaximumLength(4000)
            .When(x => x.GeneralNotes is not null);

        RuleFor(x => x.SoapNotes)
            .MaximumLength(4000)
            .When(x => x.SoapNotes is not null);

        RuleFor(x => x.DoctorFeeNotes)
            .MaximumLength(2000)
            .When(x => x.DoctorFeeNotes is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes is not null);

        RuleFor(x => x.Diagnosis)
            .MaximumLength(2000)
            .When(x => x.Diagnosis is not null);

        RuleFor(x => x.FollowUpInstructions)
            .MaximumLength(4000)
            .When(x => x.FollowUpInstructions is not null);

        RuleFor(x => x.VitalSigns)
            .ChildRules(vital =>
            {
                vital.RuleFor(x => x.TakenAt)
                    .NotNull()
                    .When(x => x.TakenAt.HasValue);
            });

        RuleFor(x => x.Soap)
            .ChildRules(soap =>
            {
                soap.RuleFor(x => x.Subjective)
                    .MaximumLength(4000)
                    .When(x => x.Subjective is not null);

                soap.RuleFor(x => x.Objective)
                    .MaximumLength(4000)
                    .When(x => x.Objective is not null);

                soap.RuleFor(x => x.Assessment)
                    .MaximumLength(4000)
                    .When(x => x.Assessment is not null);

                soap.RuleFor(x => x.Plan)
                    .MaximumLength(4000)
                    .When(x => x.Plan is not null);
            });

        RuleForEach(x => x.Diagnoses)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.DiagnosisText)
                    .MaximumLength(1000)
                    .When(x => x.DiagnosisText is not null);

                item.RuleFor(x => x.DiagnosisCode)
                    .MaximumLength(100)
                    .When(x => x.DiagnosisCode is not null);

                item.RuleFor(x => x.Notes)
                    .MaximumLength(4000)
                    .When(x => x.Notes is not null);
            });

        RuleFor(x => x.Prescription)
            .ChildRules(prescription =>
            {
                prescription.RuleForEach(x => x.Items)
                    .ChildRules(item =>
                    {
                        item.RuleFor(x => x.MedicationName)
                            .MaximumLength(200)
                            .When(x => x.MedicationName is not null);

                        item.RuleFor(x => x.Strength)
                            .MaximumLength(100)
                            .When(x => x.Strength is not null);

                        item.RuleFor(x => x.Dosage)
                            .MaximumLength(100)
                            .When(x => x.Dosage is not null);

                        item.RuleFor(x => x.Route)
                            .MaximumLength(50)
                            .When(x => x.Route is not null);

                        item.RuleFor(x => x.Frequency)
                            .MaximumLength(50)
                            .When(x => x.Frequency is not null);

                        item.RuleFor(x => x.Duration)
                            .MaximumLength(50)
                            .When(x => x.Duration is not null);

                        item.RuleFor(x => x.Quantity)
                            .MaximumLength(100)
                            .When(x => x.Quantity is not null);

                        item.RuleFor(x => x.Instructions)
                            .MaximumLength(1000)
                            .When(x => x.Instructions is not null);
                    });
            });

        RuleForEach(x => x.LabOrders)
            .ChildRules(order =>
            {
                order.RuleFor(x => x.Notes)
                    .MaximumLength(4000)
                    .When(x => x.Notes is not null);

                order.RuleForEach(x => x.Items)
                    .ChildRules(item =>
                    {
                        item.RuleFor(x => x.TestName)
                            .MaximumLength(200)
                            .When(x => x.TestName is not null);

                        item.RuleFor(x => x.TestCode)
                            .MaximumLength(50)
                            .When(x => x.TestCode is not null);

                        item.RuleFor(x => x.Instructions)
                            .MaximumLength(1000)
                            .When(x => x.Instructions is not null);
                    });
            });

        RuleFor(x => x.FollowUp)
            .ChildRules(followUp =>
            {
                followUp.RuleFor(x => x.Instructions)
                    .MaximumLength(4000)
                    .When(x => x.Instructions is not null);

                followUp.RuleFor(x => x.Reason)
                    .MaximumLength(4000)
                    .When(x => x.Reason is not null);
            });

        RuleForEach(x => x.PrescriptionItems)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.MedicineName)
                    .NotEmpty()
                    .MaximumLength(200);

                item.RuleFor(x => x.GenericName)
                    .MaximumLength(200)
                    .When(x => x.GenericName is not null);

                item.RuleFor(x => x.DosageForm)
                    .MaximumLength(100)
                    .When(x => x.DosageForm is not null);

                item.RuleFor(x => x.Strength)
                    .MaximumLength(100)
                    .When(x => x.Strength is not null);

                item.RuleFor(x => x.Sig)
                    .MaximumLength(1000)
                    .When(x => x.Sig is not null);

                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0);

                item.RuleFor(x => x.Frequency)
                    .MaximumLength(100)
                    .When(x => x.Frequency is not null);

                item.RuleFor(x => x.Duration)
                    .MaximumLength(100)
                    .When(x => x.Duration is not null);

                item.RuleFor(x => x.Instructions)
                    .MaximumLength(1000)
                    .When(x => x.Instructions is not null);

                item.RuleFor(x => x.Route)
                    .MaximumLength(100)
                    .When(x => x.Route is not null);

                item.RuleFor(x => x.RouteDescription)
                    .MaximumLength(200)
                    .When(x => x.RouteDescription is not null);

                item.RuleFor(x => x.UnitOfMeasure)
                    .MaximumLength(100)
                    .When(x => x.UnitOfMeasure is not null);

                item.RuleFor(x => x.UnitOfMeasureDescription)
                    .MaximumLength(200)
                    .When(x => x.UnitOfMeasureDescription is not null);

                item.RuleFor(x => x.BrandName)
                    .MaximumLength(200)
                    .When(x => x.BrandName is not null);

                item.RuleFor(x => x.FrequencyCode)
                    .MaximumLength(50)
                    .When(x => x.FrequencyCode is not null);
            });
    }
}
