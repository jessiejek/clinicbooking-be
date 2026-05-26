-- ═══════════════════════════════════════════════════════════
-- Push notification triggers for check-in / undo check-in
-- Fires when bookings status changes, inserts into notifications
-- table, which then triggers notify_send_push → edge function → FCM
-- ═══════════════════════════════════════════════════════════

CREATE OR REPLACE FUNCTION public.notify_booking_checkin_change()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = ''
AS $$
DECLARE
  v_doctor_user_id UUID;
  v_patient_name   TEXT;
BEGIN
  -- Only proceed if status actually changed
  IF OLD.status IS NOT DISTINCT FROM NEW.status THEN
    RETURN NEW;
  END IF;

  -- Get the doctor's user_id (for the notification recipient)
  SELECT d.user_id INTO v_doctor_user_id
  FROM public.doctors d
  WHERE d.id = NEW.doctor_id;

  IF v_doctor_user_id IS NULL THEN
    RETURN NEW;  -- No doctor linked — nothing to notify
  END IF;

  -- Get the patient's full name
  SELECT CONCAT_WS(' ', p.first_name, p.last_name) INTO v_patient_name
  FROM public.patients p
  WHERE p.id = NEW.patient_id;

  -- ── Check-in: status changed TO 'CheckedIn' ─────────
  IF NEW.status = 'CheckedIn' AND OLD.status IS DISTINCT FROM 'CheckedIn' THEN
    INSERT INTO public.notifications (user_id, title, message, navigate_to)
    VALUES (
      v_doctor_user_id,
      'Patient Arrived',
      COALESCE(v_patient_name, 'Your patient') || ' is already in the clinic and has checked in for their appointment.',
      '/doctor/appointments'
    );

  -- ── Undo check-in: status changed FROM 'CheckedIn' ──
  ELSIF OLD.status = 'CheckedIn' AND NEW.status IS DISTINCT FROM 'CheckedIn' THEN
    INSERT INTO public.notifications (user_id, title, message, navigate_to)
    VALUES (
      v_doctor_user_id,
      'Patient Check-In Undone',
      COALESCE(v_patient_name, 'Your patient') || '''s check-in has been undone.',
      '/doctor/appointments'
    );
  END IF;

  RETURN NEW;
END;
$$;

-- ── Attach the trigger to bookings table ──────────────
DROP TRIGGER IF EXISTS trg_notify_booking_checkin_change ON public.bookings;

CREATE TRIGGER trg_notify_booking_checkin_change
  AFTER UPDATE OF status ON public.bookings
  FOR EACH ROW
  WHEN (OLD.status IS DISTINCT FROM NEW.status)
  EXECUTE FUNCTION public.notify_booking_checkin_change();
