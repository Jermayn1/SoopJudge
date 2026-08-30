// Wie viele unterscheidbare Ausgänge hat ein API-Aufruf? Fünf, nicht zwei.
//
// Im stillgelegten Blazor-Frontend war "null" das einzige Fehlersignal. Damit
// fielen "gibt es nicht" und "Server nicht erreichbar" zusammen, und bei
// gestopptem Backend stand auf der Aufgabenseite "Diese Aufgabe gibt es nicht
// (mehr)" — die denkbar irreführendste Auskunft. Dieser Typ macht den
// Unterschied unübersehbar: wer ihn auswertet, muss alle Fälle behandeln.
//
// "unauthorized" kam mit dem Admin-Bereich dazu, aus demselben Grund: liefe ein
// abgelaufenes Cookie als "rejected" durch, zeigte die Verwaltung eine
// Fehlermeldung statt der Anmeldung, und niemand käme je wieder hinein.
export type ApiResult<T> =
  | { kind: 'ok'; value: T }
  | { kind: 'notFound'; message: string }
  | { kind: 'unauthorized'; message: string }
  | { kind: 'rejected'; message: string }
  | { kind: 'unreachable'; message: string }

const DEFAULT_BASE_URL = 'http://localhost:5120'

// Im Betrieb ist VITE_API_URL LEER, und das ist kein Versehen: hinter dem
// Reverse Proxy liegen Frontend und API auf demselben Ursprung, die Aufrufe
// gehen also relativ raus ("/api/..."). Damit steckt kein Hostname im Image und
// dasselbe Image läuft auf jedem Server.
//
// Deshalb ?? und nicht ||. Eine leere Zeichenkette ist falsy: mit || wäre die
// ausdrückliche Angabe "gleicher Ursprung" still auf localhost:5120
// zurückgefallen - und zwar auf den localhost des TEILNEHMERS, wo nichts
// horcht. Der Fehler hätte wie ein ausgefallenes Backend ausgesehen.
//
// Der Standard greift nur, wenn die Variable gar nicht gesetzt ist: das ist die
// lokale Entwicklung, in der Backend und Frontend getrennt laufen.
const konfigurierteAdresse = import.meta.env.VITE_API_URL as string | undefined

export const apiBaseUrl: string = konfigurierteAdresse ?? DEFAULT_BASE_URL

// Das Backend antwortet auf Ablehnungen mit text/plain und fertigen deutschen
// Sätzen ("'notiz.txt' ist keine .java-Datei."). Die werden dem Teilnehmer im
// Wortlaut gezeigt, nicht durch eine eigene Meldung ersetzt.
async function readMessage(response: Response, fallback: string): Promise<string> {
  try {
    const text = (await response.text()).trim()
    return text.length > 0 ? text : fallback
  } catch {
    return fallback
  }
}

type RequestOptions = {
  method?: string
  body?: BodyInit
  signal?: AbortSignal
  headers?: Record<string, string>
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<ApiResult<T>> {
  let response: Response

  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      method: options.method ?? 'GET',
      body: options.body,
      signal: options.signal,
      // text/plain steht bewusst VORNE. Die API gibt Ablehnungen als nackten
      // String zurück (BadRequest("...")); ASP.NET wählt dafür den ersten
      // passenden Formatter aus dem Accept-Kopf. Stand application/json vorn,
      // kam die Meldung JSON-kodiert mit Anführungszeichen an und wurde dem
      // Teilnehmer als »"'notiz.txt' ist keine .java-Datei."« gezeigt.
      // Objekte liefert die API weiterhin als JSON — für die kann der
      // StringOutputFormatter nicht einspringen.
      headers: { Accept: 'text/plain, application/json', ...options.headers },
      // Das Anmelde-Cookie des Admin-Bereichs liegt auf der API (Port 5120),
      // die Seite läuft auf 5173. Ohne "include" lässt der Browser es bei
      // jedem Aufruf weg, und jeder Admin-Endpunkt antwortet mit 401.
      credentials: 'include',
    })
  } catch (error) {
    // Hierher führt nur ein Netzwerkfehler — der Server hat gar nicht
    // geantwortet. Ein abgebrochener Aufruf ist kein Fehler des Servers.
    if (error instanceof DOMException && error.name === 'AbortError') throw error

    return {
      kind: 'unreachable',
      message: 'Der Server ist nicht erreichbar. Läuft das Backend?',
    }
  }

  // 403 kommt hier mit hinein: für ein Frontend mit genau einer Rolle ist
  // "darf nicht" dasselbe wie "nicht angemeldet" — in beiden Fällen hilft nur
  // die Anmeldung weiter.
  if (response.status === 401 || response.status === 403) {
    return {
      kind: 'unauthorized',
      message: await readMessage(response, 'Für diesen Bereich ist eine Anmeldung nötig.'),
    }
  }

  if (response.status === 404) {
    return { kind: 'notFound', message: await readMessage(response, 'Nicht gefunden.') }
  }

  if (!response.ok) {
    return {
      kind: 'rejected',
      message: await readMessage(response, `Der Server hat mit ${response.status} geantwortet.`),
    }
  }

  if (response.status === 204) {
    return { kind: 'ok', value: undefined as T }
  }

  try {
    return { kind: 'ok', value: (await response.json()) as T }
  } catch {
    return {
      kind: 'rejected',
      message: 'Die Antwort des Servers war unlesbar.',
    }
  }
}

// Schreibende Aufrufe mit JSON-Rumpf.
//
// Der Content-Type steht hier und nicht in request(): beim Hochladen von
// Dateien muss ihn der Browser selbst bilden, weil er die multipart-Grenze
// enthält. Wer ihn dort pauschal setzt, macht jeden Upload kaputt.
export function jsonRequest<T>(
  path: string,
  method: string,
  body?: unknown,
  signal?: AbortSignal,
): Promise<ApiResult<T>> {
  const hasBody = body !== undefined

  return request<T>(path, {
    method,
    body: hasBody ? JSON.stringify(body) : undefined,
    headers: hasBody ? { 'Content-Type': 'application/json' } : undefined,
    signal,
  })
}
