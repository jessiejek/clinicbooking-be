# Supabase Edge Functions — Deployment Guide

## Overview

This document covers deploying the Edge Functions needed for admin staff management
in the Clinic Booking System. These functions use the **`service_role`** key
**server-side only** — never in the frontend.

---

## Prerequisite: Service Role Key

1. Go to your Supabase dashboard → **Settings** → **API**.
2. Copy the **`service_role`** key (NOT the anon key).
3. You will set this as a secret for each function when deploying.

> **⚠️ Security:** The `service_role` key bypasses RLS. Never expose it in
> frontend code, environment files, or public repositories. It must only live
> inside Supabase Edge Functions and their secrets store.

---

## Functions

### 1. `create-staff`

Creates a new staff user in Supabase Auth + profiles + user_roles.

| Item | Detail |
|---|---|
| **Path** | `supabase/functions/create-staff/index.ts` |
| **Method** | POST |
| **Auth** | Caller must be `admin` or `super_admin` (checked via `user_roles`) |
| **Body** | `{ fullName: string, email: string, password?: string, phone?: string }` |
| **Returns** | `{ userId: string, email: string, fullName: string, role: 'staff' }` |
| **Secrets** | `SUPABASE_URL`, `SUPABASE_ANON_KEY`, `SUPABASE_SERVICE_ROLE_KEY` |

**Behavior:**
1. Verifies caller JWT → fetches caller roles from `user_roles`
2. Rejects if not `admin` or `super_admin` (403)
3. Creates Auth user via `admin.createUser()` (always `email_confirm: true`)
4. Upserts row in `profiles` (`id`, `email`, `full_name`, `phone`)
5. Inserts row in `user_roles` (`user_id`, `role: 'staff'`)
6. Returns safe response (no password)
7. On duplicate email, returns 409 with user-friendly message
8. On role insert failure, rolls back by deleting the auth user

---

### 2. `update-staff-status`

Bans or unbans a staff user in Supabase Auth + updates profile status.

| Item | Detail |
|---|---|
| **Path** | `supabase/functions/update-staff-status/index.ts` |
| **Method** | POST |
| **Auth** | Caller must be `admin` or `super_admin` (checked via `user_roles`) |
| **Body** | `{ userId: string, action: 'ban' | 'unban' }` |
| **Returns** | `{ userId: string, status: 'Active' | 'Inactive', banned: boolean }` |
| **Secrets** | `SUPABASE_URL`, `SUPABASE_ANON_KEY`, `SUPABASE_SERVICE_ROLE_KEY` |

**Behavior:**
1. Verifies caller JWT → fetches caller roles from `user_roles`
2. Rejects if not `admin` or `super_admin` (403)
3. Calls `admin.updateUserById(userId, { ban_duration })`:
   - `action: 'ban'` → `ban_duration: '876600h'` (~100 years, effectively permanent)
   - `action: 'unban'` → `ban_duration: 'none'`
4. Attempts to update `profiles.status` to `'Active'` or `'Inactive'` (see SQL migration below)
5. If `profiles.status` column doesn't exist, logged as a warning — no failure

---

## SQL Migration: Add `status` Column to `profiles`

Run this SQL in the Supabase SQL Editor **before** or **after** deploying the
Edge Functions:

```sql
-- Add status column to profiles for staff activation/deactivation display
ALTER TABLE public.profiles
ADD COLUMN IF NOT EXISTS status TEXT NOT NULL DEFAULT 'Active'
CHECK (status IN ('Active', 'Inactive'));

-- Existing rows get 'Active' automatically (the DEFAULT)
```

**Why this is needed:**
- The Edge Function bans the user via Auth (prevents login), but the frontend
  needs a way to display and filter by status without querying `auth.users`
  (which is inaccessible from the frontend).
- The `profiles.status` column provides a simple, frontend-readable status field.
- The `create-staff` function does NOT set this column — new staff default to
  `'Active'` via the DEFAULT constraint. The `update-staff-status` function
  updates it on ban/unban.

---

## Deploy Commands

Use the [Supabase CLI](https://supabase.com/docs/guides/cli) to deploy:

```bash
# Navigate to the supabase directory
cd clinicbooking-be/supabase

# 1. Deploy create-staff
supabase functions deploy create-staff \
  --no-verify-jwt \
  --secret-keys SUPABASE_URL,SUPABASE_ANON_KEY,SUPABASE_SERVICE_ROLE_KEY

# 2. Deploy update-staff-status
supabase functions deploy update-staff-status \
  --no-verify-jwt \
  --secret-keys SUPABASE_URL,SUPABASE_ANON_KEY,SUPABASE_SERVICE_ROLE_KEY
```

**About `--no-verify-jwt`:** The functions verify the caller JWT themselves
(using the anon key) so they can also check `user_roles`. If Supabase verified
the JWT automatically, the function would only see the caller's `sub` claim,
not their role membership. By handling JWT verification internally, we get
full role-based access control.

**Setting secrets interactively (alternative):**

```bash
supabase functions deploy create-staff
# You will be prompted for each secret value
```

---

## Set Secrets in Supabase Dashboard (Manual)

If you prefer not to use the CLI:

1. Go to Supabase dashboard → **Edge Functions** → **create-staff**
2. Click **"Secrets"** → add:
   - `SUPABASE_URL` — your project URL (`https://czswgpjjanllkmmwhmdh.supabase.co`)
   - `SUPABASE_ANON_KEY` — your anon/publishable key
   - `SUPABASE_SERVICE_ROLE_KEY` — your service_role key (keep safe!)
3. Repeat for **update-staff-status**

---

## Local Development (optional)

For local testing with Supabase local development:

```bash
# Start local Supabase
supabase start

# The functions are available at:
# http://localhost:54321/functions/v1/create-staff
# http://localhost:54321/functions/v1/update-staff-status
```

Local env variables are configured in `supabase/config.toml` and the
environment is shared from `supabase/.env.local` (if created).

---

## Testing

### create-staff (local)

```bash
curl -X POST http://localhost:54321/functions/v1/create-staff \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <ADMIN_JWT_TOKEN>" \
  -d '{"fullName":"Jane Staff","email":"jane@clinic.com","password":"Temp1234!"}'
```

### update-staff-status (local)

```bash
curl -X POST http://localhost:54321/functions/v1/update-staff-status \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <ADMIN_JWT_TOKEN>" \
  -d '{"userId":"<TARGET_USER_ID>","action":"ban"}'
```

### Expected error: non-admin caller

```bash
curl -X POST http://localhost:54321/functions/v1/create-staff \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <PATIENT_JWT_TOKEN>" \
  -d '{"fullName":"Bad","email":"bad@test.com"}'

# Response: 403 {"message":"Access denied. Only admin or super_admin can create staff."}
```

---

## Security Checklist

- [ ] `SUPABASE_SERVICE_ROLE_KEY` is set as a **function secret** only
- [ ] `SUPABASE_SERVICE_ROLE_KEY` is **never** in `environment.ts` or `environment.prod.ts`
- [ ] `SUPABASE_SERVICE_ROLE_KEY` is **never** committed to version control
- [ ] Frontend calls use `supabase.client.functions.invoke()` which sends the user's
      own JWT (not the service_role key) in the Authorization header
- [ ] Each function independently verifies the caller is `admin` or `super_admin`
- [ ] Role (`staff`) is hardcoded — never taken from user input
- [ ] Duplicate email returns 409, never exposes internal errors
- [ ] On partial failure (auth created but role insert failed), the auth user is deleted
- [ ] No passwords are returned in any response
- [ ] The SQL migration for `profiles.status` is optional but recommended for full UX
