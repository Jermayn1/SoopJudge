import { afterEach, describe, expect, it, vi } from 'vitest'
import indexHtml from '../../index.html?raw'
import { applyTheme, getTheme, resolveTheme, setTheme, STORAGE_KEY, subscribe } from './theme'
import { listenerCount, setSystemDark } from '../test/matchMedia'

const DARK_QUERY = '(prefers-color-scheme: dark)'

afterEach(() => {
  vi.restoreAllMocks()
})

describe('resolveTheme', () => {
  it('nimmt die gespeicherte Wahl vor dem System', () => {
    expect(resolveTheme('light', true)).toBe('light')
    expect(resolveTheme('dark', false)).toBe('dark')
  })

  it('folgt dem System, wenn nichts oder Unsinn gespeichert ist', () => {
    expect(resolveTheme(null, true)).toBe('dark')
    expect(resolveTheme(null, false)).toBe('light')
    expect(resolveTheme('blau', true)).toBe('dark')
  })
})

describe('applyTheme und setTheme', () => {
  it('setzt Klasse und color-scheme auf <html>', () => {
    applyTheme('dark')

    expect(document.documentElement).toHaveClass('dark')
    expect(document.documentElement.style.colorScheme).toBe('dark')
    expect(getTheme()).toBe('dark')

    applyTheme('light')

    expect(document.documentElement).not.toHaveClass('dark')
    expect(document.documentElement.style.colorScheme).toBe('light')
  })

  it('lässt die Klasse gegen Übergänge nicht stehen', () => {
    applyTheme('dark')

    expect(document.documentElement).not.toHaveClass('theme-wechsel')
  })

  it('merkt sich die Wahl', () => {
    setTheme('dark')

    expect(localStorage.getItem(STORAGE_KEY)).toBe('dark')
  })

  it('schaltet trotzdem um, wenn sich nichts speichern lässt, und sagt es', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('gesperrt', 'SecurityError')
    })
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})

    setTheme('dark')

    expect(getTheme()).toBe('dark')
    expect(warn).toHaveBeenCalledOnce()
  })
})

describe('subscribe', () => {
  it('folgt dem System, solange nichts gespeichert ist', () => {
    const unsubscribe = subscribe(() => {})

    setSystemDark(true)
    expect(getTheme()).toBe('dark')

    setSystemDark(false)
    expect(getTheme()).toBe('light')

    unsubscribe()
  })

  it('bleibt bei der eigenen Wahl, wenn das System wechselt', () => {
    const unsubscribe = subscribe(() => {})
    setTheme('light')

    setSystemDark(true)

    expect(getTheme()).toBe('light')
    unsubscribe()
  })

  it('übernimmt die Wahl aus einem anderen Tab', () => {
    const listener = vi.fn()
    const unsubscribe = subscribe(listener)

    localStorage.setItem(STORAGE_KEY, 'dark')
    window.dispatchEvent(new StorageEvent('storage', { key: STORAGE_KEY, newValue: 'dark' }))

    expect(getTheme()).toBe('dark')
    expect(listener).toHaveBeenCalled()
    unsubscribe()
  })

  it('fällt aufs System zurück, wenn ein anderer Tab den Speicher leert', () => {
    const unsubscribe = subscribe(() => {})
    applyTheme('dark')

    window.dispatchEvent(new StorageEvent('storage', { key: null }))

    expect(getTheme()).toBe('light')
    unsubscribe()
  })

  it('hängt die Listener nur einmal an, egal wie viele abonnieren', () => {
    const first = subscribe(() => {})
    const second = subscribe(() => {})

    expect(listenerCount(DARK_QUERY)).toBe(1)

    first()
    expect(listenerCount(DARK_QUERY)).toBe(1)

    second()
    expect(listenerCount(DARK_QUERY)).toBe(0)
  })
})

// Das Skript im <head> entscheidet vor React, theme.ts danach. Entscheiden
// die beiden verschieden, springt die Seite beim Laden einmal um.
describe('Skript im <head> von index.html', () => {
  const script = /<script id="theme-vorab">([\s\S]*?)<\/script>/.exec(indexHtml)?.[1]

  function runScript(stored: string | null | 'wirft', systemDark: boolean) {
    const classes = new Set<string>()
    const root = {
      classList: { toggle: (name: string, on: boolean) => (on ? classes.add(name) : classes.delete(name)) },
      style: { colorScheme: '' },
    }
    const storage = {
      getItem: () => {
        if (stored === 'wirft') throw new DOMException('gesperrt', 'SecurityError')
        return stored
      },
    }
    const quiet = { warn: () => {} }

    new Function('localStorage', 'matchMedia', 'document', 'console', script ?? '')(
      storage,
      () => ({ matches: systemDark }),
      { documentElement: root },
      quiet,
    )

    return { dark: classes.has('dark'), colorScheme: root.style.colorScheme }
  }

  it('steht in index.html und nennt denselben Schlüssel', () => {
    expect(script).toBeDefined()
    expect(script).toContain(`'${STORAGE_KEY}'`)
  })

  it.each([
    ['light', false],
    ['light', true],
    ['dark', false],
    ['dark', true],
    ['blau', false],
    ['blau', true],
    [null, false],
    [null, true],
  ] as const)('entscheidet bei gespeichert %s und System dunkel %s wie resolveTheme', (stored, systemDark) => {
    const expected = resolveTheme(stored, systemDark)

    expect(runScript(stored, systemDark)).toEqual({ dark: expected === 'dark', colorScheme: expected })
  })

  it.each([false, true])('folgt dem System, wenn der Speicher wirft (System dunkel %s)', (systemDark) => {
    expect(runScript('wirft', systemDark).dark).toBe(systemDark)
  })
})
