import { serve } from 'https://deno.land/std@0.170.0/http/server.ts';
import { createClient } from 'https://esm.sh/@supabase/supabase-js@2';
import { corsHeaders, errorResponse } from '../_shared/cors.ts';

serve(async (req: Request): Promise<Response> => {
  const headers = corsHeaders();

  // Handle CORS preflight
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers });
  }

  try {
    // ---- 1. Read Authorization header ----
    const authHeader = req.headers.get('Authorization');
    if (!authHeader) {
      return errorResponse('Missing Authorization header.', 401, headers);
    }

    // ---- 2. Validate caller via anon client ----
    const anonClient = createClient(
      Deno.env.get('SUPABASE_URL')!,
      Deno.env.get('SUPABASE_ANON_KEY')!,
    );

    const token = authHeader.replace(/^Bearer\s+/i, '').trim();
    const { data: { user: caller }, error: authError } = await anonClient.auth.getUser(token);

    if (authError || !caller) {
      return errorResponse('Authentication failed. Invalid or expired token.', 401, headers);
    }

    const callerEmail = caller.email?.trim().toLowerCase();
    if (!callerEmail) {
      return errorResponse('Authenticated user has no email address.', 400, headers);
    }

    const callerId = caller.id;
    const callerMetadata = caller.user_metadata ?? {};

    // ---- 3. Get service role key ----
    const serviceRoleKey =
      Deno.env.get('SUPABASE_SERVICE_ROLE_KEY') ??
      Deno.env.get('SERVICE_ROLE_KEY');

    if (!serviceRoleKey) {
      return errorResponse('Edge Function service role is not configured.', 500, headers);
    }

    const adminClient = createClient(
      Deno.env.get('SUPABASE_URL')!,
      serviceRoleKey,
      { auth: { autoRefreshToken: false, persistSession: false } },
    );

    // ---- 4. Find pending staff invite matching email ----
    const { data: invites, error: inviteError } = await adminClient
      .from('staff_invites')
      .select('*')
      .eq('status', 'pending')
      .eq('email', callerEmail)
      .limit(1);

    if (inviteError) {
      console.error('staff_invites query error:', inviteError.message);
      return errorResponse('Could not check staff invite status.', 500, headers);
    }

    const invite = (invites ?? [])[0];

    if (!invite) {
      // No pending invite found — return activated:false (not an error)
      return new Response(
        JSON.stringify({
          activated: false,
          role: null,
          reason: 'No pending staff invite found.',
        }),
        { status: 200, headers: { ...headers, 'Content-Type': 'application/json' } },
      );
    }

    // ---- 5. Pending invite exists — activate the staff member ----

    // 5a. Upsert profile row for the authenticated user
    const profileFullName = callerMetadata['full_name'] ?? invite.full_name ?? callerEmail;
    const avatarUrl = callerMetadata['avatar_url'] ?? callerMetadata['picture'] ?? null;

    const { error: profileError } = await adminClient
      .from('profiles')
      .upsert({
        id: callerId,
        email: callerEmail,
        full_name: profileFullName,
        avatar_url: avatarUrl,
        phone: invite.phone ?? null,
        is_active: true,
      }, { onConflict: 'id', ignoreDuplicates: false });

    if (profileError) {
      console.error('Profile upsert error:', profileError.message);
      return errorResponse('Failed to create profile during staff activation.', 500, headers);
    }

    // 5b. Upsert staff role in user_roles
    const { error: roleInsertError } = await adminClient
      .from('user_roles')
      .upsert({
        user_id: callerId,
        role: 'staff',
      }, { onConflict: 'user_id, role', ignoreDuplicates: false });

    if (roleInsertError) {
      console.error('Role insert error:', roleInsertError.message);
      return errorResponse('Failed to assign staff role during activation.', 500, headers);
    }

    // ---- NOTE: Staff does not have an explicit staff table or staff row.
    // The profile + user_roles entry is sufficient for staff functionality.
    // If a staff-specific table is needed later, add the upsert here. ----

    // 5c. Mark staff invite as accepted
    const inviteUpdatePayload: Record<string, unknown> = {
      status: 'accepted',
      accepted_user_id: callerId,
      accepted_at: new Date().toISOString(),
    };

    const { error: inviteUpdateError } = await adminClient
      .from('staff_invites')
      .update(inviteUpdatePayload)
      .eq('id', invite.id);

    if (inviteUpdateError) {
      console.error('Invite update error:', inviteUpdateError.message);
      // Non-fatal: activation already completed, just log the warning
    }

    // ---- 6. Return success ----
    return new Response(
      JSON.stringify({
        activated: true,
        role: 'staff',
      }),
      { status: 200, headers: { ...headers, 'Content-Type': 'application/json' } },
    );
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'Unexpected error.';
    console.error('activate-staff-invite error:', message);
    return errorResponse(message, 500, headers);
  }
});
