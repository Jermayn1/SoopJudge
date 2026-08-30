import { describe, expect, it } from 'vitest'
import { UPLOAD_LIMITS, checkFiles, formatBytes } from './uploadLimits'

// File-Objekte mit echtem Inhalt zu bauen wird bei 1 MB teuer. Die Größe
// wird deshalb aufgesetzt - checkFiles liest ohnehin nur file.size.
function datei(name: string, size = 100): File {
  const file = new File(['x'], name)
  Object.defineProperty(file, 'size', { value: size })
  return file
}

describe('formatBytes', () => {
  it.each([
    [512, '512 B'],
    [2048, '2 KB'],
    [1024 * 1024, '1,0 MB'],
    [10 * 1024 * 1024, '10,0 MB'],
  ])('%i Bytes werden als %s ausgegeben', (bytes, erwartet) => {
    expect(formatBytes(bytes)).toBe(erwartet)
  })

  // Deutsches Dezimalkomma, nicht der Punkt aus toFixed.
  it('schreibt Nachkommastellen mit Komma', () => {
    expect(formatBytes(1.5 * 1024 * 1024)).toBe('1,5 MB')
  })
})

describe('checkFiles', () => {
  it('nimmt eine gueltige .java-Datei an', () => {
    const { accepted, rejections } = checkFiles([], [datei('Main.java')])

    expect(accepted.map((f) => f.name)).toEqual(['Main.java'])
    expect(rejections).toEqual([])
  })

  it('behaelt die bereits ausgewaehlten Dateien', () => {
    const { accepted } = checkFiles([datei('Konto.java')], [datei('Kunde.java')])

    expect(accepted.map((f) => f.name)).toEqual(['Konto.java', 'Kunde.java'])
  })

  it('nennt bei jeder verworfenen Datei den Grund', () => {
    const { accepted, rejections } = checkFiles([], [datei('notiz.txt')])

    expect(accepted).toEqual([])
    expect(rejections).toEqual(["„notiz.txt“ ist keine .java-Datei."])
  })

  // Die Prüfreihenfolge ist selbst eine Zusicherung. Eine 2 MB große .txt
  // verstößt gegen zwei Regeln; genannt werden muss die Endung, denn die ist
  // der eigentliche Grund - die Größe wäre eine irreführende Auskunft.
  it('meldet bei mehreren Verstoessen den erstgepruefen', () => {
    const { rejections } = checkFiles([], [datei('notiz.txt', 2 * 1024 * 1024)])

    expect(rejections).toEqual(["„notiz.txt“ ist keine .java-Datei."])
  })

  it('erlaubt die Endung auch in Grossbuchstaben', () => {
    const { accepted } = checkFiles([], [datei('Main.JAVA')])

    expect(accepted).toHaveLength(1)
  })

  it('erkennt ein Duplikat auch gegen die bereits ausgewaehlten', () => {
    const { accepted, rejections } = checkFiles([datei('Main.java')], [datei('Main.java')])

    expect(accepted).toHaveLength(1)
    expect(rejections).toEqual(["„Main.java“ ist bereits ausgewählt."])
  })

  it('verwirft eine leere Datei', () => {
    const { rejections } = checkFiles([], [datei('Leer.java', 0)])

    expect(rejections).toEqual(["„Leer.java“ ist leer."])
  })

  it('verwirft eine zu grosse Datei und nennt die Grenze', () => {
    const { accepted, rejections } = checkFiles([], [datei('Gross.java', 2 * 1024 * 1024)])

    expect(accepted).toEqual([])
    expect(rejections).toEqual(['„Gross.java“ ist größer als 1024 KB.'])
  })

  // Knapp über der Grenze: früher stand hier die gerundete Ist-Größe neben
  // der gerundeten Grenze, und beide lauteten "1,0 MB" - die Meldung
  // widersprach sich selbst. Der Wortlaut nennt jetzt nur noch die Grenze und
  // ist damit wortgleich mit dem Server (SubmissionUploadValidator.cs).
  it('bleibt auch knapp ueber der Grenze widerspruchsfrei', () => {
    const { rejections } = checkFiles([], [datei('Knapp.java', 1024 * 1024 + 10)])

    expect(rejections).toEqual(['„Knapp.java“ ist größer als 1024 KB.'])
  })

  it('nimmt hoechstens zehn Dateien', () => {
    const vorhandene = Array.from({ length: UPLOAD_LIMITS.maxFileCount }, (_, i) =>
      datei(`Datei${i}.java`),
    )

    const { accepted, rejections } = checkFiles(vorhandene, [datei('Zuviel.java')])

    expect(accepted).toHaveLength(UPLOAD_LIMITS.maxFileCount)
    expect(rejections[0]).toContain('höchstens 10 Dateien')
  })

  it('achtet auf die Gesamtgroesse ueber alle Dateien', () => {
    const grosse = [datei('A.java', 900 * 1024), datei('B.java', 900 * 1024)]
    const noch = Array.from({ length: 8 }, (_, i) => datei(`C${i}.java`, 900 * 1024))

    const { rejections } = checkFiles([...grosse, ...noch], [datei('D.java', 900 * 1024)])

    expect(rejections).toHaveLength(1)
  })

  it('verwirft nur die schlechten und behaelt die guten aus derselben Auswahl', () => {
    const { accepted, rejections } = checkFiles(
      [],
      [datei('Gut.java'), datei('schlecht.txt'), datei('AuchGut.java')],
    )

    expect(accepted.map((f) => f.name)).toEqual(['Gut.java', 'AuchGut.java'])
    expect(rejections).toHaveLength(1)
  })

  // Der Vergleich läuft ohne Rücksicht auf Groß- und Kleinschreibung, genau wie
  // im SubmissionUploadValidator des Backends. Verglichen das Frontend bitgenau,
  // käme 'A.java' neben 'a.java' durch die Oberfläche und würde erst vom Server
  // abgelehnt.
  //
  // Fachlich richtig ist die unempfindliche Variante: javac überschreibt bei
  // gleichem Namen die eine Klasse mit der anderen.
  it('haelt A.java und a.java fuer dieselbe Datei', () => {
    const { accepted, rejections } = checkFiles([datei('A.java')], [datei('a.java')])

    expect(accepted.map((f) => f.name)).toEqual(['A.java'])
    expect(rejections).toEqual(["„a.java“ ist bereits ausgewählt."])
  })
})
