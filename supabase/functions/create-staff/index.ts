import { serve } from 'https://deno.land/std@0.170.0/http/server.ts';
import { createClient } from 'https://esm.sh/@supabase/supabase-js@2';
import { corsHeaders, errorResponse } from '../_shared/cors.ts';

interface CreateStaffPayload {
  fullName: string;
  email: string;
  password?: string;
  phone?: string;
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
    const body: CreateStaffPayload = await req.json();

    if (!body.email?.trim()) {
      return errorResponse('email is required.', 400, headers);
    }
    if (!body.fullName?.trim()) {
      return errorResponse('fullName is required.', 400, headers);
    }

    const email = body.email.trim().toLowerCase();
    const fullName = body.fullName.trim();
    const password = body.password?.trim() || crypto.randomUUID().slice(0, 12); // auto-generate if not provided
    const phone = body.phone?.trim() || null;

    // ---- 6. Create auth user ----
    const { data: newUser, error: createError } = await adminClient.auth.admin.createUser({
      email,
      password,
      email_confirm: true,
      user_metadata: { full_name: fullName },
    });

    if (createError) {
      // Handle duplicate email gracefully
      if (createError.message?.toLowerCase().includes('already registered') ||
          createError.message?.toLowerCase().includes('duplicate')) {
        return errorResponse(
          `A user with email '${email}' is already registered.`,
          409,
          headers,
        );
      }
      return errorResponse(`Failed to create user: ${createError.message}`, 500, headers);
    }

    if (!newUser?.user?.id) {
      return errorResponse('User was created but no ID was returned.', 500, headers);
    }

    const userId = newUser.user.id;

    // ---- 7. Insert/update profile row ----
    const { error: profileError } = await adminClient
      .from('profiles')
      .upsert({
        id: userId,
        email,
        full_name: fullName,
        phone,
      }, { onConflict: 'id', ignoreDuplicates: false });

    if (profileError) {
      console.error('Profile upsert warning:', profileError.message);
    }

    // ---- 8. Insert staff role ----
    const { error: roleInsertError } = await adminClient
      .from('user_roles')
      .insert({
        user_id: userId,
        role: 'staff',
      });

    if (roleInsertError) {
      // Attempt cleanup: remove the auth user if role insert fails
      await adminClient.auth.admin.deleteUser(userId).catch(() => {});
      return errorResponse(
        `Failed to assign staff role: ${roleInsertError.message}. User creation rolled back.`,
        500,
        headers,
      );
    }

    // ---- 9. Return safe response (no password, no secrets) ----
    return new Response(
      JSON.stringify({
        userId,
        email,
        fullName,
        role: 'staff',
      }),
      { status: 200, headers: { ...headers, 'Content-Type': 'application/json' } },
    );
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'Unexpected error.';
    console.error('create-staff error:', message);
    return errorResponse(message, 500, headers);
  }
});
