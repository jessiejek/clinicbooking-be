using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Seeding;

public sealed class BookingSeeder : IBookingSeeder
{
    private const string AdminEmail = "admin@gavino.clinic";
    private const string PatientEmail = "patient@gavino.clinic";
    private const string DoctorEmail = "dr.santos@gavino.clinic";
    private const string GeneralConsultationServiceName = "General Consultation";

    private static readonly TimeSpan PhilippinesOffset = TimeSpan.FromHours(8);

    private static readonly Guid PendingBookingId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    private static readonly Guid ProofSubmittedBookingId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    private static readonly Guid CompletedBookingId = Guid.Parse("11111111-1111-1111-1111-111111111103");

    private static readonly Guid PendingPaymentId = Guid.Parse("22222222-2222-2222-2222-222222222201");
    private static readonly Guid ProofSubmittedPaymentId = Guid.Parse("22222222-2222-2222-2222-222222222202");
    private static readonly Guid CompletedPaymentId = Guid.Parse("22222222-2222-2222-2222-222222222203");

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public BookingSeeder(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var patientUser = await LoadUserAsync(PatientEmail, cancellationToken);
        var doctorUser = await LoadUserAsync(DoctorEmail, cancellationToken);
        var adminUser = await LoadUserAsync(AdminEmail, cancellationToken);

        var patient = await _dbContext.Patients
            .SingleOrDefaultAsync(x => x.UserId == patientUser.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Unable to seed bookings because patient profile for {PatientEmail} was not found.");

        var doctor = await _dbContext.Doctors
            .SingleOrDefaultAsync(x => x.UserId == doctorUser.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Unable to seed bookings because doctor profile for {DoctorEmail} was not found.");

        var service = await _dbContext.Services
            .SingleOrDefaultAsync(x => x.Name == GeneralConsultationServiceName, cancellationToken)
            ?? throw new InvalidOperationException($"Unable to seed bookings because service '{GeneralConsultationServiceName}' was not found.");

        var linked = await _dbContext.DoctorServices
            .AnyAsync(x => x.DoctorId == doctor.Id && x.ServiceId == service.Id, cancellationToken);
        if (!linked)
        {
            throw new InvalidOperationException($"Unable to seed bookings because {GeneralConsultationServiceName} is not linked to {DoctorEmail}.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Add(PhilippinesOffset));
        var now = DateTime.UtcNow;
        var consultationFee = doctor.ConsultationFee;
        var serviceFee = service.Price;
        var totalFee = consultationFee + serviceFee;
        var completedAppointmentDate = today.AddDays(-1);
        var completedRecordedAt = now.AddDays(-1);

        await SeedBookingAsync(
            BuildPendingBooking(patient.Id, doctor.Id, service.Id, today, now, consultationFee, serviceFee, totalFee),
            BuildPendingPayment(PendingBookingId, now, totalFee),
            cancellationToken);

        await SeedBookingAsync(
            BuildProofSubmittedBooking(patient.Id, doctor.Id, service.Id, today, now, consultationFee, serviceFee, totalFee),
            BuildProofSubmittedPayment(ProofSubmittedBookingId, now, totalFee),
            cancellationToken);

        await SeedBookingAsync(
            BuildCompletedBooking(patient.Id, doctor.Id, service.Id, completedAppointmentDate, completedRecordedAt, consultationFee, serviceFee, totalFee),
            BuildCompletedPayment(CompletedBookingId, completedRecordedAt, adminUser.Id, completedAppointmentDate, totalFee),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser> LoadUserAsync(string email, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new InvalidOperationException($"Unable to seed bookings because {email} was not found.");
        }

        return user;
    }

    private static Booking BuildPendingBooking(Guid patientId, Guid doctorId, Guid serviceId, DateOnly today, DateTime now, decimal consultationFee, decimal serviceFee, decimal totalFee)
    {
        return new Booking
        {
            Id = PendingBookingId,
            PatientId = patientId,
            DoctorId = doctorId,
            ServiceId = serviceId,
            AppointmentDate = today.AddDays(1),
            SlotStartTime = new TimeOnly(9, 0),
            SlotEndTime = new TimeOnly(9, 30),
            Status = "Pending",
            PaymentStatus = "Unpaid",
            PaymentMode = "Online",
            QueueNumber = 1,
            TotalFee = totalFee,
            ConsultationFeeSnapshot = consultationFee,
            ServiceFeeSnapshot = serviceFee,
            IsWalkIn = false,
            Notes = "Seeded sample booking for the bookings list.",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Booking BuildProofSubmittedBooking(Guid patientId, Guid doctorId, Guid serviceId, DateOnly today, DateTime now, decimal consultationFee, decimal serviceFee, decimal totalFee)
    {
        return new Booking
        {
            Id = ProofSubmittedBookingId,
            PatientId = patientId,
            DoctorId = doctorId,
            ServiceId = serviceId,
            AppointmentDate = today.AddDays(1),
            SlotStartTime = new TimeOnly(9, 30),
            SlotEndTime = new TimeOnly(10, 0),
            Status = "ProofSubmitted",
            PaymentStatus = "Unpaid",
            PaymentMode = "Online",
            QueueNumber = 2,
            TotalFee = totalFee,
            ConsultationFeeSnapshot = consultationFee,
            ServiceFeeSnapshot = serviceFee,
            IsWalkIn = false,
            ProofType = "ReferenceNumber",
            ProofValue = "GCASH-SEED-0001",
            ProofSubmittedAt = now.AddMinutes(-15),
            Notes = "Seeded sample booking pending payment verification.",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Booking BuildCompletedBooking(Guid patientId, Guid doctorId, Guid serviceId, DateOnly appointmentDate, DateTime recordedAt, decimal consultationFee, decimal serviceFee, decimal totalFee)
    {
        var orNumber = $"OR-{appointmentDate:yyyyMMdd}-00001";

        return new Booking
        {
            Id = CompletedBookingId,
            PatientId = patientId,
            DoctorId = doctorId,
            ServiceId = serviceId,
            AppointmentDate = appointmentDate,
            SlotStartTime = new TimeOnly(11, 0),
            SlotEndTime = new TimeOnly(11, 30),
            Status = "Completed",
            PaymentStatus = "Paid",
            PaymentMode = "PayAtClinic",
            QueueNumber = 1,
            TotalFee = totalFee,
            ConsultationFeeSnapshot = consultationFee,
            ServiceFeeSnapshot = serviceFee,
            IsWalkIn = false,
            ReceiptUrl = "https://example.com/receipts/seed-completed-booking.pdf",
            OrNumber = orNumber,
            CreatedAt = recordedAt,
            UpdatedAt = recordedAt
        };
    }

    private static Payment BuildPendingPayment(Guid bookingId, DateTime now, decimal totalFee)
    {
        return new Payment
        {
            Id = PendingPaymentId,
            BookingId = bookingId,
            Amount = totalFee,
            PaymentMethod = "GCash",
            Status = "Unpaid",
            CreatedAt = now
        };
    }

    private static Payment BuildProofSubmittedPayment(Guid bookingId, DateTime now, decimal totalFee)
    {
        return new Payment
        {
            Id = ProofSubmittedPaymentId,
            BookingId = bookingId,
            Amount = totalFee,
            PaymentMethod = "GCash",
            ReferenceNumber = "GCASH-SEED-0001",
            Status = "Unpaid",
            CreatedAt = now
        };
    }

    private static Payment BuildCompletedPayment(Guid bookingId, DateTime recordedAt, string verifiedByUserId, DateOnly appointmentDate, decimal totalFee)
    {
        var orNumber = $"OR-{appointmentDate:yyyyMMdd}-00001";

        return new Payment
        {
            Id = CompletedPaymentId,
            BookingId = bookingId,
            Amount = totalFee,
            PaymentMethod = "PayAtClinic",
            Status = "Paid",
            OrNumber = orNumber,
            VerifiedByUserId = verifiedByUserId,
            VerifiedAt = recordedAt.AddHours(1),
            CreatedAt = recordedAt
        };
    }

    private async Task SeedBookingAsync(Booking booking, Payment payment, CancellationToken cancellationToken)
    {
        var bookingExists = await _dbContext.Bookings.AnyAsync(x => x.Id == booking.Id, cancellationToken);
        var paymentExists = await _dbContext.Payments.AnyAsync(x => x.Id == payment.Id || x.BookingId == booking.Id, cancellationToken);

        if (!bookingExists)
        {
            booking.Payment = payment;
            payment.Booking = booking;
            _dbContext.Bookings.Add(booking);
        }
        else if (!paymentExists)
        {
            payment.BookingId = booking.Id;
            _dbContext.Payments.Add(payment);
        }
    }
}
