-- ═══════════════════════════════════════════════════════════
-- SQL Server Views — matches Supabase views that frontend queries
-- Run this against the ClinicAppDb database
-- ═══════════════════════════════════════════════════════════

-- 1. patient_bookings_view — bookings with patient/doctor/service info
CREATE OR ALTER VIEW dbo.patient_bookings_view AS
SELECT
    b.Id,
    b.PatientId,
    p.FirstName + ' ' + p.LastName AS PatientName,
    b.DoctorId,
    d.FullName AS DoctorName,
    FORMAT(b.AppointmentDate, 'yyyy-MM-dd') AS AppointmentDate,
    FORMAT(b.SlotStartTime, N'H\:mm') AS SlotStartTime,
    FORMAT(b.SlotEndTime, N'H\:mm') AS SlotEndTime,
    b.Status,
    b.PaymentStatus,
    b.PaymentMode,
    b.QueueNumber,
    b.TotalFee,
    b.FinalAmount,
    b.AmountDue,
    b.ConsultationFeeSnapshot,
    b.ServiceFeeSnapshot,
    b.IsWalkIn,
    b.ProofType,
    b.ProofValue,
    FORMAT(b.ProofSubmittedAt, 'yyyy-MM-ddTHH:mm:ss') AS ProofSubmittedAt,
    b.CancellationReason,
    b.Notes,
    b.RescheduledFromBookingId,
    b.ReceiptUrl,
    b.OrNumber,
    FORMAT(b.CheckedInAt, 'yyyy-MM-ddTHH:mm:ss') AS CheckedInAt,
    FORMAT(b.DoctorCompletedAt, 'yyyy-MM-ddTHH:mm:ss') AS DoctorCompletedAt,
    b.IsProfessionalFeeWaived,
    b.ProfessionalFeeWaivedReason,
    FORMAT(b.CreatedAt, 'yyyy-MM-ddTHH:mm:ss') AS CreatedAt,
    d.UserId AS DoctorUserId,
    d.Specialization,
    d.ConsultationFee,
    d.ProfilePhotoUrl,
    d.Status AS DoctorStatus
FROM dbo.Bookings b
INNER JOIN dbo.Patients p ON b.PatientId = p.Id
INNER JOIN dbo.Doctors d ON b.DoctorId = d.Id;
GO

-- 2. doctor_today_queue_view — today's queue for a doctor
CREATE OR ALTER VIEW dbo.doctor_today_queue_view AS
SELECT
    b.Id,
    b.PatientId,
    p.FirstName + ' ' + p.LastName AS PatientName,
    b.DoctorId,
    d.FullName AS DoctorName,
    FORMAT(b.AppointmentDate, 'yyyy-MM-dd') AS AppointmentDate,
    FORMAT(b.SlotStartTime, N'H\:mm') AS SlotStartTime,
    FORMAT(b.SlotEndTime, N'H\:mm') AS SlotEndTime,
    b.Status,
    b.PaymentStatus,
    b.QueueNumber,
    b.IsWalkIn,
    b.IsProfessionalFeeWaived,
    b.FinalAmount,
    b.AmountDue,
    b.Notes,
    FORMAT(b.CheckedInAt, 'yyyy-MM-ddTHH:mm:ss') AS CheckedInAt
FROM dbo.Bookings b
INNER JOIN dbo.Patients p ON b.PatientId = p.Id
INNER JOIN dbo.Doctors d ON b.DoctorId = d.Id
WHERE CAST(b.AppointmentDate AS DATE) = CAST(GETDATE() AS DATE);
GO

-- 3. patient_documents_view
CREATE OR ALTER VIEW dbo.patient_documents_view AS
SELECT
    pd.Id,
    pd.PatientId,
    pd.BookingId,
    pd.ConsultationId,
    pd.DocumentType,
    pd.Title,
    pd.[Description],
    pd.FileUrl,
    pd.FileName,
    pd.FileContentType,
    pd.FileSize,
    pd.Source,
    pd.UploadedByUserId,
    FORMAT(pd.UploadedAt, 'yyyy-MM-ddTHH:mm:ss') AS UploadedAt,
    FORMAT(pd.CreatedAt, 'yyyy-MM-ddTHH:mm:ss') AS CreatedAt
FROM dbo.PatientDocuments pd;
GO

-- 4. lab_results_view
CREATE OR ALTER VIEW dbo.lab_results_view AS
SELECT
    lr.Id,
    lr.PatientId,
    lr.BookingId,
    lr.ConsultationId,
    lr.LabOrderItemId,
    lr.ResultTitle,
    lr.ResultText,
    lr.ResultFileUrl AS FileUrl,
    lr.FileName,
    lr.FileContentType,
    lr.Status,
    lr.UploadedByUserId,
    FORMAT(lr.UploadedAt, 'yyyy-MM-ddTHH:mm:ss') AS UploadedAt,
    FORMAT(lr.CreatedAt, 'yyyy-MM-ddTHH:mm:ss') AS CreatedAt
FROM dbo.LabResults lr;
GO

-- 5. consultation_record_view
CREATE OR ALTER VIEW dbo.consultation_record_view AS
SELECT
    c.Id,
    c.BookingId,
    c.PatientId,
    c.DoctorId,
    c.GeneralNotes,
    c.Status,
    FORMAT(c.StartedAt, 'yyyy-MM-ddTHH:mm:ss') AS StartedAt,
    FORMAT(c.CompletedAt, 'yyyy-MM-ddTHH:mm:ss') AS CompletedAt,
    FORMAT(c.CreatedAt, 'yyyy-MM-ddTHH:mm:ss') AS CreatedAt,
    FORMAT(c.UpdatedAt, 'yyyy-MM-ddTHH:mm:ss') AS UpdatedAt
FROM dbo.Consultations c;
GO

-- 6. doctor_patients_view
CREATE OR ALTER VIEW dbo.doctor_patients_view AS
SELECT DISTINCT
    b.DoctorId,
    b.PatientId,
    p.FirstName + ' ' + p.LastName AS PatientName,
    p.PatientCode,
    MAX(b.AppointmentDate) OVER (PARTITION BY b.DoctorId, b.PatientId) AS LatestDate,
    b.Status,
    b.QueueNumber,
    b.Id AS LatestBookingId
FROM dbo.Bookings b
INNER JOIN dbo.Patients p ON b.PatientId = p.Id
WHERE b.Status IN ('Confirmed', 'CheckedIn', 'Completed', 'InProgress');
GO

-- 7. staff_today_bookings_view — for staff dashboard
CREATE OR ALTER VIEW dbo.staff_today_bookings_view AS
SELECT
    b.Id,
    b.PatientId,
    p.FirstName + ' ' + p.LastName AS PatientName,
    p.PatientCode,
    b.DoctorId,
    d.FullName AS DoctorName,
    FORMAT(b.AppointmentDate, 'yyyy-MM-dd') AS AppointmentDate,
    FORMAT(b.SlotStartTime, N'H\:mm') AS SlotStartTime,
    FORMAT(b.SlotEndTime, N'H\:mm') AS SlotEndTime,
    b.Status,
    b.PaymentStatus,
    b.PaymentMode,
    b.QueueNumber,
    b.TotalFee,
    b.FinalAmount,
    b.AmountDue,
    b.IsWalkIn,
    b.IsProfessionalFeeWaived,
    b.Notes,
    FORMAT(b.CheckedInAt, 'yyyy-MM-ddTHH:mm:ss') AS CheckedInAt,
    FORMAT(b.DoctorCompletedAt, 'yyyy-MM-ddTHH:mm:ss') AS DoctorCompletedAt,
    b.OrNumber
FROM dbo.Bookings b
INNER JOIN dbo.Patients p ON b.PatientId = p.Id
INNER JOIN dbo.Doctors d ON b.DoctorId = d.Id
WHERE CAST(b.AppointmentDate AS DATE) = CAST(GETDATE() AS DATE);
GO
