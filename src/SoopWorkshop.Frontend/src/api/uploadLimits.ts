// Spiegelt SoopWorkshop.Shared/Constants/SubmissionUploadLimits.cs.
//
// Doppelt gepflegt, weil das Frontend nicht mehr dieselbe Assembly benutzt.
// Das Frontend blockt früh und begründet, das Backend prüft verbindlich —
// wer hier etwas ändert, ändert es dort mit.
export const UPLOAD_LIMITS = {
  allowedExtension: '.java',
  maxFileCount: 10,
  maxFileSizeBytes: 1024 * 1024,
  maxTotalSizeBytes: 10 * 1024 * 1024,
} as const

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1).replace('.', ',')} MB`
}

// Prüft eine Auswahl gegen die Grenzen und liefert die Dateien, die
// übernommen werden, samt Begründung für jede verworfene.
//
// Eine verworfene Datei darf nicht kommentarlos verschwinden — im
// Referenzprojekt kam an dieser Stelle ein alert() mit einem Satz für alle
// Fälle, und mehrere Dateien waren gar nicht vorgesehen.
export function checkFiles(
  existing: File[],
  incoming: File[],
): { accepted: File[]; rejections: string[] } {
  const accepted: File[] = [...existing]
  const rejections: string[] = []

  for (const file of incoming) {
    if (!file.name.toLowerCase().endsWith(UPLOAD_LIMITS.allowedExtension)) {
      rejections.push(`„${file.name}“ ist keine ${UPLOAD_LIMITS.allowedExtension}-Datei.`)
      continue
    }

    // Groß- und Kleinschreibung zählt hier NICHT, weil sie es im Backend
    // nicht tut (SubmissionUploadValidator vergleicht mit OrdinalIgnoreCase).
    // Vorher ließ der Browser 'A.java' neben 'a.java' durch, und erst der
    // Server lehnte ab - der Teilnehmer bekam eine Ablehnung für etwas, das
    // die Oberfläche eben noch angenommen hatte. Bei Namensgleichheit
    // überschreibt javac außerdem die eine Klasse mit der anderen.
    if (accepted.some((f) => f.name.toLowerCase() === file.name.toLowerCase())) {
      rejections.push(`„${file.name}“ ist bereits ausgewählt.`)
      continue
    }

    if (file.size === 0) {
      rejections.push(`„${file.name}“ ist leer.`)
      continue
    }

    if (file.size > UPLOAD_LIMITS.maxFileSizeBytes) {
      // Wortgleich mit SubmissionUploadValidator.cs — der Teilnehmer soll
      // denselben Satz lesen, egal ob der Browser oder der Server ablehnt.
      //
      // Hier stand vorher die tatsächliche Größe neben der erlaubten. Das
      // klang präziser, war aber bei knappen Überschreitungen falsch: 1 MB
      // plus 10 Bytes rundet auf "1,0 MB", die Grenze ebenso — herauskam
      // "ist 1,0 MB groß — erlaubt sind 1,0 MB je Datei". Der Test prüft mit
      // 2 MB und hat das nie gesehen.
      rejections.push(
        `„${file.name}“ ist größer als ${UPLOAD_LIMITS.maxFileSizeBytes / 1024} KB.`,
      )
      continue
    }

    if (accepted.length >= UPLOAD_LIMITS.maxFileCount) {
      rejections.push(
        `„${file.name}“ passt nicht mehr dazu — es sind höchstens ${UPLOAD_LIMITS.maxFileCount} Dateien erlaubt.`,
      )
      continue
    }

    const total = accepted.reduce((sum, f) => sum + f.size, 0) + file.size
    if (total > UPLOAD_LIMITS.maxTotalSizeBytes) {
      rejections.push(
        `„${file.name}“ sprengt die Gesamtgröße von ${formatBytes(UPLOAD_LIMITS.maxTotalSizeBytes)}.`,
      )
      continue
    }

    accepted.push(file)
  }

  return { accepted, rejections }
}
