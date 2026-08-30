// Zustände eines Speichervorgangs. Vier, nicht zwei: ein Wahrheitswert könnte
// "läuft gerade" und "ist fehlgeschlagen" nicht auseinanderhalten.
//
// "saved" ist eigen: es ist kein Dauerzustand, sondern eine kurze Rückmeldung.
// Ohne ihn bleibt nach dem Speichern alles wie vorher stehen und man weiß
// nicht, ob der Klick angekommen ist.
export type SaveState =
  | { kind: 'idle' }
  | { kind: 'saving' }
  | { kind: 'saved' }
  | { kind: 'error'; message: string }
