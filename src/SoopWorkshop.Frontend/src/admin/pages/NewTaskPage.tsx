import { useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { ArrowLeft, Plus } from 'lucide-react'
import { useAdminCatalog } from '../adminOutlet'
import { TextInput } from '../components/TextInput'
import { TextArea } from '../components/TextArea'
import { Select } from '../components/Select'
import { createTask } from '../api/tasks'
import { FIELD_LIMITS, checkMaxLength, checkRequired, collect } from '../validation'
import { DIFFICULTY_LABELS, MODE_LABELS } from '../../api/labels'
import type { Difficulty, EvaluationMode } from '../../api/types'

const DIFFICULTY_OPTIONS = (Object.keys(DIFFICULTY_LABELS) as Difficulty[]).map((value) => ({
  value,
  label: DIFFICULTY_LABELS[value],
}))

const MODE_OPTIONS = (Object.keys(MODE_LABELS) as EvaluationMode[]).map((value) => ({
  value,
  label: MODE_LABELS[value],
}))

// Bewusst nur das Nötigste. Vertrag, Tipps, Testfälle und Gewichte kommen im
// Editor dazu — beim Anlegen gibt es sie noch gar nicht, und ein Formular, das
// nach allem auf einmal fragt, beantwortet niemand vollständig.
export function NewTaskPage() {
  const navigate = useNavigate()
  const { categories, reload } = useAdminCatalog()
  const [params] = useSearchParams()

  const [categoryId, setCategoryId] = useState(params.get('kategorie') ?? '')

  // Die Vorauswahl kann nicht im Anfangswert von useState stehen: dieser Bau
  // rendert, während das Layout die Kategorien noch lädt, und useState nimmt
  // seinen Anfangswert nur beim ersten Mal. Die Liste war dann leer, die Auswahl
  // blieb leer, und das Anlegen scheiterte an der eigenen Prüfung — obwohl im
  // Auswahlfeld sichtbar eine Kategorie stand.
  useEffect(() => {
    if (categoryId.length === 0 && categories.length > 0) setCategoryId(categories[0].id)
  }, [categories, categoryId])
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [difficulty, setDifficulty] = useState<Difficulty>('Easy')
  const [evaluationMode, setEvaluationMode] = useState<EvaluationMode>('ConsoleOnly')

  const [problems, setProblems] = useState<string[]>([])
  const [busy, setBusy] = useState(false)

  const submit = async () => {
    const found = collect(
      checkRequired('title', 'Der Titel', title),
      checkMaxLength('title', 'Der Titel', title, FIELD_LIMITS.taskTitle),
      checkRequired('description', 'Die Beschreibung', description),
    ).map((problem) => problem.message)

    if (categoryId.length === 0) found.push('Bitte eine Kategorie wählen.')

    setProblems(found)
    if (found.length > 0) return

    setBusy(true)

    // Ans Ende der gewählten Kategorie einsortieren.
    const category = categories.find((c) => c.id === categoryId)
    const nextOrder = (category?.tasks.reduce((max, t) => Math.max(max, t.order), 0) ?? 0) + 1

    const result = await createTask({
      taskCategoryId: categoryId,
      title: title.trim(),
      description,
      difficulty,
      order: nextOrder,
      evaluationMode,
      // Der Vertrag entsteht im Editor: beim Anlegen weiß man oft noch nicht,
      // wie die Klassen heißen sollen.
      expectedTypes: [],
      hints: [],
    })

    setBusy(false)

    if (result.kind !== 'ok') {
      setProblems([result.message])
      return
    }

    reload()
    // Direkt in den Editor: dort kommen Vertrag, Testfälle und Gewichte dazu.
    navigate(`/admin/aufgaben/${result.value.id}`)
  }

  if (categories.length === 0) {
    return (
      <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
        <div className="mx-auto w-full max-w-2xl rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center">
          <p className="font-medium text-slate-700">Es gibt noch keine Kategorie.</p>
          <p className="mt-1 text-sm text-slate-500">
            Eine Aufgabe gehört immer in eine Kategorie — die muss es zuerst geben.
          </p>
          <Link
            to="/admin/kategorien"
            className="mt-6 inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700"
          >
            Zu den Kategorien
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
      <div className="mx-auto w-full max-w-2xl">
        <Link
          to="/admin"
          className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-600 hover:text-slate-900 hover:underline"
        >
          <ArrowLeft className="w-4 h-4" aria-hidden="true" />
          Übersicht
        </Link>

        <h1 className="mt-3 text-2xl font-bold text-slate-800">Neue Aufgabe</h1>
        <p className="mt-1 text-slate-600">
          Sie wird verborgen angelegt. Testfälle und Vertrag kommen im nächsten Schritt dazu.
        </p>

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

        <div className="mt-6 space-y-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <TextInput
            label="Titel"
            value={title}
            onChange={setTitle}
            maxLength={FIELD_LIMITS.taskTitle}
          />

          <div className="grid gap-4 sm:grid-cols-3">
            <Select
              label="Kategorie"
              value={categoryId}
              options={categories.map((category) => ({ value: category.id, label: category.name }))}
              onChange={setCategoryId}
            />
            <Select<Difficulty>
              label="Schwierigkeit"
              value={difficulty}
              options={DIFFICULTY_OPTIONS}
              onChange={setDifficulty}
            />
            <Select<EvaluationMode>
              label="Auswertung"
              value={evaluationMode}
              options={MODE_OPTIONS}
              onChange={setEvaluationMode}
            />
          </div>

          <TextArea
            label="Beschreibung"
            hint="Markdown."
            value={description}
            onChange={setDescription}
            rows={8}
          />

          <button
            type="button"
            onClick={submit}
            disabled={busy}
            className="flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700 hover:-translate-y-0.5 active:translate-y-0 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:shadow-none"
          >
            <Plus className="w-4 h-4" aria-hidden="true" />
            {busy ? 'Wird angelegt …' : 'Anlegen und bearbeiten'}
          </button>
        </div>
      </div>
    </div>
  )
}
