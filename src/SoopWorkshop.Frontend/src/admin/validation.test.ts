import { describe, expect, it } from 'vitest'
import {
  FIELD_LIMITS,
  checkJavaFileName,
  checkMaxLength,
  checkOrder,
  checkRequired,
  collect,
} from './validation'

describe('checkRequired', () => {
  it('laesst einen ausgefuellten Wert durch', () => {
    expect(checkRequired('name', 'Der Name', 'Bankkonto')).toBeNull()
  })

  it.each(['', '   ', '\t\n'])('beanstandet %j', (wert) => {
    expect(checkRequired('name', 'Der Name', wert)).toEqual({
      field: 'name',
      message: 'Der Name darf nicht leer sein.',
    })
  })
})

describe('checkMaxLength', () => {
  it('laesst genau die Grenze durch', () => {
    expect(checkMaxLength('titel', 'Der Titel', 'x'.repeat(200), 200)).toBeNull()
  })

  // Die Meldung nennt beide Zahlen. Ein bloßes "zu lang" zwingt den Betreuer
  // zum Nachzählen.
  it('nennt bei Ueberschreitung Ist und Soll', () => {
    const problem = checkMaxLength('titel', 'Der Titel', 'x'.repeat(201), 200)

    expect(problem?.message).toBe('Der Titel ist 201 Zeichen lang — erlaubt sind 200.')
  })
})

describe('checkOrder', () => {
  it.each([0, 1, 99])('laesst %i durch', (wert) => {
    expect(checkOrder('order', wert)).toBeNull()
  })

  it.each([-1, 1.5, Number.NaN])('beanstandet %j', (wert) => {
    expect(checkOrder('order', wert)).not.toBeNull()
  })
})

describe('checkJavaFileName', () => {
  it('nimmt einen gueltigen Namen an', () => {
    expect(checkJavaFileName('datei', 'KontoTest.java')).toBeNull()
  })

  it('nimmt die Endung auch in Grossbuchstaben', () => {
    expect(checkJavaFileName('datei', 'KontoTest.JAVA')).toBeNull()
  })

  it('beanstandet eine leere Angabe', () => {
    expect(checkJavaFileName('datei', '  ')?.message).toContain('nicht leer')
  })

  it('beanstandet eine falsche Endung', () => {
    expect(checkJavaFileName('datei', 'notiz.txt')?.message).toContain('.java')
  })

  // Der Name wird später zu einer echten Datei im Arbeitsverzeichnis.
  it.each(['unter/Test.java', 'unter\\Test.java', '../Test.java'])(
    'beanstandet den Pfadanteil in %s',
    (name) => {
      expect(checkJavaFileName('datei', name)?.message).toContain('Pfadanteil')
    },
  )

  it('beanstandet einen zu langen Namen', () => {
    const name = `${'x'.repeat(FIELD_LIMITS.unitTestFileName)}.java`

    expect(checkJavaFileName('datei', name)?.message).toContain('erlaubt sind')
  })
})

describe('collect', () => {
  // Bewusst alle Verstöße auf einmal: wer ein Formular abschickt, will nicht
  // viermal hintereinander einen neuen Fehler entdecken.
  it('sammelt alle Verstoesse und laesst die bestandenen weg', () => {
    const problems = collect(
      checkRequired('name', 'Der Name', ''),
      checkRequired('titel', 'Der Titel', 'Bankkonto'),
      checkOrder('order', -1),
    )

    expect(problems.map((p) => p.field)).toEqual(['name', 'order'])
  })

  it('liefert bei lauter gueltigen Feldern eine leere Liste', () => {
    expect(collect(null, null)).toEqual([])
  })
})

// Die Grenze für die Testfallbeschreibung ist bewusst die kleinere der beiden:
// das Request-DTO erlaubt 2000, die Datenbankspalte nur 500. Wer sich auf das
// DTO verlässt, kommt durch die Validierung und knallt in der Datenbank.
describe('FIELD_LIMITS', () => {
  it('nimmt fuer die Testfallbeschreibung die wahre Spaltengrenze', () => {
    expect(FIELD_LIMITS.testDescription).toBe(500)
  })
})
