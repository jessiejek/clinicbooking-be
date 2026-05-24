// Shared CORS + utilities for Clinic Booking Edge Functions

export function corsHeaders(origin = '*'): Record<string, string> {
  return {
    'Access-Control-Allow-Origin': origin,
    'Access-Control-Allow-Methods': 'POST, GET, OPTIONS',
    'Access-Control-Allow-Headers': [
      'authorization',
      'x-client-info',
      'apikey',
      'content-type',
      'x-supabase-api-version',
    ].join(', '),
  };
}

export function errorResponse(
  message: string,
  status: number,
  headers: Record<string, string>,
): Response {
  return new Response(JSON.stringify({ message }), {
    status,
    headers: { ...headers, 'Content-Type': 'application/json' },
  });
}