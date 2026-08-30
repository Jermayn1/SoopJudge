import { describe, expect, it } from 'vitest'
import { falte, filterIcons, ICON_COUNT } from './icons'

// filterIcons ist genau die Funktion, die der IconPickerDialog benutzt - der
// Test prüft also den echten Weg und nicht einen Nachbau.
//
// Der Schwerpunkt liegt auf der Faltung: der Symbol-Index ist mit echten
// Umlauten geschrieben, gesucht wird aber auch in Ersatzschreibung. Ohne die
// Faltung liefe "pruefung" ins Leere, ohne dass irgendwo ein Fehler entsteht -
// die Suche fände einfach nichts mehr.
function namen(begriff: string): string[] {
  return filterIcons(begriff).flatMap((group) => group.eintraege.map((entry) => entry.name))
}

describe('falte', () => {
  it('faltet Umlaute auf ihre Ersatzschreibung und macht klein', () => {
    expect(falte('Prüfung')).toBe('pruefung')
    expect(falte('Größe')).toBe('groesse')
    expect(falte('GRÖSSE')).toBe('groesse')
  })

  it('laesst Text ohne Umlaute bis auf die Kleinschreibung unveraendert', () => {
    expect(falte('Repeat')).toBe('repeat')
  })
})

describe('filterIcons', () => {
  it('liefert bei leerem Begriff den vollen Bestand', () => {
    expect(namen('')).toHaveLength(ICON_COUNT)
    expect(namen('   ')).toHaveLength(ICON_COUNT)
  })

  it.each([
    ['prüfung', 'pruefung'],
    ['löschen', 'loeschen'],
    ['schlüssel', 'schluessel'],
    ['zurück', 'zurueck'],
    ['auswählen', 'auswaehlen'],
  ])('findet zu "%s" dieselben Symbole wie zu "%s"', (mitUmlaut, ersatz) => {
    const treffer = namen(mitUmlaut)

    expect(treffer.length).toBeGreaterThan(0)
    expect(namen(ersatz)).toEqual(treffer)
  })

  it('findet weiterhin ueber den englischen Namen', () => {
    expect(namen('Repeat')).toContain('Repeat')
  })

  it('liefert bei einem Begriff ohne Treffer keine leeren Gruppen', () => {
    expect(filterIcons('gibtesnicht')).toEqual([])
  })
})
