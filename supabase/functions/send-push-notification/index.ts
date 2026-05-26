import { serve } from 'https://deno.land/std@0.170.0/http/server.ts';
import { createClient } from 'https://esm.sh/@supabase/supabase-js@2';
import * as jose from 'https://deno.land/x/jose@v4.14.4/index.ts';
import { corsHeaders, errorResponse } from '../_shared/cors.ts';

interface PushPayload {
  notification_id: string;
  user_id: string;
  title: string;
  message: string;
  navigate_to?: string | null;
}

interface FcmMessage {
  message: {
    token: string;
    notification: {
      title: string;
      body: string;
    };
    data?: Record<string, string>;
    webpush?: {
      fcm_options?: {
        link?: string;
      };
    };
  };
}

serve(async (req: Request): Promise<Response> => {
  const headers = corsHeaders();

  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers });
  }

  try {
    const authHeader = req.headers.get('x-supabase-auth');
    if (authHeader !== 'service_role') {
      return errorResponse('Unauthorized. Only Supabase triggers may call this function.', 401, headers);
    }

    const body: PushPayload = await req.json();
    if (!body.user_id || !body.title || !body.message) {
      return errorResponse('Missing required fields: user_id, title, message.', 400, headers);
    }

    const adminClient = createClient(
      Deno.env.get('SUPABASE_URL')!,
      Deno.env.get('SERVICE_ROLE_KEY') ?? Deno.env.get('SUPABASE_SERVICE_ROLE_KEY')!,
      { auth: { autoRefreshToken: false, persistSession: false } },
    );

    const { data: tokens, error: tokenError } = await adminClient
      .from('user_device_tokens')
      .select('token, platform')
      .eq('user_id', body.user_id);

    if (tokenError) {
      console.error('Error fetching device tokens:', tokenError.message);
      return errorResponse('Failed to fetch device tokens.', 500, headers);
    }

    if (!tokens || tokens.length === 0) {
      return new Response(
        JSON.stringify({ sent: 0, reason: 'No device tokens registered for this user.' }),
        { status: 200, headers: { ...headers, 'Content-Type': 'application/json' } },
      );
    }

    const fcmClientEmail = Deno.env.get('FCM_CLIENT_EMAIL');
    const fcmPrivateKey = Deno.env.get('FCM_PRIVATE_KEY');
    const fcmProjectId = Deno.env.get('FCM_PROJECT_ID');

    const firebaseTokens = (tokens ?? []).filter((row: any) => row.platform === 'firebase-web');

    let sentCount = 0;
    const errors: Array<{ token: string; error: string }> = [];

    for (const device of firebaseTokens) {
      if (!fcmClientEmail || !fcmPrivateKey || !fcmProjectId) {
        errors.push({
          token: maskToken(device.token),
          error: 'FCM credentials are missing.',
        });
        continue;
      }

      const accessToken = await getFcmAccessToken(fcmClientEmail, fcmPrivateKey);
      if (!accessToken) {
        errors.push({
          token: maskToken(device.token),
          error: 'Failed to obtain FCM OAuth2 token.',
        });
        continue;
      }

      const fcmMessage: FcmMessage = {
        message: {
          token: device.token,
          notification: {
            title: body.title,
            body: body.message,
          },
          data: body.navigate_to ? { navigate_to: body.navigate_to } : undefined,
          webpush: body.navigate_to ? { fcm_options: { link: body.navigate_to } } : undefined,
        },
      };

      try {
        const fcmResponse = await fetch(
          `https://fcm.googleapis.com/v1/projects/${fcmProjectId}/messages:send`,
          {
            method: 'POST',
            headers: {
              Authorization: `Bearer ${accessToken}`,
              'Content-Type': 'application/json',
            },
            body: JSON.stringify(fcmMessage),
          },
        );

        if (fcmResponse.ok) {
          sentCount++;
        } else {
          const errBody = await fcmResponse.text();
          errors.push({ token: maskToken(device.token), error: errBody });
        }
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Unknown error';
        errors.push({ token: maskToken(device.token), error: msg });
      }
    }

    return new Response(
      JSON.stringify({
        sent: sentCount,
        total: firebaseTokens.length,
        errors: errors.length > 0 ? errors : undefined,
      }),
      { status: 200, headers: { ...headers, 'Content-Type': 'application/json' } },
    );
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : 'Unexpected error.';
    console.error('send-push-notification error:', message);
    return errorResponse(message, 500, headers);
  }
});

async function getFcmAccessToken(
  clientEmail: string,
  privateKey: string,
): Promise<string | null> {
  const now = Math.floor(Date.now() / 1000);
  const normalizedPrivateKey = normalizePrivateKey(privateKey);

  const jwt = await new jose.SignJWT({
    iss: clientEmail,
    scope: 'https://www.googleapis.com/auth/firebase.messaging',
    aud: 'https://oauth2.googleapis.com/token',
    iat: now,
    exp: now + 3600,
  })
    .setProtectedHeader({ alg: 'RS256', typ: 'JWT' })
    .sign(await jose.importPKCS8(normalizedPrivateKey, 'RS256'));

  const response = await fetch('https://oauth2.googleapis.com/token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'urn:ietf:params:oauth:grant-type:jwt-bearer',
      assertion: jwt,
    }),
  });

  if (!response.ok) {
    const err = await response.text();
    console.error('OAuth2 token exchange failed:', err);
    return null;
  }

  const data = await response.json();
  return data.access_token as string;
}

function normalizePrivateKey(privateKey: string): string {
  return privateKey
    .replace(/^"|"$/g, '')
    .replace(/\\n/g, '\n')
    .trim();
}

function maskToken(token: string): string {
  if (token.length <= 8) return '***';
  return token.slice(0, 4) + '...' + token.slice(-4);
}

