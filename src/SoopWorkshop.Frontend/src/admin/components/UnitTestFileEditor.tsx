import { useRef, useState } from 'react'
import { ChevronDown, FileCode2, FilePlus2, Trash2, Upload } from 'lucide-react'
import { OrderButtons } from './OrderButtons'
import { LineNumberedEditor } from './LineNumberedEditor'
import { Checkbox } from './Checkbox'
import { inputClass } from './formStyles'
import { checkJavaFileName } from '../validation'
import { JUNIT_TEMPLATES } from '../junitTemplates'
import type { UnitTestFileDraft } from '../api/tasks'

type UnitTestFileEditorProps = {
  files: UnitTestFileDraft[]
  onChange: (files: UnitTestFileDraft[]) => void
}

export function UnitTestFileEditor({ files, onChange }: UnitTestFileEditorProps) {
  // Aufgeklappt wird gemerkt, nicht eingeklappt: eine neu angelegte Datei soll
  // offen sein, sonst tippt man ins Nichts.
  const [offen, setOffen] = useState<Set<number>>(new Set([0]))
  const [vorlagenOffen, setVorlagenOffen] = useState(false)
  const [hinweis, setHinweis] = useState<string | null>(null)
  const dateiRef = useRef<HTMLInputElement>(null)

  const oeffnen = (index: number) =>
    setOffen((current) => {
      const next = new Set(current)
      next.add(index)
      return next
    })

  const umschalten = (index: number) =>
    setOffen((current) => {
      const next = new Set(current)
      if (next.has(index)) next.delete(index)
      else next.add(index)
      return next
    })

  const ergaenzen = (datei: UnitTestFileDraft) => {
    onChange([...files, datei])
    oeffnen(files.length)
    setHinweis(null)
  }

  const replace = (index: number, patch: Partial<UnitTestFileDraft>) =>
    onChange(files.map((file, i) => (i === index ? { ...file, ...patch } : file)))

  const remove = (index: number) => onChange(files.filter((_, i) => i !== index))

  const move = (index: number, direction: -1 | 1) => {
    const target = index + direction
    if (target < 0 || target >= files.length) return

    const next = [...files]
    ;[next[index], next[target]] = [next[target], next[index]]
    onChange(next)
  }

  // Datei einlesen statt hochladen: der Inhalt landet im Formular und wird mit
  // allem anderen zusammen gespeichert. Dafür braucht es keinen Endpunkt.
  const einlesen = async (auswahl: FileList | null) => {
    if (!auswahl || auswahl.length === 0) return

    const angenommen: UnitTestFileDraft[] = []
    const abgelehnt: string[] = []

    for (const datei of Array.from(auswahl)) {
      const problem = checkJavaFileName('datei', datei.name)
      if (problem) {
        abgelehnt.push(problem.message)
        continue
      }

      // Ausdrücklich UTF-8. Ohne Angabe rät der Browser, und ein Umlaut im
      // @DisplayName käme zerlegt in der Datenbank an.
      const inhalt = await datei.text()
      angenommen.push({ fileName: datei.name, content: inhalt, isVisibleToParticipant: false })
    }

    if (angenommen.length > 0) {
      onChange([...files, ...angenommen])
      angenommen.forEach((_, i) => oeffnen(files.length + i))
    }

    setHinweis(abgelehnt.length > 0 ? abgelehnt.join(' ') : null)
  }

  return (
    <div>
      {hinweis && (
        <p
          role="alert"
          className="mb-4 rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900"
        >
          {hinweis}
        </p>
      )}

      <ul className="space-y-4">
        {files.map((file, index) => {
          const problem = checkJavaFileName('datei', file.fileName)
          const istOffen = offen.has(index)
          const regionId = `junit-datei-${index}`

          return (
            <li key={index} className="rounded-xl border border-slate-200 bg-slate-50">
              <div className="flex flex-wrap items-center gap-2 p-4">
                <OrderButtons
                  label={`Datei ${index + 1}`}
                  onUp={() => move(index, -1)}
                  onDown={() => move(index, 1)}
                  canMoveUp={index > 0}
                  canMoveDown={index < files.length - 1}
                />

                <FileCode2 className="w-4 h-4 shrink-0 text-slate-500" aria-hidden="true" />

                <input
                  type="text"
                  value={file.fileName}
                  onChange={(event) => replace(index, { fileName: event.target.value })}
                  aria-label={`Dateiname der ${index + 1}. JUnit-Datei`}
                  aria-invalid={problem !== null}
                  placeholder="MainTest.java"
                  className={`${inputClass(problem !== null)} min-w-48 flex-1 font-mono text-sm`}
                />

                <button
                  type="button"
                  onClick={() => umschalten(index)}
                  aria-expanded={istOffen}
                  aria-controls={regionId}
                  className="flex shrink-0 items-center gap-1 rounded-lg px-2 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-200"
                >
                  <span
                    className={`transition-transform duration-200 ${istOffen ? '' : '-rotate-90'}`}
                  >
                    <ChevronDown className="w-4 h-4" aria-hidden="true" />
                  </span>
                  Code
                </button>

                <button
                  type="button"
                  onClick={() => remove(index)}
                  aria-label={`${file.fileName || `Datei ${index + 1}`} entfernen`}
                  className="shrink-0 rounded-lg p-2 text-rose-800 hover:bg-rose-100"
                >
                  <Trash2 className="w-4 h-4" aria-hidden="true" />
                </button>
              </div>

              {problem && (
                <p role="alert" className="px-4 pb-3 text-sm font-medium text-rose-800">
                  {problem.message}
                </p>
              )}

              <div id={regionId} className={`klapp ${istOffen ? '' : 'klapp-zu'}`} inert={!istOffen}>
                <div className="klapp-inhalt">
                  <div className="space-y-3 px-4 pb-4">
                    <Checkbox
                      label="Für Teilnehmer sichtbar"
                      hint="Sichtbare Testdateien nehmen dem Teilnehmer das Raten. Bei Aufgaben, in denen das Finden der Fälle zur Übung gehört, besser aus lassen."
                      checked={file.isVisibleToParticipant}
                      onChange={(checked) => replace(index, { isVisibleToParticipant: checked })}
                    />

                    <LineNumberedEditor
                      label="Inhalt der Testdatei"
                      value={file.content}
                      onChange={(content) => replace(index, { content })}
                    />
                  </div>
                </div>
              </div>
            </li>
          )
        })}
      </ul>

      {files.length === 0 && (
        <p className="rounded-xl border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
          Noch keine JUnit-Datei hinterlegt.
        </p>
      )}

      <div className="mt-4 flex flex-wrap gap-2">
        <button
          type="button"
          onClick={() =>
            ergaenzen({ fileName: '', content: '', isVisibleToParticipant: false })
          }
          className="flex items-center gap-1.5 rounded-xl border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100"
        >
          <FilePlus2 className="w-4 h-4" aria-hidden="true" />
          Leere Datei
        </button>

        <button
          type="button"
          onClick={() => dateiRef.current?.click()}
          className="flex items-center gap-1.5 rounded-xl border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100"
        >
          <Upload className="w-4 h-4" aria-hidden="true" />
          .java hochladen
        </button>

        {/* Ein echter Knopf, der das versteckte Feld anstößt — nicht ein
            onClick auf einem div wie in der Dropzone der Teilnehmerseite. Der
            wäre mit der Tastatur nicht erreichbar. */}
        <input
          ref={dateiRef}
          type="file"
          accept=".java"
          multiple
          className="hidden"
          onChange={(event) => {
            void einlesen(event.target.files)
            // Zurücksetzen, damit dieselbe Datei erneut gewählt werden kann.
            event.target.value = ''
          }}
        />

        <div className="relative">
          <button
            type="button"
            onClick={() => setVorlagenOffen((offen) => !offen)}
            aria-expanded={vorlagenOffen}
            className="flex items-center gap-1.5 rounded-xl border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100"
          >
            <ChevronDown className="w-4 h-4" aria-hidden="true" />
            Aus Vorlage
          </button>

          {vorlagenOffen && (
            <div className="absolute left-0 top-full z-10 mt-1 w-80 rounded-xl border border-slate-200 bg-white p-2 shadow-lg">
              {JUNIT_TEMPLATES.map((template) => (
                <button
                  key={template.id}
                  type="button"
                  onClick={() => {
                    ergaenzen({
                      fileName: template.dateiname,
                      content: template.inhalt,
                      isVisibleToParticipant: false,
                    })
                    setVorlagenOffen(false)
                  }}
                  className="block w-full rounded-lg px-3 py-2 text-left transition-colors hover:bg-slate-100"
                >
                  <span className="block text-sm font-semibold text-slate-800">
                    {template.titel}
                  </span>
                  <span className="block text-xs text-slate-500">{template.wofuer}</span>
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
