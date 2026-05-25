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
    console.log('activate-staff-invite: function started');

    // ---- 1. Read Authorization header ----
    const authHeader = req.headers.get('Authorization');
    console.log('activate-staff-invite: authHeader present:', !!authHeader);
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
      console.log('activate-staff-invite: auth failed', authError?.message);
      return errorResponse('Authentication failed. Invalid or expired token.', 401, headers);
    }
    console.log('activate-staff-invite: caller authenticated:', caller.id);

    const callerEmail = caller.email?.trim().toLowerCase();
    if (!callerEmail) {
      return errorResponse('Authenticated user has no email address.', 400, headers);
    }
    console.log('activate-staff-invite: caller email:', callerEmail);

    const callerId = caller.id;
    const callerMetadata = caller.user_metadata ?? {};

    // ---- 3. Get service role key ----
    const serviceRoleKey =
      Deno.env.get('SUPABASE_SERVICE_ROLE_KEY') ??
      Deno.env.get('SERVICE_ROLE_KEY');

    if (!serviceRoleKey) {
      console.log('activate-staff-invite: service role key missing');
      return errorResponse('Edge Function service role is not configured.', 500, headers);
    }
    console.log('activate-staff-invite: service role key available');

    const adminClient = createClient(
      Deno.env.get('SUPABASE_URL')!,
      serviceRoleKey,
      { auth: { autoRefreshToken: false, persistSession: false } },
    );

    // ---- 4. Find pending staff invite matching email ----
    console.log('activate-staff-invite: querying staff_invites for:', callerEmail);
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
    console.log('activate-staff-invite: invite found:', !!invite);

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
    console.log('activate-staff-invite: upserting profile for:', callerId);

    const { error: profileError } = await adminClient
      .from('profiles')
      .upsert({
        id: callerId,
        email: callerEmail,
        full_name: profileFullName,
        avatar_url: avatarUrl,
        // Note: phone column does not exist in production profiles table
        // phone: invite.phone ?? null,
      }, { onConflict: 'id', ignoreDuplicates: false });

    if (profileError) {
      console.error('Profile upsert error:', profileError.message);
      console.error('Profile upsert details:', JSON.stringify(profileError));
      return errorResponse('Failed to create profile during staff activation.', 500, headers);
    }
    console.log('activate-staff-invite: profile upsert OK');

    // 5b. Upsert staff role in user_roles
    console.log('activate-staff-invite: inserting staff role for:', callerId);
    const { error: roleInsertError } = await adminClient
      .from('user_roles')
      .upsert({
        user_id: callerId,
        role: 'staff',
      }, { onConflict: 'user_id, role', ignoreDuplicates: false });

    if (roleInsertError) {
      console.error('Role insert error:', roleInsertError.message);
      console.error('Role insert details:', JSON.stringify(roleInsertError));
      return errorResponse('Failed to assign staff role during activation.', 500, headers);
    }
    console.log('activate-staff-invite: role insert OK');

    // ---- NOTE: Staff does not have an explicit staff table or staff row.
    // The profile + user_roles entry is sufficient for staff functionality. ----

    // 5c. Mark staff invite as accepted
    console.log('activate-staff-invite: updating invite to accepted:', invite.id);
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
    }

    // ---- 6. Return success ----
    console.log('activate-staff-invite: returning success');
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
    console.error('activate-staff-invite error stack:', err instanceof Error ? err.stack : 'no stack');
    return errorResponse(message, 500, headers);
  }
});
