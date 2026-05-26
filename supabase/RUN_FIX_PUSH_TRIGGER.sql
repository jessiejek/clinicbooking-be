-- ═══════════════════════════════════════════════════════════
-- Fix: replace notify_send_push with hardcoded project URL
-- (ALTER DATABASE custom settings not allowed on Supabase)
-- ═══════════════════════════════════════════════════════════

CREATE EXTENSION IF NOT EXISTS pg_net;

CREATE OR REPLACE FUNCTION public.notify_send_push()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = ''
AS $$
BEGIN
  PERFORM
    net.http_post(
      url := 'https://czswgpjjanllkmmwhmdh.supabase.co/functions/v1/send-push-notification',
      headers := jsonb_build_object(
        'Content-Type', 'application/json',
        'x-supabase-auth', 'service_role'
      ),
      body := jsonb_build_object(
        'notification_id', NEW.id,
        'user_id', NEW.user_id,
        'title', NEW.title,
        'message', NEW.message,
        'navigate_to', NEW.navigate_to
      )::text
    );

  RETURN NEW;
EXCEPTION
  WHEN OTHERS THEN
    RETURN NEW;
END;
$$;
