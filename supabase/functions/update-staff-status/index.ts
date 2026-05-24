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
    // ---- Verify caller is authenticated ----
    const supabase = createClient(
      Deno.env.get('SUPABASE_URL')!,
      Deno.env.get('SUPABASE_ANON_KEY')!,
      { global: { headers: { Authorization: req.headers.get('Authorization')! } } },
    );

    const { data: { user: caller }, error: authError } = await supabase.auth.getUser();
    if (authError || !caller) {
      return errorResponse('Authentication required.', 401, headers);
    }

    // ---- Create admin client with service_role ----
    const serviceRoleKey = Deno.env.get('SUPABASE_SERVICE_ROLE_KEY');
    if (!serviceRoleKey) {
      return errorResponse('Server configuration error: service_role key not set.', 500, headers);
    }

    const adminClient = createClient(
      Deno.env.get('SUPABASE_URL')!,
      serviceRoleKey,
      { auth: { autoRefreshToken: false, persistSession: false } },
    );

    // ---- Verify caller is admin or super_admin ----
    const { data: callerRoles, error: rolesError } = await adminClient
      .from('user_roles')
      .select('role')
      .eq('user_id', caller.id);

    if (rolesError) {
      return errorResponse('Could not verify caller permissions.', 403, headers);
    }

    const allowedRoles = callerRoles?.map(r => r.role) ?? [];
    if (!allowedRoles.includes('admin') && !allowedRoles.includes('super_admin')) {
      return errorResponse('Access denied. Only admin or super_admin can update staff status.', 403, headers);
    }

    // ---- Validate input ----
    const body: UpdateStaffStatusPayload = await req.json();

    if (!body.userId?.trim()) {
      return errorResponse('userId is required.', 400, headers);
    }
    if (!body.action || !['ban', 'unban'].includes(body.action)) {
      return errorResponse("action must be 'ban' or 'unban'.", 400, headers);
    }

    const targetUserId = body.userId.trim();
    const isBan = body.action === 'ban';

    // ---- Apply ban/unban via Auth admin API ----
    // `ban_duration: 'none'` removes the ban, any positive duration (e.g. '876600h' = 100 years)
    // applies a ban of that length.
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

    // ---- Update profiles.status column if it exists ----
    // The `status` column is added via SQL migration (see deployment docs).
    // If the column doesn't exist yet, this is a no-op.
    const newStatus = isBan ? 'Inactive' : 'Active';
    const { error: profileUpdateError } = await adminClient
      .from('profiles')
      .update({ status: newStatus })
      .eq('id', targetUserId);

    if (profileUpdateError) {
      // Column may not exist yet — log and continue
      console.warn(
        `Could not update profiles.status for user ${targetUserId}: ${profileUpdateError.message}. ` +
        'This is expected if the status column has not been added via migration.',
      );
    }

    // ---- Return safe response ----
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
