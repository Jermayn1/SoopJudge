import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { AlertCircle, ArrowLeft, Eye, EyeOff, RefreshCw, Trash2 } from 'lucide-react'
import { useAdminCatalog } from '../adminOutlet'
import { TextInput } from '../components/TextInput'
import { TextArea } from '../components/TextArea'
import { NumberInput } from '../components/NumberInput'
import { Select } from '../components/Select'
import { SaveBar } from '../components/SaveBar'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { StringListEditor } from '../components/StringListEditor'
import { ExpectedTypesEditor } from '../components/ExpectedTypesEditor'
import { TestCaseEditor } from '../components/TestCaseEditor'
import { UnitTestFileEditor } from '../components/UnitTestFileEditor'
import { TrialRun } from '../components/TrialRun'
import { WeightEditor } from '../components/WeightEditor'
import { TaskMarkdown } from '../../components/TaskMarkdown'
import {
  WEIGHTED_CATEGORIES,
  defaultWeights,
  toWeightValues,
  type WeightValues,
} from '../weights'
import {
  deleteTask,
  fetchAdminTask,
  fetchTaskTests,
  fetchTaskUnitTestFiles,
  fetchTaskWeights,
  saveTaskTests,
  saveTaskUnitTestFiles,
  saveTaskWeights,
  toggleTaskVisibility,
  updateTask,
  type TaskDraft,
  type TaskTestDraft,
  type UnitTestFileDraft,
} from '../api/tasks'
import {
  FIELD_LIMITS,
  checkJavaFileName,
  checkMaxLength,
  checkRequired,
  collect,
} from '../validation'
import { DIFFICULTY_LABELS, MODE_LABELS } from '../../api/labels'
import type { SaveState } from '../saveState'
import type { Difficulty, EvaluationMode } from '../../api/types'

const DIFFICULTY_OPTIONS = (Object.keys(DIFFICULTY_LABELS) as Difficulty[]).map((value) => ({
  value,
  label: DIFFICULTY_LABELS[value],
}))

const MODE_OPTIONS = (Object.keys(MODE_LABELS) as EvaluationMode[]).map((value) => ({
  value,
  label: MODE_LABELS[value],
}))

type LoadState =
  | { kind: 'loading' }
  | { kind: 'ok' }
  | { kind: 'missing'; message: string }
  | { kind: 'unreachable'; message: string }

function Card({ title, hint, children }: { title: string; hint?: string; children: React.ReactNode }) {
  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
      <h2 className="text-lg font-bold text-slate-800">{title}</h2>
      {hint && <p className="mt-0.5 text-sm text-slate-600">{hint}</p>}
      <div className="mt-4">{children}</div>
    </section>
  )
}

export function TaskEditorPage() {
  const { taskId = '' } = useParams()
  const navigate = useNavigate()
  const { categories, reload } = useAdminCatalog()

  const [state, setState] = useState<LoadState>({ kind: 'loading' })
  const [attempt, setAttempt] = useState(0)

  const [draft, setDraft] = useState<TaskDraft | null>(null)
  const [tests, setTests] = useState<TaskTestDraft[]>([])
  const [weights, setWeights] = useState<WeightValues>(defaultWeights())
  const [isVisible, setIsVisible] = useState(false)
  const [unitTestFiles, setUnitTestFiles] = useState<UnitTestFileDraft[]>([])

  // Vergleichsstand zum Erkennen von Änderungen. Als Zeichenkette, weil hier
  // nur "gleich oder nicht" zählt und die Formen flach sind.
  const [baseline, setBaseline] = useState('')

  const [save, setSave] = useState<SaveState>({ kind: 'idle' })
  const [problems, setProblems] = useState<string[]>([])
  const [pendingDelete, setPendingDelete] = useState(false)
  const [visibilityProblem, setVisibilityProblem] = useState<string | null>(null)

  const snapshot = useCallback(
    (
      d: TaskDraft | null,
      t: TaskTestDraft[],
      w: WeightValues,
      u: UnitTestFileDraft[],
    ) => JSON.stringify([d, t, w, u]),
    [],
  )

  useEffect(() => {
    const controller = new AbortController()
    setState({ kind: 'loading' })

    Promise.all([
      fetchAdminTask(taskId, controller.signal),
      fetchTaskTests(taskId, controller.signal),
      fetchTaskWeights(taskId, controller.signal),
      fetchTaskUnitTestFiles(taskId, controller.signal),
    ])
      .then(([taskResult, testsResult, weightsResult, filesResult]) => {
        if (controller.signal.aborted) return

        if (taskResult.kind === 'notFound') {
          setState({ kind: 'missing', message: taskResult.message })
          return
        }

        if (taskResult.kind !== 'ok') {
          setState({ kind: 'unreachable', message: taskResult.message })
          return
        }

        const task = taskResult.value
        const nextDraft: TaskDraft = {
          taskCategoryId: task.categoryId,
          title: task.title,
          description: task.description,
          difficulty: task.difficulty,
          order: task.order,
          evaluationMode: task.evaluationMode,
          expectedTypes: task.expectedTypes.map((type) => ({
            name: type.name,
            methods: type.methods,
          })),
          hints: task.hints.map((hint) => hint.content),
        }

        const nextTests =
          testsResult.kind === 'ok'
            ? testsResult.value.map((test) => ({
                input: test.input,
                expectedOutput: test.expectedOutput,
                description: test.description,
              }))
            : []

        const nextWeights =
          weightsResult.kind === 'ok' ? toWeightValues(weightsResult.value) : defaultWeights()

        const nextFiles: UnitTestFileDraft[] =
          filesResult.kind === 'ok'
            ? filesResult.value.map((file) => ({
                fileName: file.fileName,
                content: file.content,
                isVisibleToParticipant: file.isVisibleToParticipant,
              }))
            : []

        setDraft(nextDraft)
        setTests(nextTests)
        setWeights(nextWeights)
        setIsVisible(task.isVisible)
        setUnitTestFiles(nextFiles)
        setBaseline(snapshot(nextDraft, nextTests, nextWeights, nextFiles))
        setSave({ kind: 'idle' })
        setProblems([])
        setVisibilityProblem(null)
        setState({ kind: 'ok' })
      })
      .catch((cause) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') return
        setState({ kind: 'unreachable', message: 'Die Aufgabe konnte nicht geladen werden.' })
      })

    return () => controller.abort()
  }, [taskId, attempt, snapshot])

  const dirty =
    state.kind === 'ok' && snapshot(draft, tests, weights, unitTestFiles) !== baseline

  const patch = (values: Partial<TaskDraft>) =>
    setDraft((current) => (current ? { ...current, ...values } : current))

  // Spiegelt DescribeMissingTestData aus dem Backend. Vorab geprüft, damit der
  // Schalter erklärt statt abzuweisen — lehnt der Server trotzdem ab, steht
  // sein Satz darunter.
  const missingTestData = (): string | null => {
    if (!draft) return null

    const needsConsole =
      draft.evaluationMode === 'ConsoleOnly' || draft.evaluationMode === 'Both'
    const needsUnit = draft.evaluationMode === 'UnitTestOnly' || draft.evaluationMode === 'Both'

    if (needsConsole && tests.length === 0)
      return `Der Auswertungsmodus „${MODE_LABELS[draft.evaluationMode]}“ verlangt mindestens einen Konsolen-Testfall.`

    if (needsUnit && unitTestFiles.length === 0)
      return `Der Auswertungsmodus „${MODE_LABELS[draft.evaluationMode]}“ verlangt mindestens eine JUnit-Datei.`

    return null
  }

  const validate = (): string[] => {
    if (!draft) return []

    const found = collect(
      checkRequired('title', 'Der Titel', draft.title),
      checkMaxLength('title', 'Der Titel', draft.title, FIELD_LIMITS.taskTitle),
      checkRequired('description', 'Die Beschreibung', draft.description),
    ).map((problem) => problem.message)

    // Klassen ohne Namen sind kein Fehler — der Editor legt eine leere Zeile an,
    // wenn man "Klasse hinzufügen" drückt, und sie fällt beim Senden heraus.
    // Zu lange Namen dagegen würden erst der Server ablehnen.
    draft.expectedTypes.forEach((type, index) => {
      const problem = checkMaxLength(
        'expectedType',
        `Der Name der ${index + 1}. Klasse`,
        type.name,
        FIELD_LIMITS.expectedClassName,
      )
      if (problem) found.push(problem.message)

      // Eine Methode ohne Klasse kann nicht geprüft werden — sie hätte keinen
      // Rumpf, in dem gesucht wird.
      if (type.name.trim().length === 0 && type.methods.some((m) => m.trim().length > 0))
        found.push(
          `Die ${index + 1}. Klasse hat Methoden, aber keinen Namen. Ohne Klassennamen lässt sich nicht prüfen, wo die Methode stehen muss.`,
        )
    })

    tests.forEach((test, index) => {
      if (test.description.trim().length === 0)
        found.push(`Testfall ${index + 1}: Die Beschreibung darf nicht leer sein.`)
      else if (test.description.length > FIELD_LIMITS.testDescription)
        found.push(
          `Testfall ${index + 1}: Die Beschreibung ist ${test.description.length} Zeichen lang — erlaubt sind ${FIELD_LIMITS.testDescription}.`,
        )

      if (test.expectedOutput.length === 0)
        found.push(`Testfall ${index + 1}: Die erwartete Ausgabe darf nicht leer sein.`)
    })

    for (const entry of WEIGHTED_CATEGORIES) {
      if (weights[entry.category] <= 0)
        found.push(`Das Gewicht für ${entry.label} muss größer als 0 sein.`)
    }

    unitTestFiles.forEach((file, index) => {
      const problem = checkJavaFileName('junit', file.fileName)
      if (problem) found.push(`JUnit-Datei ${index + 1}: ${problem.message}`)

      if (file.content.trim().length === 0)
        found.push(`JUnit-Datei ${index + 1}: Der Inhalt darf nicht leer sein.`)
    })

    // Gleicher Name zweimal: im Arbeitsverzeichnis würde die eine Datei die
    // andere überschreiben. Der Server lehnt das ohnehin ab.
    const namen = unitTestFiles.map((file) => file.fileName.trim().toLowerCase())
    const doppelt = namen.find((name, index) => name.length > 0 && namen.indexOf(name) !== index)
    if (doppelt)
      found.push(`Der Dateiname '${doppelt}' kommt mehrfach vor.`)

    return found
  }

  const onSave = async () => {
    if (!draft) return

    const found = validate()
    setProblems(found)
    if (found.length > 0) {
      setSave({ kind: 'error', message: 'Bitte zuerst die markierten Punkte korrigieren.' })
      return
    }

    setSave({ kind: 'saving' })

    // Nacheinander und mit benanntem Schritt. Es gibt keine Transaktion über
    // die drei Endpunkte — scheitert einer, muss die Meldung sagen, welcher,
    // statt pauschal "Speichern fehlgeschlagen" zu behaupten.
    const steps: { name: string; run: () => Promise<{ kind: string; message?: string }> }[] = [
      {
        // Leere Klassen- und Signaturzeilen räumt updateTask selbst weg.
        name: 'Die Grunddaten',
        run: () => updateTask(taskId, draft, isVisible),
      },
      { name: 'Die Testfälle', run: () => saveTaskTests(taskId, tests) },
      { name: 'Die JUnit-Dateien', run: () => saveTaskUnitTestFiles(taskId, unitTestFiles) },
      {
        name: 'Die Gewichte',
        run: () =>
          saveTaskWeights(
            taskId,
            WEIGHTED_CATEGORIES.map((entry) => ({
              category: entry.category,
              weight: weights[entry.category],
            })),
          ),
      },
    ]

    for (const step of steps) {
      const result = await step.run()
      if (result.kind !== 'ok') {
        setSave({
          kind: 'error',
          message: `${step.name} konnten nicht gespeichert werden: ${result.message ?? 'Unbekannter Fehler.'}`,
        })
        return
      }
    }

    setBaseline(snapshot(draft, tests, weights, unitTestFiles))
    setSave({ kind: 'saved' })
    reload()
  }

  const onToggleVisibility = async () => {
    setVisibilityProblem(null)

    const result = await toggleTaskVisibility(taskId)

    if (result.kind !== 'ok') {
      setVisibilityProblem(result.message)
      return
    }

    setIsVisible(result.value.isVisible ?? false)
    reload()
  }

  const onDelete = async () => {
    const result = await deleteTask(taskId)

    if (result.kind !== 'ok') {
      setVisibilityProblem(result.message)
      setPendingDelete(false)
      return
    }

    reload()
    navigate('/admin')
  }

  if (state.kind === 'loading') {
    return (
      <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
        <div className="mx-auto w-full max-w-3xl space-y-4" aria-hidden="true">
          <div className="h-10 w-2/3 rounded-xl bg-slate-200 animate-pulse" />
          <div className="h-64 rounded-2xl bg-slate-200 animate-pulse" />
          <div className="h-40 rounded-2xl bg-slate-200 animate-pulse" />
        </div>
      </div>
    )
  }

  if (state.kind !== 'ok' || !draft) {
    const unreachable = state.kind === 'unreachable'
    return (
      <div className="flex flex-1 items-center justify-center bg-slate-50 p-8">
        <div className="max-w-md rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
          <div className="mx-auto mb-4 flex w-16 h-16 items-center justify-center rounded-3xl bg-slate-100">
            <AlertCircle className="w-8 h-8 text-slate-500" aria-hidden="true" />
          </div>
          <h2 className="mb-2 text-2xl font-bold text-slate-800">
            {unreachable ? 'Der Server antwortet nicht' : 'Diese Aufgabe gibt es nicht'}
          </h2>
          <p className="text-slate-600">{state.kind === 'ok' ? '' : state.message}</p>
          {unreachable && (
            <button
              type="button"
              onClick={() => setAttempt((n) => n + 1)}
              className="mt-6 inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700"
            >
              <RefreshCw className="w-4 h-4" aria-hidden="true" />
              Erneut versuchen
            </button>
          )}
          <Link
            to="/admin"
            className="mt-6 flex items-center justify-center gap-1.5 text-sm font-semibold text-slate-700 hover:underline"
          >
            <ArrowLeft className="w-4 h-4" aria-hidden="true" />
            Zurück zur Übersicht
          </Link>
        </div>
      </div>
    )
  }

  const blocker = missingTestData()

  return (
    <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
      <div className="mx-auto w-full max-w-3xl">
        <Link
          to="/admin"
          className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-600 hover:text-slate-900 hover:underline"
        >
          <ArrowLeft className="w-4 h-4" aria-hidden="true" />
          Übersicht
        </Link>

        <div className="mt-3 flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-bold text-slate-800">{draft.title || 'Aufgabe'}</h1>
          <Link
            to={`/admin/aufgaben/${taskId}/vorschau`}
            className="ml-auto flex items-center gap-1.5 rounded-xl border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100"
          >
            <Eye className="w-4 h-4" aria-hidden="true" />
            Teilnehmer-Vorschau
          </Link>
        </div>

        {problems.length > 0 && (
          <ul
            role="alert"
            className="mt-4 space-y-1 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800"
          >
            {problems.map((problem) => (
              <li key={problem}>{problem}</li>
            ))}
          </ul>
        )}

        <div className="mt-6 space-y-6">
          <Card title="Grunddaten">
            <div className="space-y-4">
              <TextInput
                label="Titel"
                value={draft.title}
                onChange={(value) => patch({ title: value })}
                maxLength={FIELD_LIMITS.taskTitle}
              />

              <div className="grid gap-4 sm:grid-cols-3">
                <Select
                  label="Kategorie"
                  value={draft.taskCategoryId}
                  options={categories.map((category) => ({
                    value: category.id,
                    label: category.name,
                  }))}
                  onChange={(value) => patch({ taskCategoryId: value })}
                />
                <Select<Difficulty>
                  label="Schwierigkeit"
                  value={draft.difficulty}
                  options={DIFFICULTY_OPTIONS}
                  onChange={(value) => patch({ difficulty: value })}
                />
                <NumberInput
                  label="Reihenfolge"
                  hint="Innerhalb der Kategorie"
                  value={draft.order}
                  onChange={(value) => patch({ order: value })}
                />
              </div>

              <Select<EvaluationMode>
                label="Auswertung"
                hint="Steuert, was geprüft wird — unabhängig davon, welche Testdaten hinterlegt sind."
                value={draft.evaluationMode}
                options={MODE_OPTIONS}
                onChange={(value) => patch({ evaluationMode: value })}
              />

              <TextArea
                label="Beschreibung"
                hint="Markdown. Die Vorschau darunter zeigt, was der Teilnehmer sieht."
                value={draft.description}
                onChange={(value) => patch({ description: value })}
                rows={10}
              />

              {draft.description.trim().length > 0 && (
                <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
                  <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-slate-500">
                    Vorschau
                  </p>
                  <TaskMarkdown>{draft.description}</TaskMarkdown>
                </div>
              )}
            </div>
          </Card>

          <Card
            title="Aufgaben-Vertrag"
            hint="Welche Klassen die Abgabe enthalten muss und welche Methoden in welcher davon. Wird geprüft, nicht nur angezeigt — und der Teilnehmer bekommt ihn zu lesen."
          >
            <ExpectedTypesEditor
              types={draft.expectedTypes}
              onChange={(expectedTypes) => patch({ expectedTypes })}
            />
          </Card>

          <Card title="Tipps" hint="Beim Teilnehmer eingeklappt, in dieser Reihenfolge.">
            <StringListEditor
              label="Tipps"
              itemNoun="Tipp"
              values={draft.hints}
              onChange={(values) => patch({ hints: values })}
              placeholder="Denk an die Groß- und Kleinschreibung."
              addLabel="Tipp hinzufügen"
            />
          </Card>

          <Card
            title="Konsolen-Testfälle"
            hint="Vergleich von Eingabe und Ausgabe. Zahlen auf die Kategorie Funktionalität ein."
          >
            <TestCaseEditor tests={tests} onChange={setTests} />
          </Card>

          <Card
            title="JUnit-Dateien"
            hint="Werden zusammen mit der Abgabe übersetzt und ausgeführt. Eine Testdatei darf mehrere Klassen der Abgabe benutzen."
          >
            <UnitTestFileEditor files={unitTestFiles} onChange={setUnitTestFiles} />
          </Card>

          <Card title="Gewichtung">
            <WeightEditor values={weights} onChange={setWeights} />
          </Card>

          <Card
            title="Probelauf"
            hint="Eigene Lösung hochladen und die Bewertung sehen — ohne die Aufgabe freizuschalten. So fällt eine kaputte JUnit-Datei hier auf und nicht erst im Workshop."
          >
            {dirty ? (
              <p className="rounded-xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
                Erst speichern — ein Probelauf wird gegen den gespeicherten Stand ausgewertet,
                nicht gegen das, was hier gerade im Formular steht.
              </p>
            ) : (
              <TrialRun taskItemId={taskId} />
            )}
          </Card>

          <Card title="Sichtbarkeit">
            <div className="space-y-3">
              {blocker && !isVisible && (
                <p className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
                  {blocker}
                </p>
              )}

              {visibilityProblem && (
                <p
                  role="alert"
                  className="rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-800"
                >
                  {visibilityProblem}
                </p>
              )}

              <div className="flex flex-wrap items-center gap-3">
                <button
                  type="button"
                  onClick={onToggleVisibility}
                  disabled={dirty}
                  className="flex items-center gap-2 rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {isVisible ? (
                    <EyeOff className="w-4 h-4" aria-hidden="true" />
                  ) : (
                    <Eye className="w-4 h-4" aria-hidden="true" />
                  )}
                  {isVisible ? 'Für Teilnehmer verbergen' : 'Für Teilnehmer freischalten'}
                </button>

                <span className="text-sm text-slate-600">
                  Zurzeit {isVisible ? 'sichtbar' : 'verborgen'}.
                </span>

                <button
                  type="button"
                  onClick={() => setPendingDelete(true)}
                  className="ml-auto flex items-center gap-2 rounded-xl border border-rose-200 px-4 py-2 text-sm font-semibold text-rose-800 transition-colors hover:bg-rose-50"
                >
                  <Trash2 className="w-4 h-4" aria-hidden="true" />
                  Aufgabe löschen
                </button>
              </div>

              {dirty && (
                <p className="text-xs text-slate-500">
                  Erst speichern — die Sichtbarkeit wird gegen den gespeicherten Stand geprüft.
                </p>
              )}
            </div>
          </Card>
        </div>

        <SaveBar
          state={save}
          dirty={dirty}
          onSave={onSave}
          onReset={() => setAttempt((n) => n + 1)}
        />
      </div>

      {pendingDelete && (
        <ConfirmDialog
          title={`„${draft.title}“ löschen?`}
          message="Damit verschwinden auch alle Testfälle, JUnit-Dateien, Gewichte und die bereits abgegebenen Lösungen dieser Aufgabe. Das lässt sich nicht rückgängig machen."
          confirmLabel="Endgültig löschen"
          onConfirm={onDelete}
          onCancel={() => setPendingDelete(false)}
        />
      )}
    </div>
  )
}
