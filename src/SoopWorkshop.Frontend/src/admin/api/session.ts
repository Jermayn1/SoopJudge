import type { components } from '../../api/schema'
import { jsonRequest, request, type ApiResult } from '../../api/client'

type Schemas = components['schemas']

// Fragt, ob die aktuelle Sitzung angemeldet ist.
//
// Die Antwort ist entweder 200 mit isAuthenticated=true oder 401 — ein
// "false" gibt es nicht. Der Aufrufer unterscheidet deshalb nicht am Wert,
// sondern am Ausgang: ok, unauthorized oder unreachable.
export function fetchSession(signal?: AbortSignal): Promise<ApiResult<Schemas['AdminSessionDto']>> {
  return request<Schemas['AdminSessionDto']>('/api/admin/auth/session', { signal })
}

export function login(password: string, signal?: AbortSignal): Promise<ApiResult<void>> {
  return jsonRequest<void>('/api/admin/auth/login', 'POST', { password }, signal)
}

export function logout(signal?: AbortSignal): Promise<ApiResult<void>> {
  return jsonRequest<void>('/api/admin/auth/logout', 'POST', undefined, signal)
}
