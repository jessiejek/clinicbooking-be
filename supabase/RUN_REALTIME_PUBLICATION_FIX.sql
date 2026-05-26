-- ═══════════════════════════════════════════════════════════
-- Safely add tables to supabase_realtime publication
-- Skips tables already in the publication
-- ═══════════════════════════════════════════════════════════

DO $$
DECLARE
  v_table TEXT;
  v_tables TEXT[] := ARRAY['bookings', 'notifications', 'consultations', 'doctor_day_statuses'];
  v_member BOOLEAN;
BEGIN
  FOREACH v_table IN ARRAY v_tables LOOP
    -- Check if the table is already in the publication
    SELECT EXISTS (
      SELECT 1
      FROM pg_publication_tables
      WHERE pubname = 'supabase_realtime'
        AND schemaname = 'public'
        AND tablename = v_table
    ) INTO v_member;

    IF NOT v_member THEN
      EXECUTE format('ALTER PUBLICATION supabase_realtime ADD TABLE public.%I', v_table);
      RAISE NOTICE 'Added %.% to supabase_realtime', 'public', v_table;
    ELSE
      RAISE NOTICE '% is already a member of supabase_realtime', v_table;
    END IF;
  END LOOP;
END $$;
