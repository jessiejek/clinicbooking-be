-- ============================================================
-- FIX TWO BUGS
-- 1. Add contact_email column to patients table (frontend queries it)
-- 2. Fix get_available_slots enum value 'unavailable' -> 'UnavailableToday'
-- ============================================================

-- BUG 1: patients table has 'email' column but frontend queries 'contact_email'
ALTER TABLE public.patients
  ADD COLUMN IF NOT EXISTS contact_email TEXT;

-- Backfill contact_email from existing email column
UPDATE public.patients
  SET contact_email = email
  WHERE contact_email IS NULL AND email IS NOT NULL;

-- BUG 2: get_available_slots uses 'unavailable' but enum is 'UnavailableToday'
CREATE OR REPLACE FUNCTION public.get_available_slots(
    p_doctor_id UUID,
    p_appointment_date DATE
)
RETURNS TABLE(
    slot_start_time TIME,
    slot_end_time TIME,
    is_available BOOLEAN,
    booked_count BIGINT,
    capacity BIGINT
)
LANGUAGE plpgsql
STABLE SECURITY DEFINER
SET search_path TO 'public'
AS $$
DECLARE
    v_slot_duration INT;
    v_slot_capacity INT;
    v_dow INT;
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
    DECLARE
        v_day_status RECORD;
    BEGIN
        SELECT * INTO v_day_status FROM public.doctor_day_statuses
        WHERE doctor_id = p_doctor_id AND target_date = p_appointment_date;

        -- FIXED: 'unavailable' -> 'UnavailableToday' to match enum doctor_day_status_type
        IF v_day_status.status = 'UnavailableToday' THEN
            RETURN;
        END IF;

        IF v_day_status.status = 'limited' AND v_day_status.max_slots IS NOT NULL THEN
            v_slot_capacity := v_day_status.max_slots;
        END IF;
    END;

    -- Get day-of-week number: 0=Sunday, 1=Monday, ..., 6=Saturday
    v_dow := EXTRACT(DOW FROM p_appointment_date)::INT;

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
          AND (
            (v_dow = 0 AND ds.day_of_week = 'Sunday')
            OR (v_dow = 1 AND ds.day_of_week = 'Monday')
            OR (v_dow = 2 AND ds.day_of_week = 'Tuesday')
            OR (v_dow = 3 AND ds.day_of_week = 'Wednesday')
            OR (v_dow = 4 AND ds.day_of_week = 'Thursday')
            OR (v_dow = 5 AND ds.day_of_week = 'Friday')
            OR (v_dow = 6 AND ds.day_of_week = 'Saturday')
          )
    ),
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
        SELECT
            b.slot_start_time AS booking_slot_start_time,
            b.slot_end_time AS booking_slot_end_time
        FROM public.bookings b
        WHERE b.doctor_id = p_doctor_id
          AND b.appointment_date = p_appointment_date
          AND b.status NOT IN ('Cancelled', 'NoShow', 'Expired')
    )
    SELECT
        ts.slot_start_time,
        ts.slot_end_time,
        (COUNT(eb.*) < v_slot_capacity) AS is_available,
        COUNT(eb.*)::BIGINT AS booked_count,
        v_slot_capacity::BIGINT AS capacity
    FROM time_slots ts
    LEFT JOIN existing_bookings eb
        ON eb.booking_slot_start_time < ts.slot_end_time
        AND eb.booking_slot_end_time > ts.slot_start_time
    WHERE (v_now_tstz IS NULL OR ts.slot_start_ts >= v_now_tstz)
    GROUP BY ts.slot_start_time, ts.slot_end_time, ts.slot_start_ts
    ORDER BY ts.slot_start_time;
END;
$$;
