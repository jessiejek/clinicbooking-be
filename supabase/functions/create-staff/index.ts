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
      return errorResponse('Access denied. Only admin or super_admin can create staff.', 403, headers);
    }

    // ---- Validate input ----
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

    // ---- Create auth user ----
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

    // ---- Insert/update profile row ----
    // The trigger `on_auth_user_created` may already have created a basic profile,
    // so we UPSERT to ensure full_name and phone are set correctly.
    const { error: profileError } = await adminClient
      .from('profiles')
      .upsert({
        id: userId,
        email,
        full_name: fullName,
        phone,
      }, { onConflict: 'id', ignoreDuplicates: false });

    if (profileError) {
      // Non-fatal: log but don't fail the whole request
      console.error('Profile upsert warning:', profileError.message);
    }

    // ---- Insert staff role ----
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

    // ---- Return safe response (no password) ----
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
