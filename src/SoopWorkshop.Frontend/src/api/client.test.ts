import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiBaseUrl, jsonRequest, request } from './client'

function antwort(status: number, body = '', contentType = 'application/json'): Response {
  return new Response(status === 204 ? null : body, {
    status,
    headers: { 'Content-Type': contentType },
  })
}

function gibZurueck(response: Response | Promise<Response>) {
  const spy = vi.fn().mockResolvedValue(response)
  vi.stubGlobal('fetch', spy)
  return spy
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('request — die fuenf Ausgaenge', () => {
  it('200 mit JSON liefert ok samt Wert', async () => {
    gibZurueck(antwort(200, JSON.stringify({ id: 'abc' })))

    const result = await request<{ id: string }>('/api/etwas')

    expect(result).toEqual({ kind: 'ok', value: { id: 'abc' } })
  })

  it('204 liefert ok ohne Wert', async () => {
    gibZurueck(antwort(204))

    const result = await request('/api/etwas')

    expect(result.kind).toBe('ok')
  })

  it('404 liefert notFound', async () => {
    gibZurueck(antwort(404, 'Diese Aufgabe gibt es nicht.', 'text/plain'))

    const result = await request('/api/etwas')

    expect(result).toEqual({ kind: 'notFound', message: 'Diese Aufgabe gibt es nicht.' })
  })

  // 401 und 403 fallen bewusst zusammen: bei genau einer Rolle ist "darf nicht"
  // dasselbe wie "nicht angemeldet" - in beiden Fällen hilft nur die Anmeldung.
  it.each([401, 403])('%i liefert unauthorized', async (status) => {
    gibZurueck(antwort(status, '', 'text/plain'))

    const result = await request('/api/admin/etwas')

    expect(result.kind).toBe('unauthorized')
  })

  it('400 liefert rejected mit dem Satz des Servers im Wortlaut', async () => {
    gibZurueck(antwort(400, "'notiz.txt' ist keine .java-Datei.", 'text/plain'))

    const result = await request('/api/submissions')

    expect(result).toEqual({
      kind: 'rejected',
      message: "'notiz.txt' ist keine .java-Datei.",
    })
  })

  it('Fehler ohne Rumpf bekommt einen Ersatzsatz mit dem Statuscode', async () => {
    gibZurueck(antwort(500, '', 'text/plain'))

    const result = await request('/api/etwas')

    expect(result).toEqual({
      kind: 'rejected',
      message: 'Der Server hat mit 500 geantwortet.',
    })
  })

  it('Fehler mit nur Leerzeichen bekommt ebenfalls den Ersatzsatz', async () => {
    gibZurueck(antwort(400, '   ', 'text/plain'))

    const result = await request('/api/etwas')

    expect(result.kind).toBe('rejected')
    expect(result.kind !== 'ok' && result.message).toContain('400')
  })

  it('unlesbares JSON bei 200 liefert rejected statt eines Absturzes', async () => {
    gibZurueck(antwort(200, '{ das ist kein JSON'))

    const result = await request('/api/etwas')

    expect(result).toEqual({
      kind: 'rejected',
      message: 'Die Antwort des Servers war unlesbar.',
    })
  })

  // Der Fall, an dem das stillgelegte Blazor-Frontend gescheitert ist: ohne
  // eigenen Ausgang stand bei gestopptem Backend "Diese Aufgabe gibt es nicht
  // (mehr)" auf der Seite - die denkbar irreführendste Auskunft.
  it('ein Netzwerkfehler liefert unreachable, nicht notFound', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    const result = await request('/api/etwas')

    expect(result.kind).toBe('unreachable')
    expect(result.kind !== 'ok' && result.message).toContain('nicht erreichbar')
  })

  // Ein Abbruch ist kein Fehler des Servers. Er wird durchgereicht, damit der
  // Aufrufer ihn als das erkennt, was er ist - der Polling-Hook verlässt sich
  // darauf beim Aufräumen.
  it('ein Abbruch wird weitergeworfen statt verpackt', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockRejectedValue(new DOMException('The operation was aborted.', 'AbortError')),
    )

    await expect(request('/api/etwas')).rejects.toThrowError(DOMException)
  })
})

describe('request — was auf die Leitung geht', () => {
  it('verlangt text/plain VOR application/json', async () => {
    const spy = gibZurueck(antwort(200, '{}'))

    await request('/api/etwas')

    const kopf = spy.mock.calls[0][1].headers.Accept
    expect(kopf).toBe('text/plain, application/json')

    // Die Reihenfolge ist der ganze Punkt: ASP.NET wählt den ersten passenden
    // Formatter. Stand JSON vorn, kam die Ablehnung als »"'notiz.txt' ist
    // keine .java-Datei."« an - mit Anführungszeichen.
    expect(kopf.indexOf('text/plain')).toBeLessThan(kopf.indexOf('application/json'))
  })

  // Ohne "include" lässt der Browser das Anmelde-Cookie weg, weil Frontend und
  // API auf verschiedenen Ports liegen - jeder Admin-Endpunkt antwortet dann 401.
  it('schickt Anmeldedaten mit', async () => {
    const spy = gibZurueck(antwort(200, '{}'))

    await request('/api/etwas')

    expect(spy.mock.calls[0][1].credentials).toBe('include')
  })

  it('haengt den Pfad an die Basisadresse', async () => {
    const spy = gibZurueck(antwort(200, '{}'))

    await request('/api/etwas')

    expect(spy.mock.calls[0][0]).toBe(`${apiBaseUrl}/api/etwas`)
  })

  // Beim Hochladen muss der Browser den Content-Type selbst bilden, weil er die
  // multipart-Grenze enthält. Deshalb steht er in jsonRequest und nicht in
  // request - wer ihn dort pauschal setzt, macht jeden Upload kaputt.
  it('setzt ohne Rumpf keinen Content-Type', async () => {
    const spy = gibZurueck(antwort(200, '{}'))

    await request('/api/etwas', { method: 'POST', body: new FormData() })

    expect(spy.mock.calls[0][1].headers['Content-Type']).toBeUndefined()
  })
})

describe('jsonRequest', () => {
  it('serialisiert den Rumpf und setzt den Content-Type', async () => {
    const spy = gibZurueck(antwort(200, '{}'))

    await jsonRequest('/api/etwas', 'PUT', { titel: 'Bankkonto' })

    const optionen = spy.mock.calls[0][1]
    expect(optionen.method).toBe('PUT')
    expect(optionen.body).toBe('{"titel":"Bankkonto"}')
    expect(optionen.headers['Content-Type']).toBe('application/json')
  })

  it('schickt ohne Rumpf auch keinen Content-Type', async () => {
    const spy = gibZurueck(antwort(204))

    await jsonRequest('/api/etwas', 'POST')

    expect(spy.mock.calls[0][1].body).toBeUndefined()
    expect(spy.mock.calls[0][1].headers['Content-Type']).toBeUndefined()
  })
})
