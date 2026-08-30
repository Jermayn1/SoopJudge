import type { components } from '../../api/schema'
import { jsonRequest, request, type ApiResult } from '../../api/client'
import { toCategory } from '../../api/mappers'
import type { Category } from '../../api/types'

type Schemas = components['schemas']

// Gleicher Helfer wie in api/endpoints.ts: hebt eine Umsetzung in ApiResult
// hinein, ohne die Fehlerfälle anzufassen. Die bleiben unterscheidbar.
export function mapResult<A, B>(result: ApiResult<A>, project: (value: A) => B): ApiResult<B> {
  return result.kind === 'ok' ? { kind: 'ok', value: project(result.value) } : result
}

// Anders als GET /api/categories liefert dieser Endpunkt auch die verborgenen
// Kategorien und Aufgaben — die Verwaltung muss ja gerade das sehen, was der
// Teilnehmer (noch) nicht sieht.
//
// Was hier NICHT mitkommt: Hints, erwartete Methoden, Testfälle, JUnit-Dateien
// und Gewichte. Der Service füllt die Aufgaben in der Kategorie nur mit den
// Skalarfeldern. Für eine Übersicht reicht das; der Aufgaben-Editor holt sich
// den Rest über die eigenen Endpunkte.
export async function fetchAdminCategories(signal?: AbortSignal): Promise<ApiResult<Category[]>> {
  const result = await request<Schemas['TaskCategoryDto'][]>('/api/admin/categories', { signal })
  return mapResult(result, (dtos) => dtos.map(toCategory).sort((a, b) => a.order - b.order))
}

export async function createCategory(
  name: string,
  order: number,
  signal?: AbortSignal,
): Promise<ApiResult<Category>> {
  // Kein IsVisible im Anlege-DTO: der Service setzt neue Kategorien hart auf
  // verborgen. Freigeschaltet wird bewusst in einem zweiten Schritt.
  const body: Schemas['CreateTaskCategoryDto'] = { name, order }
  const result = await jsonRequest<Schemas['TaskCategoryDto']>(
    '/api/admin/categories',
    'POST',
    body,
    signal,
  )
  return mapResult(result, toCategory)
}

export async function updateCategory(
  category: Pick<Category, 'id' | 'name' | 'order' | 'isVisible' | 'iconName'>,
  signal?: AbortSignal,
): Promise<ApiResult<Category>> {
  const body: Schemas['UpdateTaskCategoryDto'] = {
    id: category.id,
    name: category.name,
    order: category.order,
    isVisible: category.isVisible,
    iconName: category.iconName,
  }
  const result = await jsonRequest<Schemas['TaskCategoryDto']>(
    `/api/admin/categories/${category.id}`,
    'PUT',
    body,
    signal,
  )
  return mapResult(result, toCategory)
}

// Achtung: löscht per Cascade die gesamte Kategorie samt Aufgaben, Testfällen,
// JUnit-Dateien, Gewichten UND allen Abgaben darunter. Der Aufrufer muss das
// vorher sagen.
export function deleteCategory(id: string, signal?: AbortSignal): Promise<ApiResult<void>> {
  return jsonRequest<void>(`/api/admin/categories/${id}`, 'DELETE', undefined, signal)
}

// Umschalten statt Setzen: der Endpunkt kennt nur PATCH ohne Rumpf und
// antwortet mit dem Zustand danach. Genau der wird übernommen, statt ihn im
// Frontend zu erraten — beim Einschalten kann der Server ablehnen, wenn die
// Testdaten zum Auswertungsmodus fehlen.
export function toggleCategoryVisibility(
  id: string,
  signal?: AbortSignal,
): Promise<ApiResult<Schemas['VisibilityStateDto']>> {
  return jsonRequest<Schemas['VisibilityStateDto']>(
    `/api/admin/categories/${id}/visibility`,
    'PATCH',
    undefined,
    signal,
  )
}
