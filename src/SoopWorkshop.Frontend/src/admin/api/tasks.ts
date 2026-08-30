import type { components } from '../../api/schema'
import { jsonRequest, request, type ApiResult } from '../../api/client'
import { toTask, toTaskCategoryWeight, toTaskTest, toUnitTestFile } from '../../api/mappers'
import { mapResult } from './catalog'
import type { Task, TaskCategoryWeight, TaskTest, UnitTestFile } from '../../api/types'

type Schemas = components['schemas']

// Die Grunddaten einer Aufgabe samt Vertrag und Tipps. Testfälle, JUnit-Dateien
// und Gewichte hört dieser Endpunkt nicht — die haben eigene, weil die
// öffentliche Aufgabe sie nicht enthalten darf.
export async function fetchAdminTask(id: string, signal?: AbortSignal): Promise<ApiResult<Task>> {
  const result = await request<Schemas['TaskItemDto']>(`/api/admin/tasks/${id}`, { signal })
  return mapResult(result, toTask)
}

// Eine geforderte Klasse im Formular. Ohne Id: beim Speichern wird der Vertrag
// als Ganzes ersetzt, eine Identität braucht er dabei nicht.
export type ExpectedTypeDraft = {
  name: string
  methods: string[]
}

// Was in beiden Richtungen gleich aussieht — beim Anlegen und beim Ändern.
export type TaskDraft = {
  taskCategoryId: string
  title: string
  description: string
  difficulty: Task['difficulty']
  order: number
  evaluationMode: Task['evaluationMode']
  expectedTypes: ExpectedTypeDraft[]
  hints: string[]
}

// Leere Zeilen fallen hier heraus statt beim Server: ein Formular hat fast
// immer eine angefangene, noch leere Zeile am Ende.
function toExpectedTypes(draft: TaskDraft): Schemas['ExpectedTypeInputDto'][] {
  return draft.expectedTypes
    .filter((type) => type.name.trim().length > 0)
    .map((type) => ({
      name: type.name.trim(),
      methods: type.methods.map((m) => m.trim()).filter((m) => m.length > 0),
    }))
}

export async function createTask(
  draft: TaskDraft,
  signal?: AbortSignal,
): Promise<ApiResult<Task>> {
  const body: Schemas['CreateTaskItemDto'] = {
    taskCategoryId: draft.taskCategoryId,
    title: draft.title,
    description: draft.description,
    difficulty: draft.difficulty,
    order: draft.order,
    evaluationMode: draft.evaluationMode,
    expectedTypes: toExpectedTypes(draft),
    hints: draft.hints,
    // Immer verborgen anlegen. Beim Anlegen gibt es die Testfälle noch nicht,
    // und IsVisible umgeht im Backend die Prüfung auf passende Testdaten —
    // eine sofort sichtbare Aufgabe wäre also womöglich eine, die still
    // milder bewertet wird.
    isVisible: false,
  }

  const result = await jsonRequest<Schemas['TaskItemDto']>(
    '/api/admin/tasks',
    'POST',
    body,
    signal,
  )
  return mapResult(result, toTask)
}

export async function updateTask(
  id: string,
  draft: TaskDraft,
  isVisible: boolean,
  signal?: AbortSignal,
): Promise<ApiResult<Task>> {
  const body: Schemas['UpdateTaskItemDto'] = {
    id,
    taskCategoryId: draft.taskCategoryId,
    title: draft.title,
    description: draft.description,
    difficulty: draft.difficulty,
    order: draft.order,
    evaluationMode: draft.evaluationMode,
    expectedTypes: toExpectedTypes(draft),
    hints: draft.hints,
    // Unverändert durchreichen: umgeschaltet wird über den eigenen Endpunkt,
    // der die Testdaten dabei prüft.
    isVisible,
  }

  const result = await jsonRequest<Schemas['TaskItemDto']>(
    `/api/admin/tasks/${id}`,
    'PUT',
    body,
    signal,
  )
  return mapResult(result, toTask)
}

// Nimmt per Cascade alle Testfälle, JUnit-Dateien, Gewichte und Abgaben mit.
export function deleteTask(id: string, signal?: AbortSignal): Promise<ApiResult<void>> {
  return jsonRequest<void>(`/api/admin/tasks/${id}`, 'DELETE', undefined, signal)
}

export function toggleTaskVisibility(
  id: string,
  signal?: AbortSignal,
): Promise<ApiResult<Schemas['VisibilityStateDto']>> {
  return jsonRequest<Schemas['VisibilityStateDto']>(
    `/api/admin/tasks/${id}/visibility`,
    'PATCH',
    undefined,
    signal,
  )
}

// ── Konsolen-Testfälle ────────────────────────────────────────────────

export async function fetchTaskTests(
  taskItemId: string,
  signal?: AbortSignal,
): Promise<ApiResult<TaskTest[]>> {
  const result = await request<Schemas['TaskTestDto'][]>(
    `/api/admin/tasks/${taskItemId}/tests`,
    { signal },
  )
  return mapResult(result, (dtos) => dtos.map(toTaskTest).sort((a, b) => a.order - b.order))
}

export type TaskTestDraft = Pick<TaskTest, 'input' | 'expectedOutput' | 'description'>

// Blockspeicherung: was nicht mitkommt, wird gelöscht. Die Reihenfolge ergibt
// sich aus der Position in der Liste — der Nutzer verschiebt Zeilen, er tippt
// keine Ordnungszahlen.
export async function saveTaskTests(
  taskItemId: string,
  tests: TaskTestDraft[],
  signal?: AbortSignal,
): Promise<ApiResult<TaskTest[]>> {
  const body: Schemas['SaveTaskTestsDto'] = {
    taskItemId,
    tests: tests.map((test, index) => ({
      input: test.input,
      expectedOutput: test.expectedOutput,
      description: test.description,
      order: index + 1,
    })),
  }

  const result = await jsonRequest<Schemas['TaskTestDto'][]>(
    `/api/admin/tasks/${taskItemId}/tests`,
    'PUT',
    body,
    signal,
  )
  return mapResult(result, (dtos) => dtos.map(toTaskTest).sort((a, b) => a.order - b.order))
}

// ── JUnit-Dateien ──────────────────────────────────────────────────────

// Anders als TaskItemDto.visibleUnitTestFiles kommen hier ALLE Dateien mit,
// auch die für Teilnehmer verborgenen. Der Editor braucht die vollständige
// Liste, sonst kann er nicht sagen, ob eine Aufgabe im Modus UnitTestOnly
// überhaupt freigeschaltet werden darf.
export async function fetchTaskUnitTestFiles(
  taskItemId: string,
  signal?: AbortSignal,
): Promise<ApiResult<UnitTestFile[]>> {
  const result = await request<Schemas['TaskUnitTestFileDto'][]>(
    `/api/admin/tasks/${taskItemId}/unittests`,
    { signal },
  )
  return mapResult(result, (dtos) => dtos.map(toUnitTestFile).sort((a, b) => a.order - b.order))
}

export type UnitTestFileDraft = Pick<
  UnitTestFile,
  'fileName' | 'content' | 'isVisibleToParticipant'
>

// Blockspeicherung: was nicht mitkommt, wird gelöscht. Die Reihenfolge ergibt
// sich aus der Position in der Liste.
//
// Achtung: der Server vergibt dabei NEUE Ids (SaveAllAsync). Wer die Antwort
// nicht übernimmt, arbeitet danach mit veralteten Schlüsseln weiter.
export async function saveTaskUnitTestFiles(
  taskItemId: string,
  files: UnitTestFileDraft[],
  signal?: AbortSignal,
): Promise<ApiResult<UnitTestFile[]>> {
  const body: Schemas['SaveTaskUnitTestFilesDto'] = {
    taskItemId,
    files: files.map((file, index) => ({
      fileName: file.fileName.trim(),
      content: file.content,
      order: index + 1,
      isVisibleToParticipant: file.isVisibleToParticipant,
    })),
  }

  const result = await jsonRequest<Schemas['TaskUnitTestFileDto'][]>(
    `/api/admin/tasks/${taskItemId}/unittests`,
    'PUT',
    body,
    signal,
  )
  return mapResult(result, (dtos) => dtos.map(toUnitTestFile).sort((a, b) => a.order - b.order))
}

// ── Gewichte ───────────────────────────────────────────────────────────

export async function fetchTaskWeights(
  taskItemId: string,
  signal?: AbortSignal,
): Promise<ApiResult<TaskCategoryWeight[]>> {
  const result = await request<Schemas['TaskCategoryWeightDto'][]>(
    `/api/admin/tasks/${taskItemId}/weights`,
    { signal },
  )
  return mapResult(result, (dtos) => dtos.map(toTaskCategoryWeight))
}

// Eine leere Liste stellt die Standardgewichte aus der Konfiguration wieder her.
export async function saveTaskWeights(
  taskItemId: string,
  weights: { category: TaskCategoryWeight['category']; weight: number }[],
  signal?: AbortSignal,
): Promise<ApiResult<TaskCategoryWeight[]>> {
  const body: Schemas['SaveTaskCategoryWeightsDto'] = {
    taskItemId,
    weights: weights.map((entry) => ({ category: entry.category, weight: entry.weight })),
  }

  const result = await jsonRequest<Schemas['TaskCategoryWeightDto'][]>(
    `/api/admin/tasks/${taskItemId}/weights`,
    'PUT',
    body,
    signal,
  )
  return mapResult(result, (dtos) => dtos.map(toTaskCategoryWeight))
}
