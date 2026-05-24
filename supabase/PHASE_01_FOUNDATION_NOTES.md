# Phase 1 Foundation — Notes

## What This SQL Creates

A safer, re-runnable foundation layer for the Clinic Booking Supabase backend. All CREATE statements use `IF NOT EXISTS` or `OR REPLACE`. Policies are dropped before recreating. Enums use `DO $$ ... EXCEPTION WHEN duplicate_object` blocks.

## Tables Created (10 tables)

| Table | Purpose |
|---|---|
| `profiles` | Links `auth.users` to app-level user data (name, avatar, email) |
| `user_roles` | Custom RBAC — maps auth users to roles (patient, doctor, staff, admin, super_admin) |
| `patients` | Patient demographics and medical profile |
| `doctors` | Doctor professional profile and practice config |
| `services` | Clinic service catalog with categories and pricing |
| `doctor_services` | Many-to-many linking doctors to services |
| `doctor_schedules` | Weekly recurring time slots per doctor |
| `doctor_blocked_dates` | Date-specific blocks (vacation, holiday) |
| `doctor_day_statuses` | Per-day status override (available, limited, unavailable) |
| `clinic_settings` | Single-row clinic configuration (branding, payments, policies) |

## Enums Created (10 enums)

| Enum | Values |
|---|---|
| `app_role` | patient, doctor, staff, admin, super_admin |
| `doctor_status` | Active, Inactive, OnLeave |
| `doctor_day_status_type` | available, limited, unavailable |
| `appointment_day` | Monday through Sunday |
| `service_category` | Consultation, Procedure, Laboratory, Diagnostic |
| `booking_status` | Pending, ProofSubmitted, Confirmed, CheckedIn, InProgress, OnHold, Cancelled, Completed, Expired, NoShow, Rescheduled |
| `payment_mode` | Online, PayAtClinic |
| `payment_status` | Unpaid, Paid, Waived, Refunded |
| `document_type` | Prescription, LabResult, MedicalCertificate, Referral, Other |
| `consultation_status` | Open, InProgress, Completed, Cancelled |

## RLS Policies Created (40 policies)

| Table | Key Policies | Notes |
|---|---|---|
| `profiles` | Users read/update own; admin reads all | |
| `user_roles` | Admin/super_admin only | No patient/doctor/staff access |
| `patients` | Own record access; staff/admin CRUD | Doctor access **deferred** to Phase 3 |
| `doctors` | Public reads active; doctors update own; admin CRUD | |
| `services` | Public reads active; admin CRUD | |
| `doctor_services` | **Auth required**; admin CRUD | No longer public |
| `doctor_schedules` | **Auth required**; doctors manage own; admin CRUD | No longer public |
| `doctor_blocked_dates` | **Auth required**; doctors manage own; admin CRUD | No longer public |
| `doctor_day_statuses` | **Auth required**; doctors manage own; admin CRUD | No longer public |
| `clinic_settings` | Authenticated read; admin insert/update | |

## Safety Improvements vs Original

| Risk | Fix |
|---|---|
| Enums would error on rerun | Wrapped in `DO $$ ... EXCEPTION WHEN duplicate_object THEN NULL; END $$` |
| CREATE TABLE would error on rerun | Changed to `CREATE TABLE IF NOT EXISTS` |
| INDEX would error on rerun | Changed to `CREATE INDEX IF NOT EXISTS` |
| Policy would error on rerun | Drop existing policies first via `DO $$ DROP POLICY IF EXISTS` |
| Triggers would duplicate on rerun | Drop before recreating |
| Helper functions not marked STABLE | Added `STABLE` for query planner optimization |
| `ARRAY['admin', 'super_admin']` implicit cast | Changed to explicit `ARRAY['admin'::app_role, 'super_admin'::app_role]` |
| `clinic_settings` missing `created_at` and update trigger | Added both |
| Doctor could read all patients | Removed `doctor` from patients SELECT policy (deferred to Phase 3) |
| Operational tables public | Changed `doctor_services`, `schedules`, `blocked_dates`, `day_statuses` to require auth |
| No bootstrap instructions | Added detailed seeding comments at end of SQL |

## Assumptions Made

1. **`auth.users` schema** — Supabase manages this internally. The `on_auth_user_created` trigger hooks into it.
2. **Role assignment** — `user_roles` rows must be populated by admin. The auto-signup trigger only creates profiles.
3. **Patient-doctor relationship** — separate entities. Doctor assigned-patient access deferred to Phase 3 via bookings/consultations.
4. **Single-clinic model** — multi-tenant not needed for Phase 1.
5. **`doctor_services` / `doctor_schedules`** — no `updated_at` column (pure join/replace tables).
6. **`patient_code`** — TEXT UNIQUE, generation scheme deferred to Phase 2.
7. **Service pricing** — `NUMERIC(10,2)` supports PHP peso amounts.
8. **Public booking page** — public slot lookup goes through `get_available_slots` RPC (Phase 2), not direct table reads.

## Intentionally Deferred

| Item | Reason |
|---|---|
| `bookings` table | Needs RPC functions for slot validation |
| `booking_service_items` | Depends on bookings |
| `payments` table | Depends on bookings |
| `consultations` + sub-tables | Phase 3 — consultation workflow |
| `prescriptions`, `lab_orders` | Phase 3 |
| `patient_documents` | Phase 3 — requires Storage buckets |
| `lab_results` | Phase 4 — requires Storage + bookings |
| `patient_vaccinations` | Phase 6 — lower priority |
| `follow_ups` | Phase 3 |
| Storage buckets | Phase 4 |
| RPC functions (25+) | Phases 2-6 |
| Realtime subscriptions | Phase 6 |
| Seed data | After SQL is confirmed working |
| Doctor access to assigned patients | Phase 3 — when bookings/consultations exist |

## How to Run in Supabase SQL Editor

1. Open your Supabase dashboard → **SQL Editor**
2. Create a new query
3. Paste the entire contents of `phase-01-foundation.sql`
4. Click **Run** (Ctrl+Enter)
5. Verify no errors — the script is safe to re-run if any step fails
6. After successful run, follow the bootstrap comments at the end of the SQL

## Bootstrap Warning

The first admin/super_admin user **cannot** be created by this SQL alone. You must:
1. Create a user via Supabase Auth (email/password or invite)
2. Find their `auth.users` ID: `SELECT id, email FROM auth.users;`
3. Insert the role: `INSERT INTO public.user_roles (user_id, role) VALUES ('<uuid>', 'super_admin');`

This is intentional — Supabase Auth manages user creation, and we never insert into `auth.users` directly.
