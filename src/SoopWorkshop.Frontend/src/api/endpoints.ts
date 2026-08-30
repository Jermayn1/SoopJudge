import type { components } from './schema'
import { request, type ApiResult } from './client'
import {
  toCategory,
  toEvaluationResult,
  toSubmission,
  toSubmissionState,
  toTask,
} from './mappers'
import type { Category, EvaluationResult, Submission, SubmissionState, Task } from './types'

type Schemas = components['schemas']

function map<A, B>(result: ApiResult<A>, project: (value: A) => B): ApiResult<B> {
  return result.kind === 'ok' ? { kind: 'ok', value: project(result.value) } : result
}

export async function fetchCategories(signal?: AbortSignal): Promise<ApiResult<Category[]>> {
  const result = await request<Schemas['TaskCategoryDto'][]>('/api/categories', { signal })
  return map(result, (dtos) => dtos.map(toCategory).sort((a, b) => a.order - b.order))
}

export async function fetchTask(id: string, signal?: AbortSignal): Promise<ApiResult<Task>> {
  const result = await request<Schemas['TaskItemDto']>(`/api/tasks/${id}`, { signal })
  return map(result, toTask)
}

export async function createSubmission(
  taskItemId: string,
  files: File[],
  signal?: AbortSignal,
): Promise<ApiResult<Submission>> {
  const form = new FormData()
  form.append('taskItemId', taskItemId)
  for (const file of files) form.append('files', file, file.name)

  // Bewusst kein Content-Type gesetzt: den muss der Browser selbst bilden,
  // weil er die multipart-Grenze enthält.
  const result = await request<Schemas['SubmissionDto']>('/api/submissions', {
    method: 'POST',
    body: form,
    signal,
  })
  return map(result, toSubmission)
}

export async function fetchSubmissionState(
  id: string,
  signal?: AbortSignal,
): Promise<ApiResult<SubmissionState>> {
  const result = await request<Schemas['SubmissionStatusDto']>(`/api/submissions/${id}/status`, {
    signal,
  })
  return map(result, toSubmissionState)
}

export async function fetchEvaluationResult(
  id: string,
  signal?: AbortSignal,
): Promise<ApiResult<EvaluationResult>> {
  const result = await request<Schemas['EvaluationResultDto']>(`/api/submissions/${id}/result`, {
    signal,
  })
  return map(result, toEvaluationResult)
}
