# Audit Logs — Required SQL

The `audit_logs` table is needed by the Admin Booking Detail page to record
actions performed on bookings (confirm, reject, payment confirm, etc.).

## Table

```sql
-- ============================================================
-- Audit Logs — record admin/staff actions on entities
-- ============================================================

CREATE TABLE IF NOT EXISTS public.audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_type TEXT NOT NULL,           -- 'Booking', 'Payment', 'Patient', 'Doctor', 'Settings', 'Consultation'
    entity_id UUID NOT NULL,             -- FK to the target entity (no formal FK to avoid circular deps)
    action TEXT NOT NULL,                -- e.g. 'Confirmed booking', 'Waived payment'
    performed_by UUID NOT NULL,          -- user_id of the actor (references auth.users)
    performed_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    details TEXT                         -- optional notes / reason
);

-- Index for fast lookups by entity
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity ON public.audit_logs (entity_type, entity_id);

-- Index for admin dashboard timeline
CREATE INDEX IF NOT EXISTS idx_audit_logs_performed_at ON public.audit_logs (performed_at DESC);

-- Enable RLS
ALTER TABLE public.audit_logs ENABLE ROW LEVEL SECURITY;
```

## RLS Policies

```sql
-- Admin and staff can SELECT audit logs (view history)
CREATE POLICY "audit_logs_select_admin_staff"
    ON public.audit_logs
    FOR SELECT
    USING (
        EXISTS (
            SELECT 1 FROM public.user_roles
            WHERE user_id = auth.uid()
              AND role IN ('admin', 'super_admin', 'staff')
        )
    );

-- Authenticated users can INSERT audit logs
-- (any logged-in user can record an action)
CREATE POLICY "audit_logs_insert_authenticated"
    ON public.audit_logs
    FOR INSERT
    WITH CHECK (
        performed_by = auth.uid()
    );

-- No UPDATE or DELETE — audit logs are append-only
```

## Permissions

```sql
-- No special grants needed. RLS handles access.
-- Authenticated users insert; admin/staff select.
```

## Usage (call from Edge Function or frontend)

**Frontend (via `supabase.client`):**

```typescript
await supabase.client
  .from('audit_logs')
  .insert({
    entity_type: 'Booking',
    entity_id: bookingId,
    action: 'Confirmed booking',
    performed_by: currentUserId,
    details: reason ?? null,
  });
```

**Edge Function (via `adminClient` with service_role):**

```typescript
await adminClient
  .from('audit_logs')
  .insert({
    entity_type: 'Staff',
    entity_id: userId,
    action: 'Created staff account',
    performed_by: callerId,
  });
```

## Note

The `performed_by` column uses `auth.uid()` (insert RLS enforces this).  
The frontend insert sets `performed_by` to the current user's ID, which must
match `auth.uid()` or the RLS insert policy will reject it.
