import { useRef, useState } from 'react'
import { AlertTriangle, Check, Download, FileJson, Loader2, Upload } from 'lucide-react'
import { useAdminCatalog } from '../adminOutlet'
import { ConfirmDialog } from '../components/ConfirmDialog'
import {
  fetchBundle,
  offerDownload,
  previewImport,
  runImport,
  type ImportMode,
  type ImportReport,
  type TaskBundle,
} from '../api/transfer'

type Phase =
  | { kind: 'idle' }
  | { kind: 'busy'; was: string }
  | { kind: 'preview'; bundle: TaskBundle; report: ImportReport }
  | { kind: 'done'; report: ImportReport }
  | { kind: 'error'; message: string }

function Zahl({ wert, label }: { wert: number; label: string }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white px-4 py-3">
      <div className="text-2xl font-bold tabular-nums text-slate-800">{wert}</div>
      <div className="text-xs text-slate-500">{label}</div>
    </div>
  )
}

function Bericht({ report }: { report: ImportReport }) {
  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
      <Zahl wert={report.categoriesCreated} label="Kategorien neu" />
      <Zahl wert={report.categoriesUpdated} label="Kategorien aktualisiert" />
      <Zahl wert={report.categoriesDeleted} label="Kategorien gelöscht" />
      <Zahl wert={report.tasksCreated} label="Aufgaben neu" />
      <Zahl wert={report.tasksUpdated} label="Aufgaben aktualisiert" />
      <Zahl wert={report.tasksDeleted} label="Aufgaben gelöscht" />
    </div>
  )
}

export function TransferPage() {
  const { categories, reload } = useAdminCatalog()

  const [mode, setMode] = useState<ImportMode>('Merge')
  const [phase, setPhase] = useState<Phase>({ kind: 'idle' })
  const [bestaetigen, setBestaetigen] = useState(false)
  const dateiRef = useRef<HTMLInputElement>(null)

  const aufgabenImBestand = categories.reduce((sum, c) => sum + c.tasks.length, 0)

  const exportieren = async () => {
    setPhase({ kind: 'busy', was: 'Bestand wird zusammengestellt …' })

    const result = await fetchBundle()

    if (result.kind !== 'ok') {
      setPhase({ kind: 'error', message: result.message })
      return
    }

    offerDownload(result.value)
    setPhase({ kind: 'idle' })
  }

  const dateiLesen = async (auswahl: FileList | null) => {
    const datei = auswahl?.[0]
    if (!datei) return

    setPhase({ kind: 'busy', was: 'Datei wird gelesen …' })

    let bundle: TaskBundle

    try {
      // Clientseitig geparst: ein kaputtes JSON fällt hier mit der Meldung des
      // Browsers auf, statt als 400 aus dem Modelbinder zu kommen.
      bundle = JSON.parse(await datei.text()) as TaskBundle
    } catch (cause) {
      setPhase({
        kind: 'error',
        message: `'${datei.name}' ist keine lesbare JSON-Datei. ${cause instanceof Error ? cause.message : ''}`,
      })
      return
    }

    const result = await previewImport(bundle, mode)

    if (result.kind !== 'ok') {
      setPhase({ kind: 'error', message: result.message })
      return
    }

    setPhase({ kind: 'preview', bundle, report: result.value })
  }

  const importieren = async () => {
    if (phase.kind !== 'preview') return

    setBestaetigen(false)
    setPhase({ kind: 'busy', was: 'Bestand wird eingespielt …' })

    const result = await runImport(phase.bundle, mode)

    if (result.kind !== 'ok') {
      setPhase({ kind: 'error', message: result.message })
      return
    }

    reload()
    setPhase({ kind: 'done', report: result.value })
  }

  const preview = phase.kind === 'preview' ? phase.report : null
  const kannImportieren = preview !== null && preview.errors.length === 0

  return (
    <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
      <div className="mx-auto w-full max-w-3xl space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-800">Bestand übertragen</h1>
          <p className="mt-1 text-slate-600">
            Der ganze Aufgabenbestand als eine Datei — gedacht, um ihn hier zu pflegen und auf
            dem Server einzuspielen. Abgaben und Auswertungen sind nicht enthalten.
          </p>
        </div>

        {phase.kind === 'error' && (
          <div
            role="alert"
            className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-rose-800"
          >
            {phase.message}
          </div>
        )}

        {/* Export */}
        <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <h2 className="text-lg font-bold text-slate-800">Herunterladen</h2>
          <p className="mt-1 text-sm text-slate-600">
            {categories.length === 1 ? '1 Kategorie' : `${categories.length} Kategorien`} mit{' '}
            <span className="tabular-nums">{aufgabenImBestand}</span>{' '}
            {aufgabenImBestand === 1 ? 'Aufgabe' : 'Aufgaben'}, samt Vertrag, Testfällen,
            JUnit-Dateien und Gewichten.
          </p>

          <button
            type="button"
            onClick={exportieren}
            disabled={phase.kind === 'busy'}
            className="mt-4 flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700 hover:-translate-y-0.5 active:translate-y-0 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:shadow-none disabled:hover:translate-y-0"
          >
            <Download className="w-4 h-4" aria-hidden="true" />
            Als Datei speichern
          </button>
        </section>

        {/* Import */}
        <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <h2 className="text-lg font-bold text-slate-800">Einspielen</h2>

          <fieldset className="mt-4">
            <legend className="text-sm font-semibold text-slate-700">Womit</legend>

            <div className="mt-2 space-y-2">
              <label className="flex gap-3 rounded-xl border border-slate-200 p-3 transition-colors hover:bg-slate-50">
                <input
                  type="radio"
                  name="modus"
                  checked={mode === 'Merge'}
                  onChange={() => setMode('Merge')}
                  className="mt-1 h-4 w-4 shrink-0 text-indigo-600"
                />
                <span>
                  <span className="block text-sm font-semibold text-slate-800">Zusammenführen</span>
                  <span className="block text-xs text-slate-500">
                    Was dieselbe Id hat, wird aktualisiert; Neues kommt dazu. Es wird nichts
                    gelöscht — auch nicht, was hier steht und in der Datei fehlt.
                  </span>
                </span>
              </label>

              <label className="flex gap-3 rounded-xl border border-slate-200 p-3 transition-colors hover:bg-slate-50">
                <input
                  type="radio"
                  name="modus"
                  checked={mode === 'Replace'}
                  onChange={() => setMode('Replace')}
                  className="mt-1 h-4 w-4 shrink-0 text-indigo-600"
                />
                <span>
                  <span className="block text-sm font-semibold text-slate-800">Ersetzen</span>
                  <span className="block text-xs text-slate-500">
                    Der Bestand wird geleert, danach ist die Datei die Wahrheit.{' '}
                    <strong className="text-amber-900">
                      Nimmt alle abgegebenen Lösungen mit.
                    </strong>
                  </span>
                </span>
              </label>
            </div>
          </fieldset>

          <button
            type="button"
            onClick={() => dateiRef.current?.click()}
            disabled={phase.kind === 'busy'}
            className="mt-4 flex items-center gap-2 rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <Upload className="w-4 h-4" aria-hidden="true" />
            Datei wählen …
          </button>

          <input
            ref={dateiRef}
            type="file"
            accept=".json,application/json"
            className="hidden"
            onChange={(event) => {
              void dateiLesen(event.target.files)
              event.target.value = ''
            }}
          />

          {phase.kind === 'busy' && (
            <p role="status" className="mt-4 flex items-center gap-2 text-sm text-slate-600">
              <Loader2 className="w-4 h-4 animate-spin" aria-hidden="true" />
              {phase.was}
            </p>
          )}
        </section>

        {/* Vorschau */}
        {preview && (
          <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm anim-auf">
            <h2 className="flex items-center gap-2 text-lg font-bold text-slate-800">
              <FileJson className="w-5 h-5 text-slate-500" aria-hidden="true" />
              Das würde passieren
            </h2>

            {preview.errors.length > 0 ? (
              <div className="mt-4">
                <p className="text-sm font-semibold text-rose-800">
                  Die Datei hat {preview.errors.length}{' '}
                  {preview.errors.length === 1 ? 'Beanstandung' : 'Beanstandungen'}. Es wurde
                  nichts geändert.
                </p>
                <ul className="mt-2 space-y-1 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
                  {preview.errors.map((error) => (
                    <li key={error}>{error}</li>
                  ))}
                </ul>
              </div>
            ) : (
              <>
                <div className="mt-4">
                  <Bericht report={preview} />
                </div>

                {preview.warnings.map((warning) => (
                  <p
                    key={warning}
                    className="mt-3 flex gap-2 rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900"
                  >
                    <AlertTriangle className="w-4 h-4 shrink-0" aria-hidden="true" />
                    {warning}
                  </p>
                ))}

                <button
                  type="button"
                  onClick={() => setBestaetigen(true)}
                  disabled={!kannImportieren}
                  className="mt-5 flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700 hover:-translate-y-0.5 active:translate-y-0 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:shadow-none"
                >
                  <Upload className="w-4 h-4" aria-hidden="true" />
                  Jetzt einspielen
                </button>
              </>
            )}
          </section>
        )}

        {/* Ergebnis */}
        {phase.kind === 'done' && (
          <section className="rounded-2xl border border-emerald-200 bg-emerald-50 p-6 anim-auf">
            <h2 className="flex items-center gap-2 text-lg font-bold text-emerald-900">
              <Check className="w-5 h-5" aria-hidden="true" />
              Eingespielt
            </h2>
            <div className="mt-4">
              <Bericht report={phase.report} />
            </div>
          </section>
        )}
      </div>

      {bestaetigen && preview && (
        <ConfirmDialog
          title={mode === 'Replace' ? 'Bestand ersetzen?' : 'Bestand zusammenführen?'}
          message={
            mode === 'Replace'
              ? `Der vorhandene Bestand wird geleert und durch die Datei ersetzt. Dabei gehen ${preview.submissionsDeleted} bereits abgegebene Lösung(en) samt Auswertung unwiederbringlich verloren.`
              : `${preview.categoriesCreated + preview.tasksCreated} Eintrag/Einträge kommen neu dazu, ${preview.categoriesUpdated + preview.tasksUpdated} werden aktualisiert. Es wird nichts gelöscht.`
          }
          confirmLabel={mode === 'Replace' ? 'Ersetzen' : 'Zusammenführen'}
          onConfirm={importieren}
          onCancel={() => setBestaetigen(false)}
        />
      )}
    </div>
  )
}
