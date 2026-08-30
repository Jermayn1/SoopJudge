// Spiegelt die DataAnnotations aus SoopWorkshop.Shared/DTOs/Tasks/Requests/
// und die Spaltengrenzen aus den EF-Konfigurationen.
//
// Doppelt gepflegt, weil das Frontend nicht mehr dieselbe Assembly benutzt —
// dasselbe Muster wie api/uploadLimits.ts. Das Frontend blockt früh und
// begründet, das Backend prüft verbindlich. Wer hier etwas ändert, ändert
// es dort mit.
export const FIELD_LIMITS = {
  categoryName: 100,
  taskTitle: 200,
  expectedClassName: 200,
  expectedMethodSignature: 500,
  unitTestFileName: 255,

  // Das Request-DTO erlaubt hier noch 2000, die Datenbankspalte aber nur 500
  // (TaskTestConfiguration). Eine Beschreibung zwischen 501 und 2000 Zeichen
  // kommt also durch die Validierung und knallt danach in der Datenbank.
  // Hier gilt die kleinere, wahre Grenze; das DTO wird in 5.2 nachgezogen.
  testDescription: 500,
} as const

export type FieldProblem = { field: string; message: string }

// Die Prüfungen liefern fertige deutsche Sätze, keine Fehlercodes — genauso
// wie checkFiles in uploadLimits.ts. Der Satz steht dann direkt am Feld.
export function checkRequired(field: string, label: string, value: string): FieldProblem | null {
  return value.trim().length === 0 ? { field, message: `${label} darf nicht leer sein.` } : null
}

export function checkMaxLength(
  field: string,
  label: string,
  value: string,
  max: number,
): FieldProblem | null {
  if (value.length <= max) return null

  return {
    field,
    message: `${label} ist ${value.length} Zeichen lang — erlaubt sind ${max}.`,
  }
}

export function checkOrder(field: string, value: number): FieldProblem | null {
  if (Number.isInteger(value) && value >= 0) return null
  return { field, message: 'Die Reihenfolge muss eine ganze Zahl ab 0 sein.' }
}

// Regeln aus TaskUnitTestFileService.ValidateFileName. Pfadanteile sind dort
// verboten, weil der Name später zu einem Dateinamen im Arbeitsverzeichnis
// wird — ein ".." darin wäre ein Ausbruch daraus.
export function checkJavaFileName(field: string, value: string): FieldProblem | null {
  const name = value.trim()

  if (name.length === 0) return { field, message: 'Der Dateiname darf nicht leer sein.' }

  if (!name.toLowerCase().endsWith('.java'))
    return { field, message: `'${name}' muss auf .java enden.` }

  if (name.includes('/') || name.includes('\\') || name.includes('..'))
    return { field, message: `'${name}' darf keinen Pfadanteil enthalten.` }

  return checkMaxLength(field, 'Der Dateiname', name, FIELD_LIMITS.unitTestFileName)
}

// Sammelt die Verstöße einer Maske. Bewusst alle auf einmal statt nur des
// ersten: wer ein Formular abschickt, will nicht viermal hintereinander einen
// neuen Fehler entdecken.
export function collect(...problems: (FieldProblem | null)[]): FieldProblem[] {
  return problems.filter((problem): problem is FieldProblem => problem !== null)
}
