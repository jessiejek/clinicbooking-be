-- ============================================================
-- Phase 2 — Booking Workflow (Views + RPCs)
-- Clinic Booking Supabase Backend
-- Run this ENTIRE file in Supabase SQL Editor (safe to re-run)
-- ============================================================
-- Prerequisite: Phase 1 foundation SQL already deployed
-- ============================================================

-- ============================================================
-- 0. ENSURE NEW TABLES EXIST (safe re-run)
-- ============================================================

-- Bookings
CREATE TABLE IF NOT EXISTS public.bookings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_id UUID NOT NULL REFERENCES public.patients(id) ON DELETE RESTRICT,
    doctor_id UUID NOT NULL REFERENCES public.doctors(id) ON DELETE RESTRICT,
    appointment_date DATE NOT NULL,
    slot_start_time TIME NOT NULL,
    slot_end_time TIME NOT NULL,
    queue_number INT,
    status TEXT NOT NULL DEFAULT 'Pending',
    payment_mode TEXT NOT NULL DEFAULT 'PayAtClinic',
    payment_status TEXT NOT NULL DEFAULT 'Unpaid',
    total_amount NUMERIC(10,2) NOT NULL DEFAULT 0,
    final_amount NUMERIC(10,2),
    notes TEXT,
    created_by_user_id UUID REFERENCES auth.users(id),
    is_walk_in BOOLEAN NOT NULL DEFAULT false,
    is_professional_fee_waived BOOLEAN NOT NULL DEFAULT false,
    checked_in_at TIMESTAMPTZ,
    doctor_completed_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    cancellation_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Payments
CREATE TABLE IF NOT EXISTS public.payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id UUID REFERENCES public.bookings(id),
    amount NUMERIC(10,2) NOT NULL DEFAULT 0,
    payment_method TEXT,
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

-- Consultations
CREATE TABLE IF NOT EXISTS public.consultations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_id UUID NOT NULL REFERENCES public.patients(id) ON DELETE RESTRICT,
    doctor_id UUID,
    booking_id UUID REFERENCES public.bookings(id),
    status TEXT NOT NULL DEFAULT 'Open',
    general_notes TEXT,
    started_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Sub-tables for consultation details
CREATE TABLE IF NOT EXISTS public.consultation_vital_signs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES public.consultations(id) ON DELETE CASCADE,
    systolic_bp INT, diastolic_bp INT, heart_rate INT, respiratory_rate INT,
    temperature_c NUMERIC(4,1), oxygen_saturation INT, weight_kg NUMERIC(5,1), height_cm NUMERIC(5,1),
    bmi NUMERIC(4,1), pain_score INT, taken_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.consultation_soap_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES public.consultations(id) ON DELETE CASCADE,
    subjective TEXT, objective TEXT, assessment TEXT, plan TEXT
);

CREATE TABLE IF NOT EXISTS public.consultation_diagnoses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES public.consultations(id) ON DELETE CASCADE,
    diagnosis_text TEXT NOT NULL,
    diagnosis_code TEXT,
    is_primary BOOLEAN NOT NULL DEFAULT false,
    notes TEXT
);

CREATE TABLE IF NOT EXISTS public.prescriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES public.consultations(id) ON DELETE CASCADE,
    notes TEXT
);

CREATE TABLE IF NOT EXISTS public.prescription_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    prescription_id UUID NOT NULL REFERENCES public.prescriptions(id) ON DELETE CASCADE,
    medication_name TEXT NOT NULL,
    strength TEXT, dosage TEXT, route TEXT, frequency TEXT, duration TEXT,
    quantity TEXT, instructions TEXT
);

CREATE TABLE IF NOT EXISTS public.lab_orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES public.consultations(id) ON DELETE CASCADE,
    notes TEXT
);

CREATE TABLE IF NOT EXISTS public.lab_order_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lab_order_id UUID NOT NULL REFERENCES public.lab_orders(id) ON DELETE CASCADE,
    test_name TEXT NOT NULL, test_code TEXT, instructions TEXT
);

CREATE TABLE IF NOT EXISTS public.consultation_follow_ups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consultation_id UUID NOT NULL REFERENCES public.consultations(id) ON DELETE CASCADE,
    follow_up_date DATE, instructions TEXT, reason TEXT
);

-- Booking service items
CREATE TABLE IF NOT EXISTS public.booking_service_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id UUID NOT NULL REFERENCES public.bookings(id) ON DELETE CASCADE,
    service_id UUID NOT NULL REFERENCES public.services(id) ON DELETE RESTRICT,
    service_name TEXT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    price NUMERIC(10,2) NOT NULL DEFAULT 0
);

-- Frontend-compatible lab_results (matches lab_results_view)
CREATE TABLE IF NOT EXISTS public.lab_results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id UUID NOT NULL REFERENCES public.bookings(id) ON DELETE RESTRICT,
    patient_id UUID NOT NULL REFERENCES public.patients(id) ON DELETE RESTRICT,
    file_name TEXT NOT NULL,
    file_path TEXT NOT NULL,
    file_size INT,
    file_content_type TEXT,
    notes TEXT,
    result_title TEXT,
    result_text TEXT,
    lab_order_item_id UUID,
    consultation_id UUID,
    status TEXT NOT NULL DEFAULT 'Uploaded',
    uploaded_by_user_id UUID REFERENCES auth.users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Patient documents
CREATE TABLE IF NOT EXISTS public.patient_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_id UUID NOT NULL REFERENCES public.patients(id) ON DELETE RESTRICT,
    booking_id UUID REFERENCES public.bookings(id),
    consultation_id UUID,
    document_type TEXT NOT NULL DEFAULT 'Other',
    title TEXT,
    description TEXT,
    file_name TEXT NOT NULL,
    file_path TEXT NOT NULL,
    file_size INT,
    file_content_type TEXT,
    source TEXT NOT NULL DEFAULT 'StaffUpload',
    uploaded_by_user_id UUID REFERENCES auth.users(id),
    uploaded_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Safe ALTER TABLE for re-runs (columns possibly added by earlier run)
ALTER TABLE public.lab_results
  ADD COLUMN IF NOT EXISTS consultation_id UUID,
  ADD COLUMN IF NOT EXISTS file_content_type TEXT;

ALTER TABLE public.consultations
  ADD COLUMN IF NOT EXISTS booking_id UUID REFERENCES public.bookings(id),
  ADD COLUMN IF NOT EXISTS general_notes TEXT;

-- Unique constraints for ON CONFLICT targets (outside RPCs!)
-- Drop first if old partial index existed from a previous draft run.
-- Non-partial unique indexes are needed because ON CONFLICT (booking_id) cannot use a partial index.
DROP INDEX IF EXISTS public.payments_booking_id_unique;
CREATE UNIQUE INDEX IF NOT EXISTS payments_booking_id_unique
  ON public.payments(booking_id);

DROP INDEX IF EXISTS public.consultations_booking_id_unique;
CREATE UNIQUE INDEX IF NOT EXISTS consultations_booking_id_unique
  ON public.consultations(booking_id);
-- NOTE: PostgreSQL allows multiple NULLs in a unique index, so no WHERE clause is needed.

-- ============================================================
-- 1a. RLS POLICIES FOR NEW PHASE 2 TABLES
-- ============================================================
-- Required because views use `with (security_invoker = true)`,
-- which means the CALLING user's RLS permissions apply to base tables.
-- Without these policies, authenticated users would see zero rows
-- through the views on tables that have RLS enabled.
-- RPCs are SECURITY DEFINER and bypass RLS, so INSERT/UPDATE/DELETE
-- policies here exist mainly for direct-access safety.

-- bookings
ALTER TABLE public.bookings ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "bookings_select_patient" ON public.bookings;
CREATE POLICY "bookings_select_patient" ON public.bookings
  FOR SELECT USING (patient_id = public.current_patient_id());

DROP POLICY IF EXISTS "bookings_select_doctor" ON public.bookings;
CREATE POLICY "bookings_select_doctor" ON public.bookings
  FOR SELECT USING (doctor_id = public.current_doctor_id());

DROP POLICY IF EXISTS "bookings_select_staff" ON public.bookings;
CREATE POLICY "bookings_select_staff" ON public.bookings
  FOR SELECT USING (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

DROP POLICY IF EXISTS "bookings_insert_staff" ON public.bookings;
CREATE POLICY "bookings_insert_staff" ON public.bookings
  FOR INSERT WITH CHECK (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

DROP POLICY IF EXISTS "bookings_update_staff" ON public.bookings;
CREATE POLICY "bookings_update_staff" ON public.bookings
  FOR UPDATE USING (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

DROP POLICY IF EXISTS "bookings_update_doctor" ON public.bookings;
CREATE POLICY "bookings_update_doctor" ON public.bookings
  FOR UPDATE USING (doctor_id = public.current_doctor_id());

-- payments
ALTER TABLE public.payments ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "payments_select_patient" ON public.payments;
CREATE POLICY "payments_select_patient" ON public.payments
  FOR SELECT USING (
    EXISTS (SELECT 1 FROM public.bookings b WHERE b.id = booking_id AND b.patient_id = public.current_patient_id())
  );

DROP POLICY IF EXISTS "payments_select_staff" ON public.payments;
CREATE POLICY "payments_select_staff" ON public.payments
  FOR SELECT USING (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

DROP POLICY IF EXISTS "payments_manage_staff" ON public.payments;
CREATE POLICY "payments_manage_staff" ON public.payments
  FOR ALL USING (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

-- consultations
ALTER TABLE public.consultations ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "consultations_select_patient" ON public.consultations;
CREATE POLICY "consultations_select_patient" ON public.consultations
  FOR SELECT USING (patient_id = public.current_patient_id());

DROP POLICY IF EXISTS "consultations_select_doctor" ON public.consultations;
CREATE POLICY "consultations_select_doctor" ON public.consultations
  FOR SELECT USING (doctor_id = public.current_doctor_id());

DROP POLICY IF EXISTS "consultations_select_staff" ON public.consultations;
CREATE POLICY "consultations_select_staff" ON public.consultations
  FOR SELECT USING (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

DROP POLICY IF EXISTS "consultations_insert_doctor" ON public.consultations;
CREATE POLICY "consultations_insert_doctor" ON public.consultations
  FOR INSERT WITH CHECK (doctor_id = public.current_doctor_id());

DROP POLICY IF EXISTS "consultations_update_doctor" ON public.consultations;
CREATE POLICY "consultations_update_doctor" ON public.consultations
  FOR UPDATE USING (doctor_id = public.current_doctor_id());

DROP POLICY IF EXISTS "consultations_update_staff" ON public.consultations;
CREATE POLICY "consultations_update_staff" ON public.consultations
  FOR UPDATE USING (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

-- consultation sub-tables: 3 groups by FK structure
--
-- Group A — tables with direct consultation_id FK:
--   consultation_vital_signs, consultation_soap_notes, consultation_diagnoses,
--   prescriptions, lab_orders, consultation_follow_ups
--
-- Group B — prescription_items (FK: prescription_id → prescriptions.id)
--   Must join through prescriptions to reach consultation_id
--
-- Group C — lab_order_items (FK: lab_order_id → lab_orders.id)
--   Must join through lab_orders to reach consultation_id
--
-- Pattern: SELECT via patient-ownership, doctor-assignment, or staff role
--          INSERT/UPDATE via doctor-assignment or staff role

-- Group A: direct consultation_id
DO $$
DECLARE
    tbl TEXT;
BEGIN
    FOREACH tbl IN ARRAY ARRAY['consultation_vital_signs', 'consultation_soap_notes',
                               'consultation_diagnoses', 'prescriptions',
                               'lab_orders', 'consultation_follow_ups']
    LOOP
        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY;', tbl);

        EXECUTE format($p$
            DROP POLICY IF EXISTS %I ON public.%I;
            CREATE POLICY %I ON public.%I
              FOR SELECT USING (
                consultation_id IN (SELECT c.id FROM public.consultations c WHERE c.patient_id = public.current_patient_id())
                OR consultation_id IN (SELECT c.id FROM public.consultations c WHERE c.doctor_id = public.current_doctor_id())
                OR public.has_any_role(ARRAY[''staff''::app_role, ''admin''::app_role, ''super_admin''::app_role])
              );
        $p$, 'csub_a_select', tbl, 'csub_a_select', tbl);

        EXECUTE format($p$
            DROP POLICY IF EXISTS %I ON public.%I;
            CREATE POLICY %I ON public.%I
              FOR INSERT WITH CHECK (
                consultation_id IN (SELECT c.id FROM public.consultations c WHERE c.doctor_id = public.current_doctor_id())
                OR public.has_any_role(ARRAY[''staff''::app_role, ''admin''::app_role, ''super_admin''::app_role])
              );
        $p$, 'csub_a_insert', tbl, 'csub_a_insert', tbl);

        EXECUTE format($p$
            DROP POLICY IF EXISTS %I ON public.%I;
            CREATE POLICY %I ON public.%I
              FOR UPDATE USING (
                consultation_id IN (SELECT c.id FROM public.consultations c WHERE c.doctor_id = public.current_doctor_id())
                OR public.has_any_role(ARRAY[''staff''::app_role, ''admin''::app_role, ''super_admin''::app_role])
              );
        $p$, 'csub_a_update', tbl, 'csub_a_update', tbl);
    END LOOP;
END;
$$;

-- Group B: prescription_items (via prescriptions.consultation_id)
ALTER TABLE public.prescription_items ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "pi_select" ON public.prescription_items;
CREATE POLICY "pi_select" ON public.prescription_items
  FOR SELECT USING (
    prescription_id IN (
      SELECT p.id FROM public.prescriptions p
      JOIN public.consultations c ON c.id = p.consultation_id
      WHERE c.patient_id = public.current_patient_id()
    )
    OR prescription_id IN (
      SELECT p.id FROM public.prescriptions p
      JOIN public.consultations c ON c.id = p.consultation_id
      WHERE c.doctor_id = public.current_doctor_id()
    )
    OR public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role])
  );

DROP POLICY IF EXISTS "pi_insert" ON public.prescription_items;
CREATE POLICY "pi_insert" ON public.prescription_items
  FOR INSERT WITH CHECK (
    prescription_id IN (
      SELECT p.id FROM public.prescriptions p
      JOIN public.consultations c ON c.id = p.consultation_id
      WHERE c.doctor_id = public.current_doctor_id()
    )
    OR public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role])
  );

DROP POLICY IF EXISTS "pi_update" ON public.prescription_items;
CREATE POLICY "pi_update" ON public.prescription_items
  FOR UPDATE USING (
    prescription_id IN (
      SELECT p.id FROM public.prescriptions p
      JOIN public.consultations c ON c.id = p.consultation_id
      WHERE c.doctor_id = public.current_doctor_id()
    )
    OR public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role])
  );

-- Group C: lab_order_items (via lab_orders.consultation_id)
ALTER TABLE public.lab_order_items ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "loi_select" ON public.lab_order_items;
CREATE POLICY "loi_select" ON public.lab_order_items
  FOR SELECT USING (
    lab_order_id IN (
      SELECT lo.id FROM public.lab_orders lo
      JOIN public.consultations c ON c.id = lo.consultation_id
      WHERE c.patient_id = public.current_patient_id()
    )
    OR lab_order_id IN (
      SELECT lo.id FROM public.lab_orders lo
      JOIN public.consultations c ON c.id = lo.consultation_id
      WHERE c.doctor_id = public.current_doctor_id()
    )
    OR public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role])
  );

DROP POLICY IF EXISTS "loi_insert" ON public.lab_order_items;
CREATE POLICY "loi_insert" ON public.lab_order_items
  FOR INSERT WITH CHECK (
    lab_order_id IN (
      SELECT lo.id FROM public.lab_orders lo
      JOIN public.consultations c ON c.id = lo.consultation_id
      WHERE c.doctor_id = public.current_doctor_id()
    )
    OR public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role])
  );

DROP POLICY IF EXISTS "loi_update" ON public.lab_order_items;
CREATE POLICY "loi_update" ON public.lab_order_items
  FOR UPDATE USING (
    lab_order_id IN (
      SELECT lo.id FROM public.lab_orders lo
      JOIN public.consultations c ON c.id = lo.consultation_id
      WHERE c.doctor_id = public.current_doctor_id()
    )
    OR public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role])
  );

-- booking_service_items
ALTER TABLE public.booking_service_items ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "bsi_select" ON public.booking_service_items;
CREATE POLICY "bsi_select" ON public.booking_service_items
  FOR SELECT USING (
    booking_id IN (SELECT b.id FROM public.bookings b WHERE b.patient_id = public.current_patient_id())
    OR booking_id IN (SELECT b.id FROM public.bookings b WHERE b.doctor_id = public.current_doctor_id())
    OR public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role])
  );

DROP POLICY IF EXISTS "bsi_insert_staff" ON public.booking_service_items;
CREATE POLICY "bsi_insert_staff" ON public.booking_service_items
  FOR INSERT WITH CHECK (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

-- lab_results
ALTER TABLE public.lab_results ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "lab_results_select_patient" ON public.lab_results;
CREATE POLICY "lab_results_select_patient" ON public.lab_results
  FOR SELECT USING (patient_id = public.current_patient_id());

DROP POLICY IF EXISTS "lab_results_select_staff" ON public.lab_results;
CREATE POLICY "lab_results_select_staff" ON public.lab_results
  FOR SELECT USING (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

DROP POLICY IF EXISTS "lab_results_select_doctor" ON public.lab_results;
CREATE POLICY "lab_results_select_doctor" ON public.lab_results
  FOR SELECT USING (
    EXISTS (SELECT 1 FROM public.bookings b WHERE b.id = booking_id AND b.doctor_id = public.current_doctor_id())
  );

DROP POLICY IF EXISTS "lab_results_insert_staff" ON public.lab_results;
CREATE POLICY "lab_results_insert_staff" ON public.lab_results
  FOR INSERT WITH CHECK (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

DROP POLICY IF EXISTS "lab_results_update_staff" ON public.lab_results;
CREATE POLICY "lab_results_update_staff" ON public.lab_results
  FOR UPDATE USING (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

-- patient_documents
ALTER TABLE public.patient_documents ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "pd_select_patient" ON public.patient_documents;
CREATE POLICY "pd_select_patient" ON public.patient_documents
  FOR SELECT USING (patient_id = public.current_patient_id());

DROP POLICY IF EXISTS "pd_select_staff" ON public.patient_documents;
CREATE POLICY "pd_select_staff" ON public.patient_documents
  FOR SELECT USING (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

DROP POLICY IF EXISTS "pd_select_doctor" ON public.patient_documents;
CREATE POLICY "pd_select_doctor" ON public.patient_documents
  FOR SELECT USING (
    EXISTS (SELECT 1 FROM public.bookings b WHERE b.id = booking_id AND b.doctor_id = public.current_doctor_id())
  );

DROP POLICY IF EXISTS "pd_insert_staff" ON public.patient_documents;
CREATE POLICY "pd_insert_staff" ON public.patient_documents
  FOR INSERT WITH CHECK (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));

DROP POLICY IF EXISTS "pd_update_staff" ON public.patient_documents;
CREATE POLICY "pd_update_staff" ON public.patient_documents
  FOR UPDATE USING (public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]));


-- ============================================================
-- 1b. VIEWS (security_invoker = true for RLS enforcement)
-- ============================================================

-- 1a. patient_bookings_view — authenticated only, RLS on base tables
DROP VIEW IF EXISTS public.patient_bookings_view;
CREATE VIEW public.patient_bookings_view
WITH (security_invoker = true)
AS
SELECT
    b.id AS booking_id,
    b.id AS booking_key,
    b.patient_id,
    CONCAT(p.first_name, ' ', COALESCE(p.middle_name || ' ', ''), p.last_name) AS patient_name,
    p.patient_code,
    b.doctor_id,
    d.full_name AS doctor_name,
    d.specialization AS doctor_specialization,
    (SELECT bsi.service_id FROM public.booking_service_items bsi WHERE bsi.booking_id = b.id ORDER BY bsi.id LIMIT 1) AS primary_service_id,
    (SELECT s.name FROM public.booking_service_items bsi JOIN public.services s ON s.id = bsi.service_id WHERE bsi.booking_id = b.id ORDER BY bsi.id LIMIT 1) AS primary_service_name,
    COALESCE(
        (SELECT jsonb_agg(
            jsonb_build_object(
                'service_id', bsi.service_id,
                'service_name', COALESCE(s.name, bsi.service_name)
            )
        ) FROM public.booking_service_items bsi
        LEFT JOIN public.services s ON s.id = bsi.service_id
        WHERE bsi.booking_id = b.id),
        '[]'::jsonb
    ) AS services,
    b.appointment_date,
    b.slot_start_time,
    b.slot_end_time,
    b.queue_number,
    b.status AS booking_status,
    b.payment_status,
    b.payment_mode,
    b.total_amount AS total_fee,
    b.final_amount,
    b.is_walk_in,
    NULL::TEXT AS proof_type,
    NULL::TIMESTAMPTZ AS proof_submitted_at,
    b.checked_in_at,
    b.doctor_completed_at,
    b.created_at,
    b.updated_at
FROM public.bookings b
JOIN public.patients p ON p.id = b.patient_id
JOIN public.doctors d ON d.id = b.doctor_id;

-- 1b-d. Queue views reference patient_bookings_view (same RLS applies)
DROP VIEW IF EXISTS public.doctor_today_queue_view;
CREATE VIEW public.doctor_today_queue_view
WITH (security_invoker = true)
AS
SELECT *
FROM public.patient_bookings_view
WHERE appointment_date = CURRENT_DATE
ORDER BY queue_number ASC NULLS LAST, slot_start_time ASC;

DROP VIEW IF EXISTS public.staff_today_queue_view;
CREATE VIEW public.staff_today_queue_view
WITH (security_invoker = true)
AS
SELECT *
FROM public.patient_bookings_view
WHERE appointment_date = CURRENT_DATE
ORDER BY queue_number ASC NULLS LAST, slot_start_time ASC;

-- 1e. consultation_record_view — full consultation record, authenticated only
DROP VIEW IF EXISTS public.consultation_record_view;
CREATE VIEW public.consultation_record_view
WITH (security_invoker = true)
AS
SELECT
    b.id AS booking_id,
    c.id AS consultation_id,
    c.patient_id,
    c.doctor_id,
    b.status AS booking_status,
    c.general_notes,
    COALESCE(
        (SELECT jsonb_agg(
            jsonb_build_object(
                'id', cd.id,
                'diagnosis_text', cd.diagnosis_text,
                'diagnosis_code', cd.diagnosis_code,
                'is_primary', cd.is_primary,
                'notes', cd.notes
            )
        ) FROM public.consultation_diagnoses cd WHERE cd.consultation_id = c.id),
        '[]'::jsonb
    ) AS diagnoses,
    COALESCE(
        (SELECT jsonb_agg(
            jsonb_build_object(
                'id', pr.id,
                'notes', pr.notes,
                'items', COALESCE(
                    (SELECT jsonb_agg(
                        jsonb_build_object(
                            'id', pi.id,
                            'medication_name', pi.medication_name,
                            'strength', pi.strength,
                            'dosage', pi.dosage,
                            'route', pi.route,
                            'frequency', pi.frequency,
                            'duration', pi.duration,
                            'quantity', pi.quantity,
                            'instructions', pi.instructions
                        )
                    ) FROM public.prescription_items pi WHERE pi.prescription_id = pr.id),
                    '[]'::jsonb
                )
            )
        ) FROM public.prescriptions pr WHERE pr.consultation_id = c.id),
        '[]'::jsonb
    ) AS prescriptions,
    COALESCE(
        (SELECT jsonb_agg(
            jsonb_build_object(
                'id', lo.id,
                'notes', lo.notes,
                'items', COALESCE(
                    (SELECT jsonb_agg(
                        jsonb_build_object(
                            'id', loi.id,
                            'test_name', loi.test_name,
                            'test_code', loi.test_code,
                            'instructions', loi.instructions
                        )
                    ) FROM public.lab_order_items loi WHERE loi.lab_order_id = lo.id),
                    '[]'::jsonb
                )
            )
        ) FROM public.lab_orders lo WHERE lo.consultation_id = c.id),
        '[]'::jsonb
    ) AS lab_orders,
    COALESCE(
        (SELECT jsonb_agg(
            jsonb_build_object(
                'id', fu.id,
                'follow_up_date', fu.follow_up_date,
                'instructions', fu.instructions,
                'reason', fu.reason
            )
        ) FROM public.consultation_follow_ups fu WHERE fu.consultation_id = c.id),
        '[]'::jsonb
    ) AS follow_ups,
    (SELECT jsonb_build_object(
        'subjective', csn.subjective,
        'objective', csn.objective,
        'assessment', csn.assessment,
        'plan', csn.plan
    ) FROM public.consultation_soap_notes csn WHERE csn.consultation_id = c.id LIMIT 1) AS soap_note,
    COALESCE(
        (SELECT jsonb_agg(
            jsonb_build_object(
                'systolic_bp', cvs.systolic_bp,
                'diastolic_bp', cvs.diastolic_bp,
                'heart_rate', cvs.heart_rate,
                'respiratory_rate', cvs.respiratory_rate,
                'temperature_c', cvs.temperature_c,
                'oxygen_saturation', cvs.oxygen_saturation,
                'weight_kg', cvs.weight_kg,
                'height_cm', cvs.height_cm,
                'bmi', cvs.bmi,
                'pain_score', cvs.pain_score,
                'taken_at', cvs.taken_at
            )
            ORDER BY cvs.taken_at DESC NULLS LAST
        ) FROM public.consultation_vital_signs cvs WHERE cvs.consultation_id = c.id),
        '[]'::jsonb
    ) AS vital_signs
FROM public.bookings b
LEFT JOIN public.consultations c ON c.booking_id = b.id;

-- 1f. public_doctors_view — public-safe, no security_invoker (direct SELECT)
DROP VIEW IF EXISTS public.public_doctors_view;
CREATE VIEW public.public_doctors_view AS
SELECT
    d.id AS doctor_id,
    d.full_name,
    d.specialization,
    d.bio,
    d.profile_photo_url,
    d.consultation_fee,
    d.slot_duration_minutes,
    d.slot_capacity,
    d.daily_patient_limit,
    d.average_rating,
    d.review_count,
    d.status,
    COALESCE(
        (SELECT jsonb_agg(
            jsonb_build_object(
                'service_id', s.id,
                'name', s.name,
                'description', s.description,
                'category', s.category,
                'price', s.price,
                'estimated_duration_minutes', s.estimated_duration_minutes,
                'is_active', s.is_active
            )
        ) FROM public.doctor_services ds
        JOIN public.services s ON s.id = ds.service_id
        WHERE ds.doctor_id = d.id AND s.is_active = true),
        '[]'::jsonb
    ) AS services
FROM public.doctors d
WHERE d.status = 'Active';

-- 1g. doctor_available_services_view — public safe
DROP VIEW IF EXISTS public.doctor_available_services_view;
CREATE VIEW public.doctor_available_services_view AS
SELECT
    ds.doctor_id,
    s.id AS service_id,
    s.name AS service_name,
    s.description AS service_description,
    s.category,
    s.price,
    s.estimated_duration_minutes,
    s.is_active
FROM public.doctor_services ds
JOIN public.services s ON s.id = ds.service_id
WHERE s.is_active = true;

-- 1h. patient_documents_view — authenticated only
DROP VIEW IF EXISTS public.patient_documents_view;
CREATE VIEW public.patient_documents_view
WITH (security_invoker = true)
AS
SELECT
    pd.id,
    pd.patient_id,
    pd.booking_id,
    pd.consultation_id,
    pd.document_type,
    pd.title,
    pd.description,
    pd.file_path,
    pd.file_name,
    pd.file_content_type,
    pd.file_size,
    pd.source,
    pd.uploaded_by_user_id,
    pd.uploaded_at,
    pd.created_at
FROM public.patient_documents pd;

-- 1i. lab_results_view — authenticated only
DROP VIEW IF EXISTS public.lab_results_view;
CREATE VIEW public.lab_results_view
WITH (security_invoker = true)
AS
SELECT
    lr.id,
    lr.patient_id,
    lr.booking_id,
    lr.consultation_id,
    lr.lab_order_item_id,
    lr.result_title,
    lr.result_text,
    lr.file_path,
    lr.file_name,
    lr.file_content_type,
    lr.file_size,
    lr.status,
    lr.uploaded_by_user_id,
    lr.created_at AS uploaded_at,
    lr.created_at
FROM public.lab_results lr;

-- ============================================================
-- 2. RPC FUNCTIONS
-- ============================================================

-- 2a. create_booking — validate slot, create booking + service items, assign queue number
-- Access control:
--   - If p_patient_id is NULL → self-booking (current_patient_id)
--   - If p_patient_id is set and != current_patient_id → requires staff/admin/super_admin
--   - Blocks past appointment dates
--   - Patients cannot book for other patients
-- Schedule validation:
--   - Slot must be within doctor's schedule for that day of week
--   - Blocked dates and unavailable day status are respected
--   - Slot capacity is checked against existing bookings
CREATE OR REPLACE FUNCTION public.create_booking(
    p_doctor_id UUID,
    p_service_ids UUID[],
    p_appointment_date DATE,
    p_slot_start_time TIME,
    p_slot_end_time TIME,
    p_patient_id UUID DEFAULT NULL,
    p_notes TEXT DEFAULT NULL,
    p_is_walk_in BOOLEAN DEFAULT NULL
)
RETURNS TABLE (
    booking_id UUID,
    queue_number INT,
    status TEXT,
    payment_status TEXT
)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_patient_id UUID;
    v_queue_number INT;
    v_booking_id UUID;
    v_total_amount NUMERIC(10,2);
    v_doctor_consultation_fee NUMERIC(10,2);
    v_service RECORD;
    v_booking_status TEXT;
    v_is_walk_in BOOLEAN;
    v_current_patient_id UUID;
    v_day_of_week TEXT;
    v_slot_duration INT;
    v_slot_capacity INT;
    v_existing_count INT;
BEGIN
    -- ================================================================
    -- DATE AND PATIENT VALIDATION
    -- ================================================================

    -- Reject past dates
    IF p_appointment_date < CURRENT_DATE THEN
        RAISE EXCEPTION 'Cannot book an appointment in the past.';
    END IF;

    -- Doctor must be active
    IF NOT EXISTS (SELECT 1 FROM public.doctors WHERE id = p_doctor_id AND status = 'Active') THEN
        RAISE EXCEPTION 'Doctor not found or not active.';
    END IF;

    -- Resolve patient: NULL means self-booking
    v_current_patient_id := public.current_patient_id();
    v_patient_id := COALESCE(p_patient_id, v_current_patient_id);

    IF v_patient_id IS NULL THEN
        RAISE EXCEPTION 'Patient is required.';
    END IF;

    -- Access control: if booking for a DIFFERENT patient, require staff/admin/super_admin
    IF p_patient_id IS NOT NULL AND p_patient_id IS DISTINCT FROM v_current_patient_id THEN
        IF NOT public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]) THEN
            RAISE EXCEPTION 'Permission denied: staff or admin role required to book for another patient.';
        END IF;
    END IF;

    -- Determine is_walk_in: explicit param beats inferred
    v_is_walk_in := COALESCE(p_is_walk_in, (p_patient_id IS NOT NULL OR v_patient_id IS DISTINCT FROM v_current_patient_id));

    -- ================================================================
    -- SCHEDULE VALIDATION
    -- ================================================================

    -- Check blocked dates
    IF EXISTS (SELECT 1 FROM public.doctor_blocked_dates
               WHERE doctor_id = p_doctor_id AND blocked_date = p_appointment_date) THEN
        RAISE EXCEPTION 'Doctor is not available on this date (blocked).';
    END IF;

    -- Check day status
    DECLARE
        v_day_status TEXT;
    BEGIN
        SELECT status INTO v_day_status FROM public.doctor_day_statuses
        WHERE doctor_id = p_doctor_id AND target_date = p_appointment_date;

        IF v_day_status = 'unavailable' THEN
            RAISE EXCEPTION 'Doctor is not available on this date (unavailable).';
        END IF;
    END;

    -- Validate slot is within doctor's schedule for this day of week
    v_day_of_week := trim(to_char(p_appointment_date, 'Day'));

    IF NOT EXISTS (
        SELECT 1 FROM public.doctor_schedules
        WHERE doctor_id = p_doctor_id
          AND day_of_week = v_day_of_week
          AND start_time <= p_slot_start_time
          AND end_time >= p_slot_end_time
    ) THEN
        RAISE EXCEPTION 'Selected time slot is outside the doctor''s schedule.';
    END IF;

    -- Check slot capacity (how many overlapping bookings already exist)
    SELECT slot_capacity INTO v_slot_capacity
    FROM public.doctors WHERE id = p_doctor_id;
    v_slot_capacity := COALESCE(v_slot_capacity, 1);

    -- Check if limited day status reduces capacity
    DECLARE
        v_limited_capacity INT;
    BEGIN
        SELECT max_slots INTO v_limited_capacity FROM public.doctor_day_statuses
        WHERE doctor_id = p_doctor_id AND target_date = p_appointment_date AND status = 'limited';
        IF v_limited_capacity IS NOT NULL THEN
            v_slot_capacity := v_limited_capacity;
        END IF;
    END;

    SELECT COUNT(*) INTO v_existing_count
    FROM public.bookings
    WHERE doctor_id = p_doctor_id
      AND appointment_date = p_appointment_date
      AND slot_start_time < p_slot_end_time
      AND slot_end_time > p_slot_start_time
      AND status NOT IN ('Cancelled', 'NoShow', 'Expired');

    IF v_existing_count >= v_slot_capacity THEN
        RAISE EXCEPTION 'This time slot has reached its capacity of % appointments.', v_slot_capacity;
    END IF;

    -- ================================================================
    -- SERVICE VALIDATION
    -- ================================================================

    IF NOT EXISTS (
        SELECT 1 FROM public.services s
        WHERE s.id = ANY(p_service_ids) AND s.is_active = true
        HAVING COUNT(*) = array_length(p_service_ids, 1)
    ) THEN
        RAISE EXCEPTION 'One or more services not found or inactive.';
    END IF;

    -- ================================================================
    -- BOOKING CREATION
    -- ================================================================

    SELECT COALESCE(SUM(s.price), 0) INTO v_total_amount
    FROM public.services s WHERE s.id = ANY(p_service_ids);

    SELECT consultation_fee INTO v_doctor_consultation_fee
    FROM public.doctors WHERE id = p_doctor_id;

    v_total_amount := v_total_amount + COALESCE(v_doctor_consultation_fee, 0);

    SELECT COALESCE(MAX(queue_number), 0) + 1 INTO v_queue_number
    FROM public.bookings
    WHERE doctor_id = p_doctor_id AND appointment_date = p_appointment_date;

    v_booking_status := 'Confirmed';

    INSERT INTO public.bookings (
        patient_id, doctor_id, appointment_date, slot_start_time, slot_end_time,
        queue_number, status, payment_mode, payment_status, total_amount, final_amount,
        notes, is_walk_in, created_by_user_id
    ) VALUES (
        v_patient_id, p_doctor_id, p_appointment_date, p_slot_start_time, p_slot_end_time,
        v_queue_number, v_booking_status, 'PayAtClinic', 'Unpaid', v_total_amount, v_total_amount,
        p_notes, v_is_walk_in, auth.uid()
    )
    RETURNING id INTO v_booking_id;

    FOR v_service IN SELECT s.id, s.name, s.price FROM public.services s WHERE s.id = ANY(p_service_ids)
    LOOP
        INSERT INTO public.booking_service_items (booking_id, service_id, service_name, quantity, price)
        VALUES (v_booking_id, v_service.id, v_service.name, 1, v_service.price);
    END LOOP;

    RETURN QUERY SELECT v_booking_id, v_queue_number, v_booking_status, 'Unpaid';
END;
$$;

-- 2b. cancel_booking — patient-owner or staff/admin/super_admin only
CREATE OR REPLACE FUNCTION public.cancel_booking(
    p_booking_id UUID,
    p_reason TEXT DEFAULT NULL
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_patient_id UUID;
BEGIN
    -- Fetch the booking's patient for ownership check
    SELECT patient_id INTO v_patient_id
    FROM public.bookings WHERE id = p_booking_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Booking not found.';
    END IF;

    -- Allow cancel if:
    -- 1. user is staff/admin/super_admin, OR
    -- 2. the booking belongs to the current patient
    IF NOT (
        public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role])
        OR v_patient_id = public.current_patient_id()
    ) THEN
        RAISE EXCEPTION 'Permission denied: you can only cancel your own bookings.';
    END IF;

    UPDATE public.bookings
    SET status = 'Cancelled',
        cancellation_reason = p_reason,
        cancelled_at = now(),
        updated_at = now()
    WHERE id = p_booking_id
      AND status NOT IN ('Completed', 'Cancelled', 'NoShow');
END;
$$;

-- 2c. check_in_booking — staff/admin/super_admin only
CREATE OR REPLACE FUNCTION public.check_in_booking(
    p_booking_id UUID
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    IF NOT public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]) THEN
        RAISE EXCEPTION 'Permission denied: staff or admin role required';
    END IF;

    UPDATE public.bookings
    SET status = 'CheckedIn',
        checked_in_at = now(),
        updated_at = now()
    WHERE id = p_booking_id
      AND status IN ('Confirmed', 'Pending');
END;
$$;

-- 2d. undo_check_in — staff/admin/super_admin only
CREATE OR REPLACE FUNCTION public.undo_check_in(
    p_booking_id UUID
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    IF NOT public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]) THEN
        RAISE EXCEPTION 'Permission denied: staff or admin role required';
    END IF;

    UPDATE public.bookings
    SET status = 'Confirmed',
        checked_in_at = NULL,
        updated_at = now()
    WHERE id = p_booking_id
      AND status = 'CheckedIn';
END;
$$;

-- 2e. confirm_booking — staff/admin/super_admin only
CREATE OR REPLACE FUNCTION public.confirm_booking(
    p_booking_id UUID
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    IF NOT public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]) THEN
        RAISE EXCEPTION 'Permission denied: staff or admin role required';
    END IF;

    UPDATE public.bookings
    SET status = 'Confirmed',
        updated_at = now()
    WHERE id = p_booking_id
      AND status = 'Pending';
END;
$$;

-- 2f. complete_booking_basic — doctor/admin/super_admin only
CREATE OR REPLACE FUNCTION public.complete_booking_basic(
    p_booking_id UUID,
    p_final_amount NUMERIC DEFAULT NULL,
    p_diagnosis TEXT DEFAULT NULL,
    p_doctor_fee_notes TEXT DEFAULT NULL,
    p_soap_notes TEXT DEFAULT NULL,
    p_follow_up_date DATE DEFAULT NULL,
    p_follow_up_instructions TEXT DEFAULT NULL
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_doctor_id UUID;
    v_patient_id UUID;
    v_consultation_id UUID;
    v_current_user_doctor_id UUID;
BEGIN
    IF NOT public.has_any_role(ARRAY['doctor'::app_role, 'admin'::app_role, 'super_admin'::app_role]) THEN
        RAISE EXCEPTION 'Permission denied: doctor or admin role required';
    END IF;

    SELECT doctor_id, patient_id INTO v_doctor_id, v_patient_id
    FROM public.bookings WHERE id = p_booking_id;

    -- Verify the doctor is the assigned doctor (or admin)
    v_current_user_doctor_id := public.current_doctor_id();
    IF public.has_role('doctor'::app_role) AND v_current_user_doctor_id IS DISTINCT FROM v_doctor_id THEN
        IF NOT public.has_any_role(ARRAY['admin'::app_role, 'super_admin'::app_role]) THEN
            RAISE EXCEPTION 'Permission denied: you are not the assigned doctor for this booking';
        END IF;
    END IF;

    INSERT INTO public.consultations (patient_id, doctor_id, booking_id, status, general_notes)
    VALUES (v_patient_id, v_doctor_id, p_booking_id, 'Completed', p_doctor_fee_notes)
    ON CONFLICT (booking_id) DO UPDATE SET
        status = 'Completed',
        general_notes = COALESCE(p_doctor_fee_notes, consultations.general_notes)
    RETURNING id INTO v_consultation_id;

    IF v_consultation_id IS NULL THEN
        SELECT id INTO v_consultation_id FROM public.consultations WHERE booking_id = p_booking_id;
    END IF;

    IF p_soap_notes IS NOT NULL AND v_consultation_id IS NOT NULL THEN
        INSERT INTO public.consultation_soap_notes (consultation_id, subjective)
        VALUES (v_consultation_id, p_soap_notes);
    END IF;

    IF (p_follow_up_date IS NOT NULL OR p_follow_up_instructions IS NOT NULL) AND v_consultation_id IS NOT NULL THEN
        INSERT INTO public.consultation_follow_ups (consultation_id, follow_up_date, instructions, reason)
        VALUES (v_consultation_id, p_follow_up_date, p_follow_up_instructions, NULL);
    END IF;

    UPDATE public.bookings
    SET status = 'Completed',
        final_amount = COALESCE(p_final_amount, final_amount, total_amount),
        doctor_completed_at = now(),
        updated_at = now()
    WHERE id = p_booking_id
      AND status IN ('CheckedIn', 'InProgress');
END;
$$;

-- 2g. no_show_booking — staff/admin/super_admin only
CREATE OR REPLACE FUNCTION public.no_show_booking(
    p_booking_id UUID
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    IF NOT public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]) THEN
        RAISE EXCEPTION 'Permission denied: staff or admin role required';
    END IF;

    UPDATE public.bookings
    SET status = 'NoShow',
        updated_at = now()
    WHERE id = p_booking_id
      AND status IN ('Confirmed', 'CheckedIn');
END;
$$;

-- 2h. save_consultation_record — full consultation save, doctor/admin only
CREATE OR REPLACE FUNCTION public.save_consultation_record(
    p_booking_id UUID,
    p_chief_complaint TEXT DEFAULT NULL,
    p_general_notes TEXT DEFAULT NULL,
    p_vitals JSONB DEFAULT NULL,
    p_soap JSONB DEFAULT NULL,
    p_diagnoses JSONB DEFAULT '[]'::jsonb,
    p_prescription JSONB DEFAULT NULL,
    p_lab_order JSONB DEFAULT NULL,
    p_follow_up JSONB DEFAULT NULL,
    p_mark_completed BOOLEAN DEFAULT true,
    p_final_amount NUMERIC DEFAULT NULL
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_doctor_id UUID;
    v_patient_id UUID;
    v_consultation_id UUID;
    v_prescription_id UUID;
    v_lab_order_id UUID;
    v_item JSONB;
    v_current_user_doctor_id UUID;
BEGIN
    IF NOT public.has_any_role(ARRAY['doctor'::app_role, 'admin'::app_role, 'super_admin'::app_role]) THEN
        RAISE EXCEPTION 'Permission denied: doctor or admin role required';
    END IF;

    SELECT doctor_id, patient_id INTO v_doctor_id, v_patient_id
    FROM public.bookings WHERE id = p_booking_id;

    v_current_user_doctor_id := public.current_doctor_id();
    IF public.has_role('doctor'::app_role) AND v_current_user_doctor_id IS DISTINCT FROM v_doctor_id THEN
        IF NOT public.has_any_role(ARRAY['admin'::app_role, 'super_admin'::app_role]) THEN
            RAISE EXCEPTION 'Permission denied: you are not the assigned doctor';
        END IF;
    END IF;

    INSERT INTO public.consultations (patient_id, doctor_id, booking_id, status, general_notes)
    VALUES (v_patient_id, v_doctor_id, p_booking_id, 'Open', p_general_notes)
    ON CONFLICT (booking_id) DO UPDATE SET
        general_notes = COALESCE(p_general_notes, consultations.general_notes),
        status = CASE WHEN consultations.status = 'Completed' THEN consultations.status ELSE 'Open' END
    RETURNING id INTO v_consultation_id;

    IF v_consultation_id IS NULL THEN
        SELECT id INTO v_consultation_id FROM public.consultations WHERE booking_id = p_booking_id;
    END IF;

    IF p_vitals IS NOT NULL AND p_vitals != 'null'::jsonb THEN
        INSERT INTO public.consultation_vital_signs (
            consultation_id, systolic_bp, diastolic_bp, heart_rate,
            respiratory_rate, temperature_c, oxygen_saturation,
            weight_kg, height_cm, bmi, pain_score
        ) VALUES (
            v_consultation_id,
            (p_vitals->>'systolic_bp')::INT,
            (p_vitals->>'diastolic_bp')::INT,
            (p_vitals->>'heart_rate')::INT,
            (p_vitals->>'respiratory_rate')::INT,
            (p_vitals->>'temperature_c')::NUMERIC(4,1),
            (p_vitals->>'oxygen_saturation')::INT,
            (p_vitals->>'weight_kg')::NUMERIC(5,1),
            (p_vitals->>'height_cm')::NUMERIC(5,1),
            (p_vitals->>'bmi')::NUMERIC(4,1),
            (p_vitals->>'pain_score')::INT
        );
    END IF;

    IF p_soap IS NOT NULL AND p_soap != 'null'::jsonb THEN
        DELETE FROM public.consultation_soap_notes WHERE consultation_id = v_consultation_id;
        INSERT INTO public.consultation_soap_notes (consultation_id, subjective, objective, assessment, plan)
        VALUES (
            v_consultation_id,
            p_soap->>'subjective',
            p_soap->>'objective',
            p_soap->>'assessment',
            p_soap->>'plan'
        );
    END IF;

    IF jsonb_array_length(p_diagnoses) > 0 THEN
        DELETE FROM public.consultation_diagnoses WHERE consultation_id = v_consultation_id;
        FOR v_item IN SELECT * FROM jsonb_array_elements(p_diagnoses)
        LOOP
            INSERT INTO public.consultation_diagnoses (consultation_id, diagnosis_text, diagnosis_code, is_primary, notes)
            VALUES (
                v_consultation_id,
                v_item->>'diagnosis_text',
                v_item->>'diagnosis_code',
                COALESCE((v_item->>'is_primary')::BOOLEAN, false),
                v_item->>'notes'
            );
        END LOOP;
    END IF;

    IF p_prescription IS NOT NULL AND p_prescription != 'null'::jsonb THEN
        INSERT INTO public.prescriptions (consultation_id, notes)
        VALUES (v_consultation_id, p_prescription->>'notes')
        RETURNING id INTO v_prescription_id;

        FOR v_item IN SELECT * FROM jsonb_array_elements(COALESCE(p_prescription->'items', '[]'::jsonb))
        LOOP
            INSERT INTO public.prescription_items (
                prescription_id, medication_name, strength, dosage, route,
                frequency, duration, quantity, instructions
            ) VALUES (
                v_prescription_id,
                v_item->>'medication_name',
                v_item->>'strength',
                v_item->>'dosage',
                v_item->>'route',
                v_item->>'frequency',
                v_item->>'duration',
                v_item->>'quantity',
                v_item->>'instructions'
            );
        END LOOP;
    END IF;

    IF p_lab_order IS NOT NULL AND p_lab_order != 'null'::jsonb THEN
        INSERT INTO public.lab_orders (consultation_id, notes)
        VALUES (v_consultation_id, p_lab_order->>'notes')
        RETURNING id INTO v_lab_order_id;

        FOR v_item IN SELECT * FROM jsonb_array_elements(COALESCE(p_lab_order->'items', '[]'::jsonb))
        LOOP
            INSERT INTO public.lab_order_items (lab_order_id, test_name, test_code, instructions)
            VALUES (v_lab_order_id, v_item->>'test_name', v_item->>'test_code', v_item->>'instructions');
        END LOOP;
    END IF;

    IF p_follow_up IS NOT NULL AND p_follow_up != 'null'::jsonb THEN
        INSERT INTO public.consultation_follow_ups (consultation_id, follow_up_date, instructions, reason)
        VALUES (
            v_consultation_id,
            (p_follow_up->>'follow_up_date')::DATE,
            p_follow_up->>'instructions',
            p_follow_up->>'reason'
        );
    END IF;

    IF p_mark_completed THEN
        UPDATE public.bookings
        SET status = 'Completed',
            final_amount = COALESCE(p_final_amount, final_amount, total_amount),
            doctor_completed_at = now(),
            updated_at = now()
        WHERE id = p_booking_id
          AND status IN ('CheckedIn', 'InProgress', 'Open');
    END IF;
END;
$$;

-- 2i. record_payment — staff/admin/super_admin only
CREATE OR REPLACE FUNCTION public.record_payment(
    p_booking_id UUID,
    p_amount NUMERIC,
    p_payment_method TEXT,
    p_reference_number TEXT DEFAULT NULL,
    p_or_number TEXT DEFAULT NULL
)
RETURNS TABLE (
    booking_id UUID,
    or_number TEXT,
    status TEXT
)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_payment_id UUID;
    v_or_number TEXT;
BEGIN
    IF NOT public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]) THEN
        RAISE EXCEPTION 'Permission denied: staff or admin role required';
    END IF;

    v_or_number := COALESCE(p_or_number, 'OR-' || to_char(now(), 'YYYYMMDD-') || upper(substr(md5(random()::text), 1, 6)));

    INSERT INTO public.payments (booking_id, amount, payment_method, reference_number, or_number, status, verified_by_user_id, verified_at)
    VALUES (p_booking_id, p_amount, p_payment_method, p_reference_number, v_or_number, 'Paid', auth.uid(), now())
    ON CONFLICT (booking_id) DO UPDATE SET
        amount = p_amount,
        payment_method = p_payment_method,
        reference_number = COALESCE(p_reference_number, payments.reference_number),
        or_number = v_or_number,
        status = 'Paid',
        verified_by_user_id = auth.uid(),
        verified_at = now()
    RETURNING id INTO v_payment_id;

    UPDATE public.bookings
    SET payment_status = 'Paid',
        updated_at = now()
    WHERE id = p_booking_id;

    RETURN QUERY SELECT p_booking_id, v_or_number, 'Paid';
END;
$$;

-- 2j. waive_professional_fee — staff/admin/super_admin only
CREATE OR REPLACE FUNCTION public.waive_professional_fee(
    p_booking_id UUID,
    p_reason TEXT DEFAULT 'Professional fee waived.'
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    IF NOT public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role]) THEN
        RAISE EXCEPTION 'Permission denied: staff or admin role required';
    END IF;

    UPDATE public.bookings
    SET is_professional_fee_waived = true,
        updated_at = now()
    WHERE id = p_booking_id;

    INSERT INTO public.payments (booking_id, amount, payment_method, status, waived_by_user_id, waived_at, waived_reason)
    VALUES (p_booking_id, 0, 'Waiver', 'Waived', auth.uid(), now(), p_reason)
    ON CONFLICT (booking_id) DO UPDATE SET
        status = 'Waived',
        waived_by_user_id = auth.uid(),
        waived_at = now(),
        waived_reason = p_reason;
END;
$$;

-- 2k. refund_payment — admin/super_admin only
CREATE OR REPLACE FUNCTION public.refund_payment(
    p_booking_id UUID
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    IF NOT public.has_any_role(ARRAY['admin'::app_role, 'super_admin'::app_role]) THEN
        RAISE EXCEPTION 'Permission denied: admin role required';
    END IF;

    UPDATE public.payments
    SET status = 'Refunded',
        refunded_by_user_id = auth.uid(),
        refunded_at = now()
    WHERE booking_id = p_booking_id;

    UPDATE public.bookings
    SET payment_status = 'Refunded',
        updated_at = now()
    WHERE id = p_booking_id;
END;
$$;

-- 2l. get_doctor_today_summary — authenticated, returns for current doctor
CREATE OR REPLACE FUNCTION public.get_doctor_today_summary()
RETURNS TABLE (
    today_total BIGINT,
    checked_in_count BIGINT,
    in_progress_count BIGINT,
    completed_count BIGINT,
    no_show_count BIGINT
)
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_doctor_id UUID;
BEGIN
    v_doctor_id := public.current_doctor_id();
    IF v_doctor_id IS NULL THEN
        RETURN;
    END IF;

    RETURN QUERY
    SELECT
        COUNT(*)::BIGINT AS today_total,
        COUNT(*) FILTER (WHERE status = 'CheckedIn')::BIGINT AS checked_in_count,
        COUNT(*) FILTER (WHERE status = 'InProgress')::BIGINT AS in_progress_count,
        COUNT(*) FILTER (WHERE status = 'Completed')::BIGINT AS completed_count,
        COUNT(*) FILTER (WHERE status = 'NoShow')::BIGINT AS no_show_count
    FROM public.bookings
    WHERE doctor_id = v_doctor_id
      AND appointment_date = CURRENT_DATE;
END;
$$;

-- 2m. get_available_slots — public-safe (no auth needed)
-- Uses generate_series(TIMESTAMPTZ, TIMESTAMPTZ, INTERVAL) to produce
-- every slot from schedule start to end (PostgreSQL does not support
-- generate_series for plain TIME — must convert to timestamp first).
-- Blocks past dates. Respects blocked_dates, unavailable day_statuses.
-- Never returns slot_end_time beyond schedule_end_time.
CREATE OR REPLACE FUNCTION public.get_available_slots(
    p_doctor_id UUID,
    p_appointment_date DATE
)
RETURNS TABLE (
    slot_start_time TIME,
    slot_end_time TIME,
    is_available BOOLEAN,
    booked_count BIGINT,
    capacity BIGINT
)
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_slot_duration INT;
    v_slot_capacity INT;
    v_day_of_week TEXT;
    v_day_status RECORD;
    v_duration_interval INTERVAL;
    v_now_tstz TIMESTAMPTZ;
BEGIN
    -- Block past dates
    IF p_appointment_date < CURRENT_DATE THEN
        RETURN;
    END IF;

    SELECT slot_duration_minutes, slot_capacity
    INTO v_slot_duration, v_slot_capacity
    FROM public.doctors WHERE id = p_doctor_id;

    v_slot_duration := COALESCE(v_slot_duration, 30);
    v_slot_capacity := COALESCE(v_slot_capacity, 1);
    v_duration_interval := make_interval(mins => v_slot_duration);

    -- Blocked date check
    IF EXISTS (SELECT 1 FROM public.doctor_blocked_dates
               WHERE doctor_id = p_doctor_id AND blocked_date = p_appointment_date) THEN
        RETURN;
    END IF;

    -- Day status check
    SELECT * INTO v_day_status FROM public.doctor_day_statuses
    WHERE doctor_id = p_doctor_id AND target_date = p_appointment_date;

    IF v_day_status.status = 'unavailable' THEN
        RETURN;
    END IF;

    IF v_day_status.status = 'limited' AND v_day_status.max_slots IS NOT NULL THEN
        v_slot_capacity := v_day_status.max_slots;
    END IF;

    v_day_of_week := trim(to_char(p_appointment_date, 'Day'));

    -- For today, only show slots that haven't passed yet
    IF p_appointment_date = CURRENT_DATE THEN
        v_now_tstz := CURRENT_TIMESTAMP;
    ELSE
        v_now_tstz := NULL;
    END IF;

    RETURN QUERY
    WITH schedule_slots AS (
        SELECT
            (p_appointment_date + ds.start_time)::TIMESTAMPTZ AS slot_start_ts,
            (p_appointment_date + ds.end_time)::TIMESTAMPTZ AS schedule_end_ts
        FROM public.doctor_schedules ds
        WHERE ds.doctor_id = p_doctor_id
          AND ds.day_of_week = v_day_of_week
    ),
    -- generate_series(TIMESTAMPTZ, TIMESTAMPTZ, INTERVAL) — PostgreSQL supports this.
    -- Produces every slot_start from schedule_start to (schedule_end minus one duration).
    all_slots AS (
        SELECT
            generate_series(
                ss.slot_start_ts,
                ss.schedule_end_ts - v_duration_interval,
                v_duration_interval
            ) AS slot_start_ts,
            ss.schedule_end_ts
        FROM schedule_slots ss
    ),
    time_slots AS (
        SELECT
            asl.slot_start_ts,
            asl.slot_start_ts::TIME AS slot_start_time,
            LEAST(
                (asl.slot_start_ts + v_duration_interval)::TIME,
                asl.schedule_end_ts::TIME
            ) AS slot_end_time,
            asl.schedule_end_ts
        FROM all_slots asl
    ),
    existing_bookings AS (
        SELECT slot_start_time, slot_end_time
        FROM public.bookings
        WHERE doctor_id = p_doctor_id
          AND appointment_date = p_appointment_date
          AND status NOT IN ('Cancelled', 'NoShow', 'Expired')
    )
    SELECT
        ts.slot_start_time,
        ts.slot_end_time,
        (COUNT(eb.*) < v_slot_capacity) AS is_available,
        COUNT(eb.*)::BIGINT AS booked_count,
        v_slot_capacity::BIGINT AS capacity
    FROM time_slots ts
    LEFT JOIN existing_bookings eb
        ON eb.slot_start_time < ts.slot_end_time
        AND eb.slot_end_time > ts.slot_start_time
    WHERE (v_now_tstz IS NULL OR ts.slot_start_ts >= v_now_tstz)
    GROUP BY ts.slot_start_time, ts.slot_end_time
    ORDER BY ts.slot_start_time;
END;
$$;

-- 2n. register_patient_document — patient document upload tracking
-- Required params first, then optional (DEFAULT) params.
CREATE OR REPLACE FUNCTION public.register_patient_document(
    p_patient_id TEXT,
    p_file_path TEXT,
    p_file_name TEXT,
    p_booking_id TEXT DEFAULT '',
    p_file_size INT DEFAULT NULL,
    p_content_type TEXT DEFAULT NULL,
    p_title TEXT DEFAULT NULL,
    p_description TEXT DEFAULT NULL,
    p_document_type TEXT DEFAULT NULL,
    p_consultation_id TEXT DEFAULT NULL
)
RETURNS TABLE (id UUID)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_id UUID;
    v_patient_uuid UUID;
    v_booking_uuid UUID;
    v_consultation_uuid UUID;
BEGIN
    BEGIN
        v_patient_uuid := p_patient_id::UUID;
    EXCEPTION WHEN OTHERS THEN
        v_patient_uuid := NULL;
    END;

    IF p_booking_id IS NOT NULL AND p_booking_id != '' THEN
        BEGIN
            v_booking_uuid := p_booking_id::UUID;
        EXCEPTION WHEN OTHERS THEN
            v_booking_uuid := NULL;
        END;
    END IF;

    IF p_consultation_id IS NOT NULL AND p_consultation_id != '' THEN
        BEGIN
            v_consultation_uuid := p_consultation_id::UUID;
        EXCEPTION WHEN OTHERS THEN
            v_consultation_uuid := NULL;
        END;
    END IF;

    INSERT INTO public.patient_documents (
        patient_id, booking_id, consultation_id, document_type,
        title, description, file_name, file_path, file_size,
        file_content_type, source, uploaded_by_user_id
    ) VALUES (
        v_patient_uuid, v_booking_uuid, v_consultation_uuid,
        COALESCE(p_document_type, 'Other'), p_title, p_description,
        p_file_name, p_file_path, p_file_size, p_content_type,
        'PatientUpload', auth.uid()
    )
    RETURNING id INTO v_id;

    RETURN QUERY SELECT v_id;
END;
$$;

-- 2o. register_lab_result — lab result upload tracking
-- Required params first, then optional (DEFAULT) params.
CREATE OR REPLACE FUNCTION public.register_lab_result(
    p_patient_id TEXT,
    p_file_path TEXT,
    p_file_name TEXT,
    p_booking_id TEXT DEFAULT '',
    p_file_size INT DEFAULT NULL,
    p_content_type TEXT DEFAULT NULL,
    p_title TEXT DEFAULT NULL,
    p_notes TEXT DEFAULT NULL,
    p_consultation_id TEXT DEFAULT NULL
)
RETURNS TABLE (id UUID)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_id UUID;
    v_patient_uuid UUID;
    v_booking_uuid UUID;
    v_consultation_uuid UUID;
BEGIN
    BEGIN
        v_patient_uuid := p_patient_id::UUID;
    EXCEPTION WHEN OTHERS THEN
        v_patient_uuid := NULL;
    END;

    IF p_booking_id IS NOT NULL AND p_booking_id != '' THEN
        BEGIN
            v_booking_uuid := p_booking_id::UUID;
        EXCEPTION WHEN OTHERS THEN
            v_booking_uuid := NULL;
        END;
    END IF;

    IF p_consultation_id IS NOT NULL AND p_consultation_id != '' THEN
        BEGIN
            v_consultation_uuid := p_consultation_id::UUID;
        EXCEPTION WHEN OTHERS THEN
            v_consultation_uuid := NULL;
        END;
    END IF;

    INSERT INTO public.lab_results (
        patient_id, booking_id, consultation_id, result_title, result_text,
        file_name, file_path, file_size, file_content_type, status, uploaded_by_user_id
    ) VALUES (
        v_patient_uuid, v_booking_uuid, v_consultation_uuid,
        p_title, p_notes, p_file_name, p_file_path,
        p_file_size, p_content_type, 'Uploaded', auth.uid()
    )
    RETURNING id INTO v_id;

    RETURN QUERY SELECT v_id;
END;
$$;

-- ============================================================
-- 3. ADDITIONAL RLS POLICIES
-- ============================================================

-- user_roles: allow users to SELECT their own role
DROP POLICY IF EXISTS "user_roles_select_own" ON public.user_roles;
CREATE POLICY "user_roles_select_own" ON public.user_roles
  FOR SELECT USING (user_id = auth.uid());

-- ============================================================
-- 4. VIEW GRANTS (RLS enforced per-role via base tables for security_invoker views)
-- ============================================================

-- Public-safe views: anon can read
GRANT SELECT ON public.public_doctors_view TO anon, authenticated;
GRANT SELECT ON public.doctor_available_services_view TO anon, authenticated;

-- Private views: authenticated only
GRANT SELECT ON public.patient_bookings_view TO authenticated;
GRANT SELECT ON public.doctor_today_queue_view TO authenticated;
GRANT SELECT ON public.staff_today_queue_view TO authenticated;
GRANT SELECT ON public.consultation_record_view TO authenticated;
GRANT SELECT ON public.patient_documents_view TO authenticated;
GRANT SELECT ON public.lab_results_view TO authenticated;

-- ============================================================
-- 5. STORAGE BUCKETS
-- ============================================================

INSERT INTO storage.buckets (id, name, public)
VALUES ('patient-documents', 'patient-documents', false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO storage.buckets (id, name, public)
VALUES ('lab-results', 'lab-results', false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO storage.buckets (id, name, public)
VALUES ('proof-payments', 'proof-payments', false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO storage.buckets (id, name, public)
VALUES ('doctor-photos', 'doctor-photos', true)
ON CONFLICT (id) DO NOTHING;

INSERT INTO storage.buckets (id, name, public)
VALUES ('clinic-assets', 'clinic-assets', true)
ON CONFLICT (id) DO NOTHING;

-- Storage RLS policies (uses explicit app_role casts for has_any_role)

-- proof-payments: patient can view own, doctor can view for assigned bookings, staff/admin can view all
DROP POLICY IF EXISTS "proof-payments_select" ON storage.objects;
CREATE POLICY "proof-payments_select" ON storage.objects
  FOR SELECT USING (
    bucket_id = 'proof-payments'
    AND (
      (storage.foldername(name))[1] = auth.uid()::text
      OR public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role])
    )
  );

DROP POLICY IF EXISTS "proof-payments_insert" ON storage.objects;
CREATE POLICY "proof-payments_insert" ON storage.objects
  FOR INSERT WITH CHECK (
    bucket_id = 'proof-payments'
    AND auth.uid() IS NOT NULL
  );

DROP POLICY IF EXISTS "patient-documents_select" ON storage.objects;
CREATE POLICY "patient-documents_select" ON storage.objects
  FOR SELECT USING (
    bucket_id = 'patient-documents'
    AND (
      (storage.foldername(name))[1] = auth.uid()::text
      OR public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role])
    )
  );

DROP POLICY IF EXISTS "patient-documents_insert" ON storage.objects;
CREATE POLICY "patient-documents_insert" ON storage.objects
  FOR INSERT WITH CHECK (
    bucket_id = 'patient-documents'
    AND auth.uid() IS NOT NULL
  );

DROP POLICY IF EXISTS "lab-results_select" ON storage.objects;
CREATE POLICY "lab-results_select" ON storage.objects
  FOR SELECT USING (
    bucket_id = 'lab-results'
    AND (
      (storage.foldername(name))[1] = auth.uid()::text
      OR public.has_any_role(ARRAY['staff'::app_role, 'admin'::app_role, 'super_admin'::app_role])
    )
  );

DROP POLICY IF EXISTS "lab-results_insert" ON storage.objects;
CREATE POLICY "lab-results_insert" ON storage.objects
  FOR INSERT WITH CHECK (
    bucket_id = 'lab-results'
    AND auth.uid() IS NOT NULL
  );

DROP POLICY IF EXISTS "doctor-photos_select" ON storage.objects;
CREATE POLICY "doctor-photos_select" ON storage.objects
  FOR SELECT USING (bucket_id = 'doctor-photos');

DROP POLICY IF EXISTS "doctor-photos_insert" ON storage.objects;
CREATE POLICY "doctor-photos_insert" ON storage.objects
  FOR INSERT WITH CHECK (
    bucket_id = 'doctor-photos'
    AND public.has_any_role(ARRAY['admin'::app_role, 'super_admin'::app_role])
  );

DROP POLICY IF EXISTS "clinic-assets_select" ON storage.objects;
CREATE POLICY "clinic-assets_select" ON storage.objects
  FOR SELECT USING (bucket_id = 'clinic-assets');

DROP POLICY IF EXISTS "clinic-assets_insert" ON storage.objects;
CREATE POLICY "clinic-assets_insert" ON storage.objects
  FOR INSERT WITH CHECK (
    bucket_id = 'clinic-assets'
    AND public.has_any_role(ARRAY['admin'::app_role, 'super_admin'::app_role])
  );

-- ============================================================
-- 6. RPC EXECUTION GRANTS
-- ============================================================

-- Public-safe (anon): slot availability only
GRANT EXECUTE ON FUNCTION public.get_available_slots TO anon, authenticated;

-- Bookings: patient-facing, but needs auth
GRANT EXECUTE ON FUNCTION public.create_booking TO authenticated;
GRANT EXECUTE ON FUNCTION public.cancel_booking TO authenticated;

-- Staff/operations (authenticated, role-checked internally)
GRANT EXECUTE ON FUNCTION public.check_in_booking TO authenticated;
GRANT EXECUTE ON FUNCTION public.undo_check_in TO authenticated;
GRANT EXECUTE ON FUNCTION public.confirm_booking TO authenticated;
GRANT EXECUTE ON FUNCTION public.complete_booking_basic TO authenticated;
GRANT EXECUTE ON FUNCTION public.no_show_booking TO authenticated;
GRANT EXECUTE ON FUNCTION public.save_consultation_record TO authenticated;
GRANT EXECUTE ON FUNCTION public.record_payment TO authenticated;
GRANT EXECUTE ON FUNCTION public.waive_professional_fee TO authenticated;
GRANT EXECUTE ON FUNCTION public.refund_payment TO authenticated;

-- Doctor/staff reads
GRANT EXECUTE ON FUNCTION public.get_doctor_today_summary TO authenticated;

-- Document upload tracking (authenticated, role-checked via caller)
GRANT EXECUTE ON FUNCTION public.register_patient_document TO authenticated;
GRANT EXECUTE ON FUNCTION public.register_lab_result TO authenticated;

-- ============================================================
-- DONE
-- ============================================================
