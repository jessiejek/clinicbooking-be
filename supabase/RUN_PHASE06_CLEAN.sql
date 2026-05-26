-- ═══════════════════════════════════════════════════════════
-- Phase 06: Push Notifications + Realtime Infrastructure
-- SAFE VERSION — skips ALTER PUBLICATION (already done)
-- ═══════════════════════════════════════════════════════════

-- ── 1. user_device_tokens ────────────────────────────────

CREATE TABLE IF NOT EXISTS public.user_device_tokens (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id       UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  token         TEXT NOT NULL,
  platform      TEXT NOT NULL DEFAULT 'web',
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (user_id, platform, token)
);

CREATE INDEX IF NOT EXISTS idx_device_tokens_user ON public.user_device_tokens(user_id);

ALTER TABLE public.user_device_tokens ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users manage own device tokens" ON public.user_device_tokens;
CREATE POLICY "Users manage own device tokens"
  ON public.user_device_tokens
  FOR ALL
  USING (auth.uid() = user_id)
  WITH CHECK (auth.uid() = user_id);

-- ── 2. upsert_device_token ────────────────────────────────

CREATE OR REPLACE FUNCTION public.upsert_device_token(
  p_token    TEXT,
  p_platform TEXT DEFAULT 'web'
) RETURNS public.user_device_tokens
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
  v_user_id UUID := auth.uid();
  v_result  public.user_device_tokens;
BEGIN
  INSERT INTO public.user_device_tokens (user_id, token, platform)
  VALUES (v_user_id, p_token, p_platform)
  ON CONFLICT (user_id, platform, token)
    DO UPDATE SET updated_at = now()
  RETURNING * INTO v_result;
  RETURN v_result;
END;
$$;

-- ── 3. Edge Function trigger ───────────────────────────────

CREATE OR REPLACE FUNCTION public.notify_send_push()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = ''
AS $$
DECLARE
  v_edge_url TEXT := current_setting('app.edge_function_url', true);
  v_anon_key TEXT := current_setting('app.supabase_anon_key', true);
BEGIN
  IF v_edge_url IS NULL OR v_edge_url = '' THEN
    RETURN NEW;
  END IF;
  BEGIN
    PERFORM
      net.http_post(
        url := v_edge_url || '/send-push-notification',
        headers := jsonb_build_object(
          'Content-Type', 'application/json',
          'Authorization', 'Bearer ' || v_anon_key,
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
  EXCEPTION
    WHEN OTHERS THEN
      NULL;
  END;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_notifications_send_push ON public.notifications;

CREATE TRIGGER trg_notifications_send_push
  AFTER INSERT ON public.notifications
  FOR EACH ROW
  EXECUTE FUNCTION public.notify_send_push();

-- ── 4. Helper: Get user device tokens ─────────────────────

CREATE OR REPLACE FUNCTION public.get_user_device_tokens(p_user_id UUID)
RETURNS TABLE(token TEXT, platform TEXT)
LANGUAGE sql
SECURITY DEFINER
STABLE
AS $$
  SELECT token, platform
  FROM public.user_device_tokens
  WHERE user_id = p_user_id
    AND updated_at > now() - INTERVAL '90 days';
$$;
