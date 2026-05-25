-- ============================================================
-- Fast invite activation RPC for auth callback
-- Replaces two cold-start Edge Functions with one DB call
-- Returns: { doctor_activated, staff_activated, role }
-- ============================================================

CREATE OR REPLACE FUNCTION public.check_and_activate_invites()
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path TO 'public'
AS $$
DECLARE
    v_user_id UUID;
    v_email TEXT;
    v_result JSONB;
    v_doctor_activated BOOLEAN := false;
    v_staff_activated BOOLEAN := false;
    v_role TEXT := NULL;
BEGIN
    -- Get current user from auth context
    v_user_id := auth.uid();
    v_email := auth.email();

    IF v_user_id IS NULL THEN
        RETURN jsonb_build_object(
            'doctor_activated', false,
            'staff_activated', false,
            'role', null
        );
    END IF;

    -- 1. Check for pending doctor invite
    IF EXISTS (SELECT 1 FROM public.doctor_invites
               WHERE email = v_email AND status = 'pending' AND expires_at > now()) THEN
        -- Update doctor_invites status
        UPDATE public.doctor_invites
        SET status = 'accepted', accepted_at = now()
        WHERE email = v_email AND status = 'pending';

        -- Ensure profile exists
        INSERT INTO public.profiles (id, full_name, email)
        VALUES (v_user_id, v_email, v_email)
        ON CONFLICT (id) DO NOTHING;

        -- Ensure user_roles row
        INSERT INTO public.user_roles (user_id, role)
        VALUES (v_user_id, 'doctor')
        ON CONFLICT (user_id, role) DO NOTHING;

        v_doctor_activated := true;
        v_role := 'Doctor';
    END IF;

    -- 2. Check for pending staff invite (if doctor didn't already activate)
    IF NOT v_doctor_activated THEN
        IF EXISTS (SELECT 1 FROM public.staff_invites
                   WHERE email = v_email AND status = 'pending' AND expires_at > now()) THEN
            -- Update staff_invites status
            UPDATE public.staff_invites
            SET status = 'accepted', accepted_at = now()
            WHERE email = v_email AND status = 'pending';

            -- Ensure profile exists
            INSERT INTO public.profiles (id, full_name, email)
            VALUES (v_user_id, v_email, v_email)
            ON CONFLICT (id) DO NOTHING;

            -- Ensure user_roles row
            INSERT INTO public.user_roles (user_id, role)
            VALUES (v_user_id, 'staff')
            ON CONFLICT (user_id, role) DO NOTHING;

            v_staff_activated := true;
            v_role := 'Staff';
        END IF;
    END IF;

    v_result := jsonb_build_object(
        'doctor_activated', v_doctor_activated,
        'staff_activated', v_staff_activated,
        'role', v_role
    );

    RETURN v_result;
END;
$$;

-- Grant execute to authenticated users
GRANT EXECUTE ON FUNCTION public.check_and_activate_invites TO authenticated;
