# SUPABASE BACKEND BLUEPRINT — Clinic Booking System

## 1. Current Backend Feature Inventory

| Area | Modules | Complexity |
|---|---|---|
| Auth | Register, Login, Social (Google/Facebook), Refresh tokens, Role-based JWT | High |
| Patients | CRUD, search, portal accounts, consent management | High |
| Doctors | CRUD, schedules, blocked dates, day statuses, availability slots | High |
| Services | CRUD, categorised (Consultation/Procedure/Lab), price, duration | Medium |
| Bookings | Create (patient + walk-in), check-in, undo check-in, confirm, cancel, complete, no-show, reschedule, proof submission, status transitions | Very High |
| Consultation | SOAP notes, vital signs, diagnoses, prescriptions, lab orders, follow-ups | Very High |
| Payments | Online proof upload, clinic cash/gcash/maya/bank, waive, refund, OR number | High |
| Vaccinations | Patient vaccination CRUD, reminders | Medium |
| Documents | File upload/download for patient docs and lab results | Medium |
| Real-time | SignalR dashboard hub for staff (booking events) | Medium |
| Dashboard | Admin/staff summaries, doctor queue, today's summary | Medium |
| Settings | Clinic info, branding, payment config (GCash/Maya/Bank), policies | Low |

**Total entities:** 23 domain entities + 2 auth entities  
**Total API endpoints:** ~75+

---

## 2. Entity-to-Table Mapping

### Auth / Identity

| .NET Entity | Supabase Table | Notes |
|---|---|---|
| `ApplicationUser` (AspNetUsers) | `auth.users` | Built-in Supabase Auth table. Maps via `raw_user_meta_data` for fullName, avatarUrl |
| `RefreshToken` | `user_refresh_tokens` | Custom table; Supabase handles sessions automatically via `auth.sessions` |
| `ExternalLoginAccount` | (built into Supabase Auth) | Supabase Auth handles Google/Facebook identity linking natively |
| ASP.NET Roles | `user_roles` | Custom table since Supabase Auth doesn't have built-in RBAC. `auth.users` → `user_roles` join |

### Clinic Domain

| .NET Entity | Supabase Table | Notes |
|---|---|---|
| `Patient` | `patients` | Core patient profile, linked to `auth.users` via `user_id` |
| `Doctor` | `doctors` | Doctor profile, linked to `auth.users` via `user_id` |
| `Service` | `services` | Clinic services catalog |
| `DoctorService` | `doctor_services` | Many-to-many: which doctors offer which services |
| `Booking` | `bookings` | Core booking record with status lifecycle |
| `BookingServiceItem` | `booking_service_items` | Services attached to a booking |
| `ClinicSettings` | `clinic_settings` | Single-row clinic configuration |
| `DoctorSchedule` | `doctor_schedules` | Weekly recurring schedules |
| `DoctorBlockedDate` | `doctor_blocked_dates` | Date-specific blocks |
| `DoctorDayStatus` | `doctor_day_statuses` | Per-day status (available/limited/unavailable) |
| `Payment` | `payments` | Payment records, linked to bookings |
| `Consultation` | `consultations` | Consultation session record |
| `ConsultationVitalSign` | `consultation_vital_signs` | Embedded vital signs |
| `ConsultationSoapNote` | `consultation_soap_notes` | SOAP notes |
| `ConsultationDiagnosis` | `consultation_diagnoses` | Diagnoses (ICD-10 coded) |
| `Prescription` | `prescriptions` | Prescription header |
| `PrescriptionItem` | `prescription_items` | Individual medication lines |
| `LabOrder` | `lab_orders` | Lab order header |
| `LabOrderItem` | `lab_order_items` | Individual lab test lines |
| `LabResult` | `lab_results` | Uploaded lab result files |
| `PatientDocument` | `patient_documents` | Document upload metadata |
| `PatientVaccination` | `patient_vaccinations` | Vaccination records |
| `ConsultationFollowUp` | `consultation_follow_ups` | Follow-up instructions |

---

## 3. API Endpoint Mapping

### Auth (becomes Supabase Auth)

| .NET Endpoint | Supabase Equivalent | Strategy |
|---|---|---|
| `POST /api/auth/login` | `supabase.auth.signInWithPassword()` | Direct SDK call |
| `POST /api/auth/register` | `supabase.auth.signUp()` + create patient record | Auth SDK + RPC |
| `POST /api/auth/refresh-token` | `supabase.auth.refreshSession()` | Direct SDK call |
| `POST /api/auth/social-login` | `supabase.auth.signInWithOAuth()` | Direct SDK call |
| `POST /api/auth/logout` | `supabase.auth.signOut()` | Direct SDK call |
| `GET /api/auth/me` | `supabase.auth.getUser()` | Direct SDK call |

### Patients

| .NET Endpoint | Supabase | Strategy |
|---|---|---|
| `GET /api/patients` | `query patients` | Direct table query (RLS filtered) |
| `POST /api/patients` | `INSERT INTO patients` | Direct table insert (RPC if validation needed) |
| `GET /api/patients/{id}` | `query patients` | Direct table query |
| `POST /api/patients/{id}/portal-account` | RPC `create_portal_account` | RPC — links auth user to patient |
| `GET /api/patients/me` | `query patients WHERE user_id = auth.uid()` | Direct table query |
| `POST /api/patients/me/consent` | `UPDATE patients` | Direct update |
| Patient documents endpoints | `patient_documents` table + Storage | Mix of table query + Storage |
| Patient lab results endpoints | `lab_results` table + Storage | Mix of table query + Storage |
| Clinical history | RPC `get_clinical_history` | RPC — joins across 6+ tables |

### Doctors

| .NET Endpoint | Supabase | Strategy |
|---|---|---|
| `GET /api/doctors` | `query doctors WHERE status = 'Active'` | Direct table query |
| `GET /api/doctors/admin` | `query doctors` (admin only) | RLS-protected query |
| `GET /api/doctors/{id}` | `query doctors` | Direct table query |
| `GET /api/doctors/{id}/services` | `query doctor_services` | Direct table query |
| `POST /api/doctors` | `INSERT INTO doctors` | RPC (admin-only validation) |
| `PUT /api/doctors/{id}` | `UPDATE doctors` | Direct update (RLS for admin) |
| `PUT /api/doctors/{id}/services` | RPC `upsert_doctor_services` | RPC — bulk replace |
| `GET /api/doctors/me` | `query doctors WHERE user_id = auth.uid()` | Direct query |
| `GET /api/doctors/{id}/schedule` | `query doctor_schedules` | Direct query |
| `PUT /api/doctors/{id}/schedule` | RPC `upsert_schedule` | RPC — bulk replace weekly schedules |
| `GET /api/doctors/{id}/blocked-dates` | `query doctor_blocked_dates` | Direct query |
| `POST /api/doctors/{id}/blocked-dates` | `INSERT INTO doctor_blocked_dates` | Direct insert |
| `GET /api/doctors/{id}/day-status` | `query doctor_day_statuses` | Direct query |
| `POST /api/doctors/{id}/day-status` | RPC `upsert_day_status` | RPC — upsert |
| `GET /api/doctors/{id}/available-slots` | RPC `get_available_slots` | RPC — complex calculation |

### Bookings (highest complexity)

| .NET Endpoint | Supabase | Strategy |
|---|---|---|
| `GET /api/bookings` (filtered) | `query bookings WITH filters` | Direct query with RLS |
| `POST /api/bookings` (create) | RPC `create_booking` | RPC — validates slot, creates booking + service items |
| `GET /api/bookings/{id}` | `query bookings WHERE id = ...` | Direct query |
| `PATCH /api/bookings/{id}/check-in` | RPC `check_in_booking` | RPC — status transition + validation |
| `PATCH /api/bookings/{id}/undo-check-in` | RPC `undo_check_in` | RPC |
| `PATCH /api/bookings/{id}/doctor-complete` | RPC `doctor_complete_booking` | RPC — complex: updates consultation + prescription + lab orders |
| `GET /api/bookings/{id}/consultation-record` | `query consultations + JOINs` | Direct query (or RPC for complex joins) |
| `PATCH /api/bookings/{id}/consultation-record` | RPC `update_consultation_record` | RPC — validates doctor ownership |
| `PATCH /api/bookings/{id}/confirm` | RPC `confirm_booking` | RPC |
| `PATCH /api/bookings/{id}/cancel` | RPC `cancel_booking` | RPC |
| `PATCH /api/bookings/{id}/complete` | RPC `complete_booking` | RPC |
| `PATCH /api/bookings/{id}/no-show` | RPC `no_show_booking` | RPC |
| `PATCH /api/bookings/{id}/reschedule` | RPC `reschedule_booking` | RPC — validates slot availability |
| `POST /api/bookings/{id}/proof` | RPC `submit_proof` + Storage | RPC + Storage for proof image |
| `GET /api/bookings/me` | `query bookings WHERE patient_id IN (my patients)` | Direct query |
| `GET /api/bookings/doctor/today` | `query bookings WHERE doctor_id = ... AND date = today` | Direct query |
| `GET /api/bookings/doctor/today-summary` | RPC `get_doctor_today_summary` | RPC — aggregation query |
| `GET /api/bookings/doctor/upcoming` | `query bookings WHERE ... AND date >= today` | Direct query |
| `GET /api/bookings/doctor/patients` | RPC `get_doctor_patients` | RPC — distinct patients query |
| `GET /api/bookings/staff/today` | `query bookings WHERE date = today` | Direct query |
| `GET /api/bookings/staff/for-payment` | `query bookings WHERE payment_status = 'Unpaid'` | Direct query |
| `GET /api/bookings/pending-verification` | `query bookings WHERE status = 'ProofSubmitted'` | Direct query |
| `POST /api/bookings/walk-in` | RPC `create_walk_in_booking` | RPC |

### Payments

| .NET Endpoint | Supabase | Strategy |
|---|---|---|
| `POST /api/payments/{id}/confirm` | RPC `confirm_payment` | RPC — validates + updates booking status |
| `POST /api/payments/{id}/waive` | RPC `waive_payment` | RPC |
| `POST /api/payments/{id}/refund` | RPC `refund_payment` | RPC |
| `GET /api/payments` | `query payments` | Direct query |

### Admin / Settings / Services

| .NET Endpoint | Supabase | Strategy |
|---|---|---|
| `GET /api/admin/dashboard` | RPC `get_admin_dashboard` | RPC — complex aggregation |
| `GET /api/settings` | `query clinic_settings` | Direct query |
| `PUT /api/settings` | `UPDATE clinic_settings` | Direct update (RLS: admin only) |
| `GET /api/services` | `query services` | Direct query |
| `POST /api/services` | `INSERT INTO services` | Direct insert (RLS: admin only) |
| `PUT /api/services/{id}` | `UPDATE services` | Direct update |

### Vaccinations

| .NET Endpoint | Supabase | Strategy |
|---|---|---|
| `GET /api/vaccinations/patient/{patientId}` | `query patient_vaccinations` | Direct query |
| `POST /api/vaccinations` | `INSERT INTO patient_vaccinations` | Direct insert (RPC if complex validation) |
| `PUT /api/vaccinations/{id}` | `UPDATE patient_vaccinations` | Direct update |

### Real-time

| .NET SignalR Hub | Supabase Equivalent | Strategy |
|---|---|---|
| `ClinicDashboardHub` | Supabase Realtime | Subscribe to `bookings` table changes filtered by relevant criteria |

---

## 4. Proposed Supabase Tables

```sql
-- Core tables (23 tables + auth mapping)

-- 1. user_roles — custom role mapping since Supabase Auth lacks native RBAC
CREATE TABLE user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    role TEXT NOT NULL CHECK (role IN ('patient', 'doctor', 'staff', 'admin', 'super_admin')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(user_id, role)
);

-- 2. patients
CREATE TABLE patients (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_code TEXT NOT NULL UNIQUE,
    user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    first_name TEXT NOT NULL,
    middle_name TEXT,
    last_name TEXT NOT NULL,
    date_of_birth DATE NOT NULL,
    sex TEXT NOT NULL,
    civil_status TEXT,
    address TEXT,
    city TEXT,
    zip_code TEXT,
    contact_number TEXT,
    email TEXT,
    emergency_contact_name TEXT,
    emergency_contact_number TEXT,
    emergency_contact_relationship TEXT,
    blood_type TEXT,
    phil_health_number TEXT,
    hmo_provider TEXT,
    hmo_card_number TEXT,
    is_guest BOOLEAN NOT NULL DEFAULT false,
    is_email_verified BOOLEAN NOT NULL DEFAULT false,
    consented_at TIMESTAMPTZ,
    consent_version TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 3. doctors
CREATE TABLE doctors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    full_name TEXT NOT NULL,
    specialization TEXT NOT NULL,
    bio TEXT,
    profile_photo_url TEXT,
    license_number TEXT,
    ptr_number TEXT,
    s2_number TEXT,
    consultation_fee NUMERIC(10,2) NOT NULL DEFAULT 0,
    slot_duration_minutes INT NOT NULL DEFAULT 30,
    slot_capacity INT NOT NULL DEFAULT 1,
    daily_patient_limit INT,
    status TEXT NOT NULL DEFAULT 'Active',
    average_rating NUMERIC(3,2),
    review_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 4. services
CREATE TABLE services (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    description TEXT,
    category TEXT NOT NULL,
    price NUMERIC(10,2) NOT NULL DEFAULT 0,
    estimated_duration_minutes INT NOT NULL DEFAULT 30,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 5. doctor_services
CREATE TABLE doctor_services (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doctor_id UUID NOT NULL REFERENCES doctors(id) ON DELETE CASCADE,
    service_id UUID NOT NULL REFERENCES services(id) ON DELETE CASCADE,
    UNIQUE(doctor_id, service_id)
);

-- 6. doctor_schedules
CREATE TABLE doctor_schedules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doctor_id UUID NOT NULL REFERENCES doctors(id) ON DELETE CASCADE,
    day_of_week TEXT NOT NULL CHECK (day_of_week IN ('Monday','Tuesday','Wednesday','Thursday','Friday','Saturday','Sunday')),
    start_time TIME NOT NULL,
    end_time TIME NOT NULL
);

-- 7. doctor_blocked_dates
CREATE TABLE doctor_blocked_dates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doctor_id UUID NOT NULL REFERENCES doctors(id) ON DELETE CASCADE,
    blocked_date DATE NOT NULL,
    reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(doctor_id, blocked_date)
);

-- 8. doctor_day_statuses
CREATE TABLE doctor_day_statuses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doctor_id UUID NOT NULL REFERENCES doctors(id) ON DELETE CASCADE,
    target_date DATE NOT NULL,
    status TEXT NOT NULL CHECK (status IN ('available', 'limited', 'unavailable')),
    max_slots INT,
    reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(doctor_id, target_date)
);

-- 9. clinic_settings (single-row config)
CREATE TABLE clinic_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinic_name TEXT NOT NULL,
    logo_url TEXT,
    primary_color TEXT NOT NULL DEFAULT '#5D3E8E',
    secondary_color TEXT NOT NULL DEFAULT '#2563EB',
    address TEXT,
    phone TEXT,
    contact_email TEXT,
    facebook_url TEXT,
    instagram_url TEXT,
    operating_hours_json TEXT NOT NULL DEFAULT '{}',
    cancellation_deadline_hours INT NOT NULL DEFAULT 24,
    patient_portal_enabled BOOLEAN NOT NULL DEFAULT true,
    vaccination_reminder_enabled BOOLEAN NOT NULL DEFAULT true,
    follow_up_reminder_enabled BOOLEAN NOT NULL DEFAULT true,
    is_pay_at_clinic_mode BOOLEAN NOT NULL DEFAULT false,
    pay_at_clinic_no_show_window_minutes INT NOT NULL DEFAULT 60,
    privacy_policy_text TEXT,
    consent_version TEXT NOT NULL DEFAULT 'v1.0',
    gcash_account_name TEXT,
    gcash_number TEXT,
    gcash_qr_image_url TEXT,
    maya_account_name TEXT,
    maya_number TEXT,
    maya_qr_image_url TEXT,
    bank_name TEXT,
    bank_account_name TEXT,
    bank_account_number TEXT,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 10. bookings
CREATE TYPE booking_status AS ENUM (
    'Pending', 'ProofSubmitted', 'Confirmed', 'CheckedIn',
    'InProgress', 'OnHold', 'Cancelled', 'Completed',
    'Expired', 'NoShow', 'Rescheduled'
);
CREATE TYPE payment_mode AS ENUM ('Online', 'PayAtClinic');
CREATE TYPE payment_status AS ENUM ('Unpaid', 'Paid', 'Waived', 'Refunded');

CREATE TABLE bookings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_id UUID NOT NULL REFERENCES patients(id) ON DELETE RESTRICT,
    doctor_id UUID NOT NULL REFERENCES doctors(id) ON DELETE RESTRICT,
    appointment_date DATE NOT NULL,
    slot_start_time TIME NOT NULL,
    slot_end_time TIME NOT NULL,
    queue_number INT,
    status booking_status NOT NULL DEFAULT 'Pending',
    payment_mode payment_mode NOT NULL DEFAULT 'Online',
    payment_status payment_status NOT NULL DEFAULT 'Unpaid',
    total_amount NUMERIC(10,2) NOT NULL DEFAULT 0,
    final_amount NUMERIC(10,2),
    notes TEXT,
    created_by_user_id UUID REFERENCES auth.users(id),
    checked_in_at TIMESTAMPTZ,
    checked_in_by TEXT,
    completed_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    cancelled_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 11. booking_service_items
CREATE TABLE booking_service_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id UUID NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    service_id UUID NOT NULL REFERENCES services(id) ON DELETE RESTRICT,
    service_name TEXT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    price NUMERIC(10,2) NOT NULL DEFAULT 0
);

-- 12. payments
CREATE TABLE payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id UUID NOT NULL REFERENCES bookings(id) ON DELETE RESTRICT,
    amount NUMERIC(10,2) NOT NULL,
    payment_method TEXT NOT NULL,
    reference_number TEXT,
    proof_image_url TEXT,
    status TEXT NOT NULL DEFAULT 'Unpaid',
    or_number TEXT,
    verified_by_user_id UUID REFERENCES auth.users(id),
    verified_at TIMESTAMPTZ,
    waived_by_user_id UUID REFERENCES auth.users(id),
    waived_at TIMESTAMPTZ,
    waived_reason TEXT,
    refunded_by_user_id UUID REFERENCES auth.users(id),
    refunded_at TIMESTAMPTZ,
    refund_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 13. consultations
CREATE TABLE consultations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_id UUID NOT NULL REFERENCES patients(id) ON DELETE RESTRICT,
    doctor_id UUID REFERENCES doctors(id),
    booking_id UUID REFERENCES bookings(id),
    status TEXT NOT NULL DEFAULT 'Open',
    general_notes TEXT,
    started_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 14-19. Consultation sub-tables
CREATE TABLE consultation_vital_signs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES consultations(id) ON DELETE CASCADE,
    systolic_bp INT, diastolic_bp INT, heart_rate INT, respiratory_rate INT,
    temperature NUMERIC(4,1), oxygen_saturation INT, weight NUMERIC(5,1), height NUMERIC(5,1),
    bmi NUMERIC(4,1), pain_score INT, taken_at TIMESTAMPTZ
);

CREATE TABLE consultation_soap_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES consultations(id) ON DELETE CASCADE,
    subjective TEXT, objective TEXT, assessment TEXT, plan TEXT
);

CREATE TABLE consultation_diagnoses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES consultations(id) ON DELETE CASCADE,
    diagnosis_text TEXT NOT NULL,
    diagnosis_code TEXT,
    is_primary BOOLEAN NOT NULL DEFAULT false,
    notes TEXT
);

CREATE TABLE prescriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES consultations(id) ON DELETE CASCADE,
    notes TEXT
);

CREATE TABLE prescription_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    prescription_id UUID NOT NULL REFERENCES prescriptions(id) ON DELETE CASCADE,
    medication_name TEXT NOT NULL,
    strength TEXT, dosage TEXT, route TEXT, frequency TEXT, duration TEXT,
    quantity TEXT, instructions TEXT
);

CREATE TABLE lab_orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES consultations(id) ON DELETE CASCADE,
    notes TEXT
);

CREATE TABLE lab_order_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lab_order_id UUID NOT NULL REFERENCES lab_orders(id) ON DELETE CASCADE,
    test_name TEXT NOT NULL, test_code TEXT, instructions TEXT
);

-- 20. lab_results (file uploads)
CREATE TABLE lab_results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id UUID NOT NULL REFERENCES bookings(id) ON DELETE RESTRICT,
    patient_id UUID NOT NULL REFERENCES patients(id) ON DELETE RESTRICT,
    file_name TEXT NOT NULL,
    file_path TEXT NOT NULL,
    file_size INT,
    content_type TEXT,
    notes TEXT,
    uploaded_by_user_id UUID REFERENCES auth.users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 21. patient_documents (file uploads)
CREATE TABLE patient_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_id UUID NOT NULL REFERENCES patients(id) ON DELETE RESTRICT,
    booking_id UUID REFERENCES bookings(id),
    document_type TEXT NOT NULL,
    file_name TEXT NOT NULL,
    file_path TEXT NOT NULL,
    file_size INT,
    content_type TEXT,
    notes TEXT,
    uploaded_by_user_id UUID REFERENCES auth.users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 22. patient_vaccinations
CREATE TABLE patient_vaccinations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_id UUID NOT NULL REFERENCES patients(id) ON DELETE RESTRICT,
    vaccine_name TEXT NOT NULL,
    vaccination_date DATE NOT NULL,
    next_due_date DATE,
    dosage TEXT,
    administered_by TEXT,
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 23. consultation_follow_ups
CREATE TABLE consultation_follow_ups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES consultations(id) ON DELETE CASCADE,
    follow_up_date DATE,
    instructions TEXT,
    reason TEXT
);

-- 24. user_refresh_tokens (fallback if Supabase session management insufficient)
CREATE TABLE user_refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    token TEXT NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

---

## 5. Proposed PostgreSQL Enums

```sql
CREATE TYPE booking_status AS ENUM (
    'Pending', 'ProofSubmitted', 'Confirmed', 'CheckedIn',
    'InProgress', 'OnHold', 'Cancelled', 'Completed',
    'Expired', 'NoShow', 'Rescheduled'
);

CREATE TYPE payment_mode AS ENUM ('Online', 'PayAtClinic');
CREATE TYPE payment_status AS ENUM ('Unpaid', 'Paid', 'Waived', 'Refunded');
CREATE TYPE appointment_day AS ENUM (
    'Monday','Tuesday','Wednesday','Thursday','Friday','Saturday','Sunday'
);
CREATE TYPE doctor_day_status_type AS ENUM ('available', 'limited', 'unavailable');
CREATE TYPE doctor_status AS ENUM ('Active', 'Inactive', 'OnLeave');
```

---

## 6. Proposed RLS Policy Matrix

### Role Definitions
- **patient** — owns their own patient record, can view own bookings/records
- **doctor** — can view assigned patients, manage consultations, set schedules
- **staff** — can manage bookings, patients, payments (not medical records)
- **admin** — full CRUD on all tables
- **super_admin** — unrestricted access (system configuration)

### Policy Matrix

| Table | patient | doctor | staff | admin |
|---|---|---|---|---|
| `user_roles` | No access | No access | Read own | Read all |
| `patients` | Read/update own | Read assigned | CRUD | ALL |
| `doctors` | Read active only | Read/update own | Read | ALL |
| `services` | Read active | Read active | Read | ALL |
| `doctor_services` | Read | Read own | Read | ALL |
| `doctor_schedules` | Read | Manage own | Read | ALL |
| `doctor_blocked_dates` | Read | Manage own | Read | ALL |
| `doctor_day_statuses` | Read | Manage own | Read | ALL |
| `clinic_settings` | Read public fields | Read public fields | Read | ALL |
| `bookings` | Read/create own | Read assigned, update status | ALL read/update | ALL |
| `booking_service_items` | Read own | Read assigned | ALL | ALL |
| `payments` | Read own | Read assigned | ALL | ALL |
| `consultations` | Read own | Read/write assigned | Read | ALL |
| `consultation_vital_signs` | Read own | Read/write assigned | No access | ALL |
| `consultation_soap_notes` | Read own | Read/write assigned | No access | ALL |
| `consultation_diagnoses` | Read own | Read/write assigned | No access | ALL |
| `prescriptions` | Read own | Read/write assigned | No access | ALL |
| `prescription_items` | Read own | Read/write assigned | No access | ALL |
| `lab_orders` | Read own | Read/write assigned | Read | ALL |
| `lab_order_items` | Read own | Read/write assigned | Read | ALL |
| `lab_results` | Read own | Read assigned | Upload | ALL |
| `patient_documents` | Read own | Read assigned | Upload | ALL |
| `patient_vaccinations` | Read own | Read/write assigned | Read | ALL |
| `consultation_follow_ups` | Read own | Read/write assigned | No access | ALL |

---

## 7. Proposed RPC Functions (Sensitive Workflows)

These are the operations too complex for direct table queries — they need transaction-level validation, multi-table writes, and business logic.

### Booking Workflow
| RPC Name | Purpose | Complexity |
|---|---|---|
| `create_booking` | Validate slot availability, create booking + service items, assign queue number | High |
| `create_walk_in_booking` | Same as create_booking for walk-in (no patient portal account needed) | High |
| `check_in_booking` | Validate booking is in correct state, update status to CheckedIn, set timestamp | Low |
| `undo_check_in` | Revert check-in, restore previous status | Low |
| `confirm_booking` | Transition from pending states to Confirmed | Low |
| `cancel_booking` | Validate cancellation deadline, update status, reason | Medium |
| `complete_booking` | Mark as completed, update timestamps | Low |
| `no_show_booking` | Mark as NoShow, release slot | Low |
| `reschedule_booking` | Validate new slot, update date/time, log old values | High |
| `submit_proof` | Update proof reference, transition status | Low |

### Doctor Workflow
| RPC Name | Purpose | Complexity |
|---|---|---|
| `get_available_slots` | Calculate available time slots for a doctor+date, considering schedule, blocked dates, day status, existing bookings | High |
| `upsert_schedule` | Bulk replace weekly schedules for a doctor | Medium |
| `upsert_day_status` | Set/update a doctor's day status | Low |
| `get_doctor_today_summary` | Aggregate today's stats (checked-in, waiting, completed) | Medium |
| `get_doctor_patients` | Get distinct patients assigned to a doctor | Low |

### Consultation Workflow
| RPC Name | Purpose | Complexity |
|---|---|---|
| `doctor_complete_booking` | **Most complex**: updates consultation, vital signs, SOAP, diagnoses, prescriptions, lab orders, follow-ups — all in one transaction | Very High |
| `update_consultation_record` | Partial update of consultation sub-records | High |
| `get_clinical_history` | Join across 6+ tables for a patient's full history | High |

### Payment Workflow
| RPC Name | Purpose | Complexity |
|---|---|---|
| `confirm_payment` | Validate amount, update payment + booking status, generate OR number | Medium |
| `waive_payment` | Validate authorization, mark as waived | Low |
| `refund_payment` | Validate authorization, mark as refunded | Low |

### Dashboard
| RPC Name | Purpose | Complexity |
|---|---|---|
| `get_admin_dashboard` | Aggregate counts across bookings, patients, revenue | Medium |
| `get_staff_dashboard` | Today's queue, upcoming, recent activity | Medium |

---

## 8. Proposed Storage Buckets

| Bucket | Visibility | Access Rules | Used By |
|---|---|---|---|
| `patient-documents` | Private | Patients read own, doctors read assigned, staff/admin CRUD | Patient documents |
| `lab-results` | Private | Patients read own, doctors read assigned, staff/admin upload | Lab result files |
| `proof-payments` | Private | Patients upload own, staff verify, admin read all | Payment proof screenshots |
| `doctor-photos` | Public | Admin CRUD, public read | Doctor profile photos |
| `clinic-assets` | Public | Admin CRUD, public read | Logo, QR codes, branding |

### Storage RLS (example for `patient-documents`)
```sql
-- Read: patient can only see their own
CREATE POLICY "Patients read own documents" ON storage.objects
  FOR SELECT USING (
    bucket_id = 'patient-documents'
    AND (storage.foldername(name))[1] = auth.uid()::text
  );

-- Insert: patient can upload to their own folder
CREATE POLICY "Patients insert own documents" ON storage.objects
  FOR INSERT WITH CHECK (
    bucket_id = 'patient-documents'
    AND (storage.foldername(name))[1] = auth.uid()::text
  );
```

---

## 9. Proposed Realtime Channels

| Channel | Table | Filter | Who Subscribes | Purpose |
|---|---|---|---|---|
| `realtime:bookings` | `bookings` | `IN (staff, admin)` role | Staff dashboard | New booking, check-in, status change alerts |
| `realtime:booking:{booking_id}` | `bookings` | Specific booking | Doctor portal | Status change during consultation flow |
| `realtime:queue:{doctor_id}` | `bookings` | `doctor_id = {id} AND date = today` | Doctor/staff | Queue updates per doctor |
| `realtime:payments` | `payments` | `IN (staff, admin)` role | Staff/payment screen | New payment or status change |

---

## 10. Risks and Edge Cases

| Risk | Impact | Mitigation |
|---|---|---|
| **Supabase Auth rate limits** | Login/register failures under load | Use Supabase Pro plan. Implement exponential backoff in FE |
| **RLS complexity for booking access** | Bugs exposing wrong data | Test every role combination. Use helper functions in RLS policies |
| **File upload size limits** | Large documents/lab results rejected | Supabase free tier: 100MB per file, 1GB total. Upgrade for production |
| **Realtime message limits** | Missed real-time updates under heavy usage | Supabase Pro: 100 concurrent connections, 2M messages/day. Monitor and upgrade |
| **Doctor complete booking (single RPC)** | Transaction timeout with many prescription items | Split into multiple RPC calls if needed. Keep transaction short |
| **Slot availability race conditions** | Double-booking when two patients book simultaneously | Use SERIALIZABLE isolation in `create_booking` RPC |
| **Migration from .NET Identity** | Lost password hashes, user sessions | Users must reset passwords. Supabase Auth starts fresh |
| **Patient-patient linking** | Patient portal account linking to wrong patient record | Use email verification + OTP before linking `auth.users` to `patients` |
| **Consent management** | Missing audit trail for consent | Track consent version in patients table + log consent events |
| **OR number generation** | Gaps in sequential receipt numbers | Use a `receipt_sequences` table with atomic increment in RPC |

---

## 11. Recommended Phase Order

### Phase 1 — Foundation (Core tables + Auth)
- Run schema SQL (all tables + enums)
- Configure Supabase Auth (email/password, Google, Facebook)
- Create `user_roles` table + basic RLS
- Deploy: `get_available_slots`, `create_booking` RPCs
- Verify: Register, login, create booking, view doctors

### Phase 2 — Booking Workflow
- RPCs: check-in, undo check-in, confirm, cancel, complete, no-show, reschedule
- RPCs: submit proof, walk-in booking
- Staff/today/dashboard queries
- Realtime: staff dashboard channel
- Storage: `proof-payments` bucket

### Phase 3 — Consultation + Medical Records
- RPCs: doctor_complete_booking (BIG one), update_consultation_record
- All consultation sub-tables
- Prescriptions, lab orders, follow-ups
- RPCs: get_clinical_history
- Storage: `patient-documents`, `lab-results` buckets

### Phase 4 — Payments + Billing
- RPCs: confirm_payment, waive, refund
- Staff for-payment queries
- OR number generation
- Receipt data

### Phase 5 — Doctor Portal
- Doctor schedules (upsert_schedule)
- Blocked dates, day statuses
- Doctor-specific real-time channels
- Doctor dashboard queries

### Phase 6 — Admin + Settings
- Admin dashboard RPC
- Clinic settings management
- Service CRUD
- User management
- Audit logs

### Phase 7 — Polish + Migration
- Patient vaccination CRUD
- Consent management improvements
- Performance optimization
- Frontend rewrite to Supabase client
- End-to-end testing all flows

---

## Files Inspected

- `ClinicApp.Domain/Entities/Clinic/*.cs` (all 18 entity files)
- `ClinicApp.Domain/Entities/Authentication/ApplicationUser.cs`
- `ClinicApp.API/Controllers/*.cs` (all 9 controllers)
- `ClinicApp.Infrastructure/Persistence/AppDbContext.cs` (schema + relationships)
- `ClinicApp.Infrastructure/Authentication/AuthService.cs`
- `ClinicApp.Infrastructure/Bookings/BookingsService.cs`
- `ClinicApp.Infrastructure/Doctors/DoctorsService.cs`
- `ClinicApp.Infrastructure/Patients/PatientsService.cs`
- `ClinicApp.Infrastructure/PatientClinicalHistory/PatientClinicalHistoryService.cs`
- `ClinicApp.Infrastructure/PatientDocuments/PatientDocumentsService.cs`
- `ClinicApp.Infrastructure/PatientMedia/PatientMediaService.cs`
- `ClinicApp.Infrastructure/PatientVaccinations/PatientVaccinationsService.cs`
- `ClinicApp.Infrastructure/Settings/ClinicSettingsService.cs`
- `ClinicApp.Infrastructure/Services/ClinicServicesService.cs`
- `ClinicApp.Infrastructure/Payments/*` (via BookingsService)
- `ClinicApp.Infrastructure/Seeding/*.cs` (seed data patterns)
- `ClinicApp.API/Realtime/*.cs` (SignalR hub + notifier)
- `ClinicApp.API/Middleware/ExceptionHandlingMiddleware.cs`
- `ClinicApp.Infrastructure/DependencyInjection/*.cs`

## Assumptions Made

1. **Supabase Auth** fully replaces ASP.NET Identity — password hashes won't transfer
2. **`user_roles` table** is the custom RBAC layer since Supabase Auth doesn't have role management
3. **No migration of existing data** — clean start Supabase database
4. **File uploads** stored in Supabase Storage, not on local filesystem or blob storage
5. **Realtime subscriptions** replace SignalR Hub — FE subscribes to Postgres changes
6. **Edge Functions** used only for complex multi-table transactions, not for simple CRUD
7. **Patient-Facing workflow** (create booking, view own records) handled by direct DB queries via RLS
8. **Staff/Doctor workflows** (check-in, complete, payment) handled by RPC functions
9. **ClinicSettings** is a single-row table (not key-value like the existing AppSettings pattern)
10. **Receipt/OR numbering** needs a custom sequence table

## Missing Information

1. **Exact Google/Facebook OAuth client IDs** for the clinic system (separate from courtbooking)
2. **File naming convention** for Storage objects (folder structure pattern)
3. **Patient code generation scheme** (sequential? UUID-based? clinic-prefixed?)
4. **Queue number assignment rules** (per doctor? per day? per shift?)
5. **OR number format** (prefix, length, reset period)
6. **Specific staff permissions** — can staff view medical records? Can they write prescriptions?
7. **Consent version history** — does each consent need a separate row or just update the patient record?
8. **Follow-up reminder timing** — when to send reminders and via what channel
9. **Vaccination reminder timing** — days before due date
10. **Supabase project plan** — free tier or Pro (determines realtime/Storage limits)

## Recommended Next Prompt

> "Take the SUPABASE_BACKEND_BLUEPRINT.md from Phase 1 and start Phase 1 implementation:
> 1. Run the schema.sql in Supabase SQL Editor
> 2. Create the first batch of RPC functions: `create_booking`, `get_available_slots`
> 3. Set up Supabase Auth with Google + Facebook OAuth
> 4. Create RLS policies for patients and doctors tables
> 5. Deploy the storage buckets
> Use the Supabase project URL and anon key from the clinic project."
