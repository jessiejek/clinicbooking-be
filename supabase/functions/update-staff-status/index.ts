import { serve } from 'https://deno.land/std@0.170.0/http/server.ts';
import { createClient } from 'https://esm.sh/@supabase/supabase-js@2';
import { corsHeaders, errorResponse } from '../_shared/cors.ts';

interface UpdateStaffStatusPayload {
  userId: string;
  action: 'ban' | 'unban';
}

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

    // ---- 3. Create admin client with service_role ----
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

    // ---- 4. Verify caller is admin or super_admin ----
    const { data: callerRoles, error: rolesError } = await adminClient
      .from('user_roles')
      .select('role')
      .eq('user_id', caller.id);

    if (rolesError) {
      return errorResponse('Could not verify caller permissions.', 403, headers);
    }

    const normalizedRoles = (callerRoles ?? []).map(
      r => String(r.role).toLowerCase()
    );

    const isAllowed = normalizedRoles.includes('admin') || normalizedRoles.includes('super_admin');
    if (!isAllowed) {
      return new Response(
        JSON.stringify({
          error: 'Admin or super_admin role required',
          userId: caller.id,
        }),
        { status: 403, headers: { ...headers, 'Content-Type': 'application/json' } },
      );
    }

    // ---- 5. Validate input ----
    const body: UpdateStaffStatusPayload = await req.json();

    if (!body.userId?.trim()) {
      return errorResponse('userId is required.', 400, headers);
    }
    if (!body.action || !['ban', 'unban'].includes(body.action)) {
      return errorResponse("action must be 'ban' or 'unban'.", 400, headers);
    }

    const targetUserId = body.userId.trim();
    const isBan = body.action === 'ban';

    // ---- 6. Apply ban/unban via Auth admin API ----
    const { error: updateError } = await adminClient.auth.admin.updateUserById(
      targetUserId,
      { ban_duration: isBan ? '876600h' : 'none' },
    );

    if (updateError) {
      return errorResponse(
        `Failed to ${isBan ? 'deactivate' : 'reactivate'} user: ${updateError.message}`,
        500,
        headers,
      );
    }

    // ---- 7. Update profiles.status column if it exists ----
    const newStatus = isBan ? 'Inactive' : 'Active';
    const { error: profileUpdateError } = await adminClient
      .from('profiles')
      .update({ status: newStatus })
      .eq('id', targetUserId);

    if (profileUpdateError) {
      console.warn(
        `Could not update profiles.status for user ${targetUserId}: ${profileUpdateError.message}. ` +
        'This is expected if the status column has not been added via migration.',
      );
    }

    // ---- 8. Return safe response ----
    return new Response(
      JSON.stringify({
        userId: targetUserId,
        status: newStatus,
        banned: isBan,
      }),
      { status: 200, headers: { ...headers, 'Content-Type': 'application/json' } },
    );
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'Unexpected error.';
    console.error('update-staff-status error:', message);
    return errorResponse(message, 500, headers);
  }
});
