import { useCallback, useRef, useState } from 'react'
import { AlertCircle, CheckCircle, FileCode2, FileText, Loader2, Upload, X } from 'lucide-react'
import { createSubmission } from '../api/endpoints'
import { UPLOAD_LIMITS, checkFiles, formatBytes } from '../api/uploadLimits'

type SubmissionFormProps = {
  taskItemId: string
  /** Was nach dem erfolgreichen Absenden passiert — die Teilnehmersicht wechselt
   *  auf die Ergebnisseite, der Probelauf bleibt und wartet auf das Ergebnis. */
  onSubmitted: (submissionId: string) => void
  /** Beschriftung des Knopfes; im Probelauf steht dort etwas anderes. */
  submitLabel?: string
}

// Dateien wählen, prüfen, absenden.
//
// Herausgelöst aus TaskPage, damit der Probelauf im Verwaltungsbereich
// dieselbe Auswahl samt Grenzen und Ablehnungen benutzt statt einer zweiten,
// die irgendwann anders prüft.
export function SubmissionForm({
  taskItemId,
  onSubmitted,
  submitLabel = 'Jetzt prüfen',
}: SubmissionFormProps) {
  const [files, setFiles] = useState<File[]>([])
  const [rejections, setRejections] = useState<string[]>([])
  const [serverError, setServerError] = useState<string | null>(null)
  const [sending, setSending] = useState(false)
  const [dragging, setDragging] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  const totalBytes = files.reduce((sum, f) => sum + f.size, 0)

  const addFiles = useCallback(
    (incoming: FileList | null) => {
      if (!incoming || incoming.length === 0) return
      const { accepted, rejections: reasons } = checkFiles(files, Array.from(incoming))
      setFiles(accepted)
      setRejections(reasons)
      setServerError(null)
    },
    [files],
  )

  const submit = useCallback(async () => {
    if (files.length === 0 || sending) return
    setSending(true)
    setServerError(null)

    const result = await createSubmission(taskItemId, files)
    setSending(false)

    if (result.kind === 'ok') {
      onSubmitted(result.value.id)
      return
    }

    // Der Server antwortet mit fertigen deutschen Sätzen. Die werden
    // durchgereicht, nicht durch eine eigene Meldung ersetzt.
    setServerError(result.message)
  }, [files, onSubmitted, sending, taskItemId])

  return (
    <>
      <div className="mb-4 flex items-center justify-between gap-4 flex-wrap">
        <h2 className="text-xl font-bold text-slate-800">Lösung abgeben</h2>
        {files.length > 0 && (
          <button
            type="button"
            onClick={() => {
              setFiles([])
              setRejections([])
              setServerError(null)
            }}
            className="text-sm text-rose-700 font-semibold hover:underline"
          >
            Alle entfernen
          </button>
        )}
      </div>

      <div
        onDragOver={(event) => {
          event.preventDefault()
          setDragging(true)
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={(event) => {
          event.preventDefault()
          setDragging(false)
          addFiles(event.dataTransfer.files)
        }}
        onClick={() => inputRef.current?.click()}
        className={`group relative border-2 border-dashed rounded-2xl p-12 flex flex-col items-center justify-center transition-all cursor-pointer ${
          dragging
            ? 'bg-indigo-50 border-indigo-500 shadow-xl shadow-indigo-50'
            : files.length > 0
              ? 'bg-emerald-50 border-emerald-300'
              : 'bg-slate-50 border-slate-300 hover:bg-white hover:border-indigo-400 hover:shadow-xl hover:shadow-indigo-50'
        }`}
      >
        <input
          type="file"
          className="hidden"
          ref={inputRef}
          accept={UPLOAD_LIMITS.allowedExtension}
          multiple
          onChange={(event) => {
            addFiles(event.target.files)
            event.target.value = ''
          }}
        />
        {files.length === 0 ? (
          <div key="leer" className="text-center">
            <div className="w-20 h-20 bg-white rounded-2xl flex items-center justify-center shadow-lg border border-slate-100 mb-6 mx-auto transition-transform duration-300 group-hover:scale-110 group-hover:rotate-3">
              <Upload className="w-10 h-10 text-indigo-600" aria-hidden="true" />
            </div>
            <p className="text-xl font-bold text-slate-800 mb-1">
              Zieh deine .java-Dateien hierher
            </p>
            <p className="text-slate-600">oder klicke, um sie auszuwählen</p>
          </div>
        ) : (
          <div key="gewaehlt" className="text-center">
            <div className="w-20 h-20 bg-white rounded-2xl flex items-center justify-center shadow-lg border border-emerald-100 mb-6 mx-auto scale-110">
              <FileCode2 className="w-10 h-10 text-emerald-700" aria-hidden="true" />
            </div>
            <p className="text-xl font-bold text-slate-800 mb-1">
              {files.length === 1 ? '1 Datei' : `${files.length} Dateien`} bereit
            </p>
            <p className="text-emerald-800 font-medium">Klicke, um weitere hinzuzufügen</p>
          </div>
        )}

        {/* Die Grenzen stehen da, bevor jemand dagegen läuft. */}
        <p className="mt-6 text-xs text-slate-600 text-center">
          {UPLOAD_LIMITS.allowedExtension} · höchstens {UPLOAD_LIMITS.maxFileCount} Dateien ·{' '}
          {formatBytes(UPLOAD_LIMITS.maxFileSizeBytes)} je Datei ·{' '}
          {formatBytes(UPLOAD_LIMITS.maxTotalSizeBytes)} gesamt
        </p>
      </div>

      {files.length > 0 && (
        <ul className="mt-4 space-y-2 overflow-hidden">
          {files.map((file, index) => (
            <li
              key={file.name}
              style={{ animationDelay: `${index * 45}ms` }}
              className="anim-links flex items-center gap-3 rounded-xl border border-slate-200 bg-white px-4 py-2.5"
            >
              <FileCode2 className="w-4 h-4 text-slate-500 shrink-0" aria-hidden="true" />
              <span className="font-mono text-sm text-slate-800 truncate flex-1">{file.name}</span>
              <span className="text-xs text-slate-600 shrink-0">{formatBytes(file.size)}</span>
              <button
                type="button"
                onClick={() => setFiles((current) => current.filter((f) => f !== file))}
                aria-label={`${file.name} entfernen`}
                className="p-1 rounded-md text-slate-500 transition-colors hover:bg-slate-100 hover:text-rose-700"
              >
                <X className="w-4 h-4" aria-hidden="true" />
              </button>
            </li>
          ))}
          <li className="px-4 text-xs text-slate-600">
            {files.length} von {UPLOAD_LIMITS.maxFileCount} Dateien · {formatBytes(totalBytes)} gesamt
          </li>
        </ul>
      )}

      {/* Eine verworfene Datei verschwindet nicht kommentarlos. */}
      {rejections.length > 0 && (
        <ul className="anim-auf mt-4 space-y-1.5 rounded-xl border border-amber-200 bg-amber-50 p-4">
          {rejections.map((reason) => (
            <li key={reason} className="flex gap-2 text-sm text-amber-900">
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" aria-hidden="true" />
              {reason}
            </li>
          ))}
        </ul>
      )}

      <div className="mt-8 flex justify-center">
        <button
          type="button"
          disabled={files.length === 0 || sending}
          onClick={submit}
          className={`px-10 py-4 rounded-xl font-bold text-lg transition-all flex items-center gap-3 ${
            files.length === 0 || sending
              ? 'bg-slate-200 text-slate-500 cursor-not-allowed'
              : 'bg-indigo-600 text-white hover:bg-indigo-700 shadow-2xl shadow-indigo-200 hover:-translate-y-1 active:translate-y-0'
          }`}
        >
          {sending ? (
            <>
              <Loader2 className="w-5 h-5 animate-spin" aria-hidden="true" />
              Wird gesendet …
            </>
          ) : (
            <>
              <CheckCircle className="w-5 h-5" aria-hidden="true" />
              {submitLabel}
            </>
          )}
        </button>
      </div>

      {serverError && (
        <div
          role="alert"
          className="anim-auf mt-4 flex gap-2 rounded-xl border border-rose-200 bg-rose-50 p-4 text-rose-800"
        >
          <FileText className="w-5 h-5 shrink-0" aria-hidden="true" />
          <p>{serverError}</p>
        </div>
      )}
    </>
  )
}
