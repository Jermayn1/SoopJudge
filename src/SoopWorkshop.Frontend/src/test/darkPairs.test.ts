import { describe, expect, it } from 'vitest'
import { checkClassString, checkSource } from './darkPairs'

// Alle Quelltexte der Anwendung, roh. Über ?raw statt node:fs, weil
// tsconfig.app.json die Node-Typen nicht kennt und tsc -b sonst scheitert.
const SOURCES = import.meta.glob<string>(
  ['/src/**/*.{ts,tsx}', '!/src/**/*.test.{ts,tsx}', '!/src/test/**', '!/src/api/schema.d.ts'],
  { query: '?raw', import: 'default', eager: true },
)

describe('checkClassString', () => {
  it('verlangt zu einer grauen Fläche ein dunkles Gegenstück', () => {
    expect(checkClassString('bg-white p-4')).toEqual([
      { token: 'bg-white', reason: 'kein dunkles Gegenstück' },
    ])
    expect(checkClassString('bg-white p-4 dark:bg-neutral-800')).toEqual([])
  })

  it('verlangt dieselben Varianten, in beliebiger Reihenfolge', () => {
    expect(checkClassString('hover:bg-slate-200 dark:bg-neutral-700')).toHaveLength(1)
    expect(checkClassString('group-hover:hover:bg-slate-200 dark:hover:group-hover:bg-neutral-700')).toEqual([])
  })

  it('findet [&_code]-Varianten trotz des Doppelpunkts in der Klammer', () => {
    expect(checkClassString('[&_:not(pre)>code]:bg-slate-100')).toHaveLength(1)
    expect(
      checkClassString('[&_:not(pre)>code]:bg-slate-100 dark:[&_:not(pre)>code]:bg-neutral-700'),
    ).toEqual([])
  })

  it('lässt kräftige Akzentflächen und weiße Schrift durch', () => {
    expect(checkClassString('bg-indigo-600 text-white hover:bg-indigo-700')).toEqual([])
    expect(checkClassString('from-emerald-600 to-emerald-800')).toEqual([])
  })

  it('verlangt bei Schrift auch für kräftige Akzente ein Gegenstück', () => {
    expect(checkClassString('text-indigo-600')).toHaveLength(1)
  })

  it('verlangt bei hellen Akzenttönen und farbigen Schatten ein Gegenstück', () => {
    expect(checkClassString('bg-rose-50 border-rose-200')).toHaveLength(2)
    expect(checkClassString('shadow-lg shadow-indigo-200')).toHaveLength(1)
  })

  it('zählt text-sm oder border-2 nicht als Farbe', () => {
    expect(checkClassString('text-sm border-2 ring-8 shadow-md')).toEqual([])
  })

  it('meldet dark: an späterer Stelle, weil das ungültiges CSS ergibt', () => {
    expect(checkClassString('backdrop:bg-slate-900/40 backdrop:dark:bg-black/60')).toContainEqual({
      token: 'backdrop:dark:bg-black/60',
      reason: 'dark: muss die erste Variante sein',
    })
  })
})

describe('checkSource', () => {
  it('prüft jeden Zweig eines Template-Strings für sich', () => {
    const source = "const c = `p-2 ${ok ? 'bg-emerald-50 dark:bg-emerald-500/10' : 'bg-rose-50'}`"
    expect(checkSource('x.tsx', source)).toEqual([
      { line: 1, token: 'bg-rose-50', reason: 'kein dunkles Gegenstück' },
    ])
  })

  it('meldet Klassen, die erst zur Laufzeit entstehen', () => {
    const source = 'const c = `p-2 bg-${farbe}-50`'
    expect(checkSource('x.tsx', source)[0]?.reason).toBe('Farbklasse wird zur Laufzeit zusammengesetzt')
  })

  it('lässt zusammengesetzte Ids und Adressen in Ruhe', () => {
    const source = 'const a = `kategorie-detail-${id}`; const b = `/aufgaben/${id}`'
    expect(checkSource('x.tsx', source)).toEqual([])
  })
})

describe('Farbklassen im Bestand', () => {
  it('findet überhaupt Quelltexte', () => {
    expect(Object.keys(SOURCES).length).toBeGreaterThan(30)
  })

  it.each(Object.keys(SOURCES))('%s', (path) => {
    const findings = checkSource(path, SOURCES[path])

    expect(findings.map((f) => `Zeile ${f.line}: ${f.token} (${f.reason})`)).toEqual([])
  })
})
