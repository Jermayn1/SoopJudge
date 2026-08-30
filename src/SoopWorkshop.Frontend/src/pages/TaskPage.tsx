import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { AlertCircle, RefreshCw } from 'lucide-react'
import { TaskView } from '../components/TaskView'
import { SubmissionForm } from '../components/SubmissionForm'
import { fetchTask } from '../api/endpoints'
import type { Task } from '../api/types'

// Vier Ausgänge, nicht zwei: bei gestopptem Backend stand hier früher
// "Diese Aufgabe gibt es nicht (mehr)" — die denkbar irreführendste Auskunft.
type LoadState =
  | { kind: 'loading' }
  | { kind: 'ok'; task: Task }
  | { kind: 'missing'; message: string }
  | { kind: 'unreachable'; message: string }

export function TaskPage() {
  const { taskId = '' } = useParams()
  const navigate = useNavigate()

  const [state, setState] = useState<LoadState>({ kind: 'loading' })
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setState({ kind: 'loading' })

    fetchTask(taskId, controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return
        if (result.kind === 'ok') setState({ kind: 'ok', task: result.value })
        else if (result.kind === 'notFound') setState({ kind: 'missing', message: result.message })
        else setState({ kind: 'unreachable', message: result.message })
      })
      .catch((cause) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') return
        setState({ kind: 'unreachable', message: 'Die Aufgabe konnte nicht geladen werden.' })
      })

    return () => controller.abort()
  }, [taskId, attempt])

  if (state.kind === 'loading') {
    return (
      <div className="flex-1 overflow-y-auto bg-white p-8">
        <div className="max-w-4xl mx-auto w-full space-y-6" aria-hidden="true">
          <div className="h-6 w-40 rounded-full bg-slate-200 animate-pulse" />
          <div className="h-10 w-2/3 rounded bg-slate-200 animate-pulse" />
          <div className="h-40 rounded-2xl bg-slate-100 animate-pulse" />
        </div>
      </div>
    )
  }

  if (state.kind !== 'ok') {
    const unreachable = state.kind === 'unreachable'
    return (
      <div className="flex-1 flex items-center justify-center bg-white p-8">
        <div className="max-w-md text-center">
          <div className="w-16 h-16 bg-slate-100 rounded-2xl flex items-center justify-center mx-auto mb-5">
            <AlertCircle className="w-8 h-8 text-slate-500" aria-hidden="true" />
          </div>
          <h2 className="text-2xl font-bold text-slate-800 mb-2">
            {unreachable ? 'Der Server antwortet nicht' : 'Diese Aufgabe gibt es nicht'}
          </h2>
          <p className="text-slate-600">{state.message}</p>
          {unreachable && (
            <button
              type="button"
              onClick={() => setAttempt((n) => n + 1)}
              className="mt-5 inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700 hover:-translate-y-0.5 active:translate-y-0"
            >
              <RefreshCw className="w-4 h-4" aria-hidden="true" />
              Erneut versuchen
            </button>
          )}
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 bg-white flex flex-col p-8 overflow-y-auto">
      <div className="max-w-4xl mx-auto w-full pb-20">
        <TaskView task={state.task} />

        <section>
          {/* Der Schlüssel sorgt dafür, dass die Auswahl beim Wechsel auf eine
              andere Aufgabe geleert wird — sonst hänge die vorige Datei noch
              in einem Formular, das zu einer anderen Aufgabe gehört. */}
          <SubmissionForm
            key={state.task.id}
            taskItemId={state.task.id}
            onSubmitted={(submissionId) => navigate(`/abgaben/${submissionId}`)}
          />
        </section>
      </div>
    </div>
  )
}
