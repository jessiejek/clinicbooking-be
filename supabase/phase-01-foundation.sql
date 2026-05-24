-- ============================================================
-- Phase 1 — Foundation
-- Clinic Booking Supabase Backend
-- Run this entire file in Supabase SQL Editor
-- ============================================================

-- 1. EXTENSIONS
-- ============================================================
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 2. ENUMS
-- ============================================================
CREATE TYPE app_role AS ENUM (
    'patient', 'doctor', 'staff', 'admin', 'super_admin'
);

CREATE TYPE doctor_status AS ENUM (
    'Active', 'Inactive', 'OnLeave'
);

CREATE TYPE doctor_day_status_type AS ENUM (
    'available', 'limited', 'unavailable'
);

CREATE TYPE appointment_day AS ENUM (
    'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'
);

CREATE TYPE service_category AS ENUM (
    'Consultation', 'Procedure', 'Laboratory', 'Diagnostic'
);

CREATE TYPE booking_status AS ENUM (
    'Pending', 'ProofSubmitted', 'Confirmed', 'CheckedIn',
    'InProgress', 'OnHold', 'Cancelled', 'Completed',
    'Expired', 'NoShow', 'Rescheduled'
);

CREATE TYPE payment_mode AS ENUM (
    'Online', 'PayAtClinic'
);

CREATE TYPE payment_status AS ENUM (
    'Unpaid', 'Paid', 'Waived', 'Refunded'
);

CREATE TYPE document_type AS ENUM (
    'Prescription', 'LabResult', 'MedicalCertificate', 'Referral', 'Other'
);

CREATE TYPE consultation_status AS ENUM (
    'Open', 'InProgress', 'Completed', 'Cancelled'
);

-- 3. UPDATED_AT TRIGGER
-- ============================================================
CREATE OR REPLACE FUNCTION public.set_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;

-- 4. PROFILES — links auth.users to app user data
-- ============================================================
CREATE TABLE public.profiles (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    email TEXT,
    full_name TEXT NOT NULL,
    avatar_url TEXT,
    phone TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_profiles_updated_at
    BEFORE UPDATE ON public.profiles
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

-- 5. USER ROLES — custom RBAC
-- ============================================================
CREATE TABLE public.user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    role app_role NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(user_id, role)
);

-- 6. PATIENTS
-- ============================================================
CREATE TABLE public.patients (
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

CREATE TRIGGER trg_patients_updated_at
    BEFORE UPDATE ON public.patients
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

-- 7. DOCTORS
-- ============================================================
CREATE TABLE public.doctors (
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
    status doctor_status NOT NULL DEFAULT 'Active',
    average_rating NUMERIC(3,2),
    review_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_doctors_updated_at
    BEFORE UPDATE ON public.doctors
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

-- 8. SERVICES
-- ============================================================
CREATE TABLE public.services (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    description TEXT,
    category service_category NOT NULL,
    price NUMERIC(10,2) NOT NULL DEFAULT 0,
    estimated_duration_minutes INT NOT NULL DEFAULT 30,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_services_updated_at
    BEFORE UPDATE ON public.services
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

-- 9. DOCTOR SERVICES (many-to-many)
-- ============================================================
CREATE TABLE public.doctor_services (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doctor_id UUID NOT NULL REFERENCES public.doctors(id) ON DELETE CASCADE,
    service_id UUID NOT NULL REFERENCES public.services(id) ON DELETE CASCADE,
    UNIQUE(doctor_id, service_id)
);

-- 10. DOCTOR SCHEDULES (weekly recurring)
-- ============================================================
CREATE TABLE public.doctor_schedules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doctor_id UUID NOT NULL REFERENCES public.doctors(id) ON DELETE CASCADE,
    day_of_week appointment_day NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL
);

-- 11. DOCTOR BLOCKED DATES
-- ============================================================
CREATE TABLE public.doctor_blocked_dates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doctor_id UUID NOT NULL REFERENCES public.doctors(id) ON DELETE CASCADE,
    blocked_date DATE NOT NULL,
    reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(doctor_id, blocked_date)
);

CREATE TRIGGER trg_doctor_blocked_dates_updated_at
    BEFORE UPDATE ON public.doctor_blocked_dates
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

-- 12. DOCTOR DAY STATUSES
-- ============================================================
CREATE TABLE public.doctor_day_statuses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    doctor_id UUID NOT NULL REFERENCES public.doctors(id) ON DELETE CASCADE,
    target_date DATE NOT NULL,
    status doctor_day_status_type NOT NULL DEFAULT 'available',
    max_slots INT,
    reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(doctor_id, target_date)
);

CREATE TRIGGER trg_doctor_day_statuses_updated_at
    BEFORE UPDATE ON public.doctor_day_statuses
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

-- 13. CLINIC SETTINGS (single-row config)
-- ============================================================
CREATE TABLE public.clinic_settings (
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

-- 14. HELPER FUNCTIONS FOR RLS
-- ============================================================

-- Check if current user has a specific role
CREATE OR REPLACE FUNCTION public.has_role(required_role app_role)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM public.user_roles
        WHERE user_id = auth.uid()
        AND role = required_role
    );
END;
$$;

-- Check if current user has any of the specified roles
CREATE OR REPLACE FUNCTION public.has_any_role(required_roles app_role[])
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM public.user_roles
        WHERE user_id = auth.uid()
        AND role = ANY(required_roles)
    );
END;
$$;

-- Get the patient record linked to the current auth user
CREATE OR REPLACE FUNCTION public.current_patient_id()
RETURNS UUID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_patient_id UUID;
BEGIN
    SELECT id INTO v_patient_id FROM public.patients WHERE user_id = auth.uid();
    RETURN v_patient_id;
END;
$$;

-- Get the doctor record linked to the current auth user
CREATE OR REPLACE FUNCTION public.current_doctor_id()
RETURNS UUID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_doctor_id UUID;
BEGIN
    SELECT id INTO v_doctor_id FROM public.doctors WHERE user_id = auth.uid();
    RETURN v_doctor_id;
END;
$$;

-- 15. ENABLE RLS ON ALL TABLES
-- ============================================================
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.patients ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.doctors ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.services ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.doctor_services ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.doctor_schedules ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.doctor_blocked_dates ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.doctor_day_statuses ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.clinic_settings ENABLE ROW LEVEL SECURITY;

-- 16. RLS POLICIES
-- ============================================================

-- PROFILES
-- Users read own profile; admin reads all
CREATE POLICY "profiles_select_own" ON public.profiles
    FOR SELECT USING (auth.uid() = id OR public.has_any_role(ARRAY['admin', 'super_admin']));

CREATE POLICY "profiles_insert_own" ON public.profiles
    FOR INSERT WITH CHECK (auth.uid() = id);

CREATE POLICY "profiles_update_own" ON public.profiles
    FOR UPDATE USING (auth.uid() = id OR public.has_any_role(ARRAY['admin', 'super_admin']));

-- USER ROLES
-- Only admin/super_admin can manage roles
CREATE POLICY "user_roles_select_admin" ON public.user_roles
    FOR SELECT USING (public.has_any_role(ARRAY['admin', 'super_admin']));

CREATE POLICY "user_roles_insert_admin" ON public.user_roles
    FOR INSERT WITH CHECK (public.has_any_role(ARRAY['admin', 'super_admin']));

CREATE POLICY "user_roles_update_admin" ON public.user_roles
    FOR UPDATE USING (public.has_any_role(ARRAY['admin', 'super_admin']));

CREATE POLICY "user_roles_delete_admin" ON public.user_roles
    FOR DELETE USING (public.has_any_role(ARRAY['admin', 'super_admin']));

-- PATIENTS
-- Patients read/update own; doctors read assigned; staff read; admin CRUD
CREATE POLICY "patients_select_own" ON public.patients
    FOR SELECT USING (
        auth.uid() = user_id
        OR public.has_any_role(ARRAY['doctor', 'staff', 'admin', 'super_admin'])
    );

CREATE POLICY "patients_insert_admin" ON public.patients
    FOR INSERT WITH CHECK (
        auth.uid() = user_id
        OR public.has_any_role(ARRAY['staff', 'admin', 'super_admin'])
    );

CREATE POLICY "patients_update_own" ON public.patients
    FOR UPDATE USING (
        auth.uid() = user_id
        OR public.has_any_role(ARRAY['staff', 'admin', 'super_admin'])
    );

CREATE POLICY "patients_delete_admin" ON public.patients
    FOR DELETE USING (public.has_any_role(ARRAY['admin', 'super_admin']));

-- DOCTORS
-- Public can read active doctors; doctors update own; admin CRUD
CREATE POLICY "doctors_select_public" ON public.doctors
    FOR SELECT USING (status = 'Active' OR auth.uid() = user_id OR public.has_any_role(ARRAY['staff', 'admin', 'super_admin']));

CREATE POLICY "doctors_insert_admin" ON public.doctors
    FOR INSERT WITH CHECK (public.has_any_role(ARRAY['admin', 'super_admin']));

CREATE POLICY "doctors_update_own_or_admin" ON public.doctors
    FOR UPDATE USING (auth.uid() = user_id OR public.has_any_role(ARRAY['admin', 'super_admin']));

CREATE POLICY "doctors_delete_admin" ON public.doctors
    FOR DELETE USING (public.has_any_role(ARRAY['admin', 'super_admin']));

-- SERVICES
-- Public reads active; admin CRUD
CREATE POLICY "services_select_public" ON public.services
    FOR SELECT USING (is_active = true OR public.has_any_role(ARRAY['staff', 'admin', 'super_admin']));

CREATE POLICY "services_insert_admin" ON public.services
    FOR INSERT WITH CHECK (public.has_any_role(ARRAY['admin', 'super_admin']));

CREATE POLICY "services_update_admin" ON public.services
    FOR UPDATE USING (public.has_any_role(ARRAY['admin', 'super_admin']));

CREATE POLICY "services_delete_admin" ON public.services
    FOR DELETE USING (public.has_any_role(ARRAY['admin', 'super_admin']));

-- DOCTOR SERVICES
-- Public reads; admin CRUD
CREATE POLICY "doctor_services_select_public" ON public.doctor_services
    FOR SELECT USING (true);

CREATE POLICY "doctor_services_insert_admin" ON public.doctor_services
    FOR INSERT WITH CHECK (public.has_any_role(ARRAY['admin', 'super_admin']));

CREATE POLICY "doctor_services_update_admin" ON public.doctor_services
    FOR UPDATE USING (public.has_any_role(ARRAY['admin', 'super_admin']));

CREATE POLICY "doctor_services_delete_admin" ON public.doctor_services
    FOR DELETE USING (public.has_any_role(ARRAY['admin', 'super_admin']));

-- DOCTOR SCHEDULES
-- Public reads; doctors manage own; admin CRUD
CREATE POLICY "doctor_schedules_select_public" ON public.doctor_schedules
    FOR SELECT USING (true);

CREATE POLICY "doctor_schedules_insert_doctor" ON public.doctor_schedules
    FOR INSERT WITH CHECK (
        doctor_id IN (SELECT id FROM public.doctors WHERE user_id = auth.uid())
        OR public.has_any_role(ARRAY['admin', 'super_admin'])
    );

CREATE POLICY "doctor_schedules_update_doctor" ON public.doctor_schedules
    FOR UPDATE USING (
        doctor_id IN (SELECT id FROM public.doctors WHERE user_id = auth.uid())
        OR public.has_any_role(ARRAY['admin', 'super_admin'])
    );

CREATE POLICY "doctor_schedules_delete_doctor" ON public.doctor_schedules
    FOR DELETE USING (
        doctor_id IN (SELECT id FROM public.doctors WHERE user_id = auth.uid())
        OR public.has_any_role(ARRAY['admin', 'super_admin'])
    );

-- DOCTOR BLOCKED DATES
-- Public reads; doctors manage own; admin CRUD
CREATE POLICY "doctor_blocked_dates_select_public" ON public.doctor_blocked_dates
    FOR SELECT USING (true);

CREATE POLICY "doctor_blocked_dates_insert_doctor" ON public.doctor_blocked_dates
    FOR INSERT WITH CHECK (
        doctor_id IN (SELECT id FROM public.doctors WHERE user_id = auth.uid())
        OR public.has_any_role(ARRAY['admin', 'super_admin'])
    );

CREATE POLICY "doctor_blocked_dates_update_doctor" ON public.doctor_blocked_dates
    FOR UPDATE USING (
        doctor_id IN (SELECT id FROM public.doctors WHERE user_id = auth.uid())
        OR public.has_any_role(ARRAY['admin', 'super_admin'])
    );

CREATE POLICY "doctor_blocked_dates_delete_doctor" ON public.doctor_blocked_dates
    FOR DELETE USING (
        doctor_id IN (SELECT id FROM public.doctors WHERE user_id = auth.uid())
        OR public.has_any_role(ARRAY['admin', 'super_admin'])
    );

-- DOCTOR DAY STATUSES
-- Public reads; doctors manage own; admin CRUD
CREATE POLICY "doctor_day_statuses_select_public" ON public.doctor_day_statuses
    FOR SELECT USING (true);

CREATE POLICY "doctor_day_statuses_insert_doctor" ON public.doctor_day_statuses
    FOR INSERT WITH CHECK (
        doctor_id IN (SELECT id FROM public.doctors WHERE user_id = auth.uid())
        OR public.has_any_role(ARRAY['admin', 'super_admin'])
    );

CREATE POLICY "doctor_day_statuses_update_doctor" ON public.doctor_day_statuses
    FOR UPDATE USING (
        doctor_id IN (SELECT id FROM public.doctors WHERE user_id = auth.uid())
        OR public.has_any_role(ARRAY['admin', 'super_admin'])
    );

CREATE POLICY "doctor_day_statuses_delete_doctor" ON public.doctor_day_statuses
    FOR DELETE USING (
        doctor_id IN (SELECT id FROM public.doctors WHERE user_id = auth.uid())
        OR public.has_any_role(ARRAY['admin', 'super_admin'])
    );

-- CLINIC SETTINGS
-- Authenticated users read; admin updates
CREATE POLICY "clinic_settings_select_auth" ON public.clinic_settings
    FOR SELECT USING (auth.role() = 'authenticated');

CREATE POLICY "clinic_settings_update_admin" ON public.clinic_settings
    FOR UPDATE USING (public.has_any_role(ARRAY['admin', 'super_admin']));

-- 17. INDEXES
-- ============================================================
CREATE INDEX idx_profiles_email ON public.profiles(email);
CREATE INDEX idx_user_roles_user_id ON public.user_roles(user_id);
CREATE INDEX idx_user_roles_role ON public.user_roles(role);
CREATE INDEX idx_patients_user_id ON public.patients(user_id);
CREATE INDEX idx_patients_code ON public.patients(patient_code);
CREATE INDEX idx_patients_name ON public.patients(last_name, first_name);
CREATE INDEX idx_doctors_user_id ON public.doctors(user_id);
CREATE INDEX idx_doctors_status ON public.doctors(status);
CREATE INDEX idx_doctors_specialization ON public.doctors(specialization);
CREATE INDEX idx_services_is_active ON public.services(is_active);
CREATE INDEX idx_services_category ON public.services(category);
CREATE INDEX idx_doctor_services_doctor_id ON public.doctor_services(doctor_id);
CREATE INDEX idx_doctor_services_service_id ON public.doctor_services(service_id);
CREATE INDEX idx_doctor_schedules_doctor_id ON public.doctor_schedules(doctor_id);
CREATE INDEX idx_doctor_schedules_day ON public.doctor_schedules(doctor_id, day_of_week);
CREATE INDEX idx_doctor_blocked_dates_doctor ON public.doctor_blocked_dates(doctor_id);
CREATE INDEX idx_doctor_blocked_dates_date ON public.doctor_blocked_dates(doctor_id, blocked_date);
CREATE INDEX idx_doctor_day_statuses_doctor ON public.doctor_day_statuses(doctor_id);
CREATE INDEX idx_doctor_day_statuses_date ON public.doctor_day_statuses(doctor_id, target_date);

-- 18. AUTO-CREATE PROFILE ON USER SIGNUP
-- ============================================================
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    INSERT INTO public.profiles (id, email, full_name, avatar_url)
    VALUES (
        NEW.id,
        NEW.email,
        COALESCE(NEW.raw_user_meta_data ->> 'full_name', NEW.email, 'User'),
        NEW.raw_user_meta_data ->> 'avatar_url'
    );
    RETURN NEW;
END;
$$;

CREATE OR REPLACE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- ============================================================
-- COMMENTS FOR FUTURE SEEDING
-- ============================================================
-- Seed data needed in later phases:
-- 1. Default clinic_settings row (INSERT with id = gen_random_uuid())
-- 2. Sample services (Consultation, Procedure, Laboratory, Diagnostic)
-- 3. Sample patients for testing
-- 4. Default admin user with super_admin role
-- Do NOT create fake auth.users rows manually — use Supabase Auth admin API
-- ============================================================
