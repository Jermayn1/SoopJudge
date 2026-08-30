import type { components } from '../../api/schema'
import { jsonRequest, request, type ApiResult } from '../../api/client'

type Schemas = components['schemas']

export type TaskBundle = Schemas['TaskBundleDto']
export type ImportMode = Schemas['ImportMode']

// Der Bericht, aber mit gefüllten Feldern statt lauter optionaler. .NET setzt
// im Vertrag kein "required", also wäre hier sonst alles optional — dasselbe
// Muster wie in api/mappers.ts.
export type ImportReport = {
  errors: string[]
  warnings: string[]
  categoriesCreated: number
  categoriesUpdated: number
  categoriesDeleted: number
  tasksCreated: number
  tasksUpdated: number
  tasksDeleted: number
  submissionsDeleted: number
}

function toNumber(value: number | string | undefined): number {
  if (typeof value === 'number') return value
  if (typeof value === 'string') {
    const parsed = Number.parseInt(value, 10)
    return Number.isNaN(parsed) ? 0 : parsed
  }
  return 0
}

function toReport(dto: Schemas['ImportReportDto']): ImportReport {
  return {
    errors: dto.errors ?? [],
    warnings: dto.warnings ?? [],
    categoriesCreated: toNumber(dto.categoriesCreated),
    categoriesUpdated: toNumber(dto.categoriesUpdated),
    categoriesDeleted: toNumber(dto.categoriesDeleted),
    tasksCreated: toNumber(dto.tasksCreated),
    tasksUpdated: toNumber(dto.tasksUpdated),
    tasksDeleted: toNumber(dto.tasksDeleted),
    submissionsDeleted: toNumber(dto.submissionsDeleted),
  }
}

export function fetchBundle(signal?: AbortSignal): Promise<ApiResult<TaskBundle>> {
  return request<TaskBundle>('/api/admin/transfer/export', { signal })
}

export async function previewImport(
  bundle: TaskBundle,
  mode: ImportMode,
  signal?: AbortSignal,
): Promise<ApiResult<ImportReport>> {
  const result = await jsonRequest<Schemas['ImportReportDto']>(
    '/api/admin/transfer/import/preview',
    'POST',
    { bundle, mode },
    signal,
  )
  return result.kind === 'ok' ? { kind: 'ok', value: toReport(result.value) } : result
}

export async function runImport(
  bundle: TaskBundle,
  mode: ImportMode,
  signal?: AbortSignal,
): Promise<ApiResult<ImportReport>> {
  const result = await jsonRequest<Schemas['ImportReportDto']>(
    '/api/admin/transfer/import',
    'POST',
    { bundle, mode },
    signal,
  )
  return result.kind === 'ok' ? { kind: 'ok', value: toReport(result.value) } : result
}

// Bietet die Datei zum Speichern an.
//
// Der Umweg über fetch und einen Blob ist Absicht: ein einfacher Link auf den
// Endpunkt würde bei abgelaufener Anmeldung eine weiße Seite mit 401 zeigen.
// So läuft der Abruf durch dieselbe Kette wie alles andere und ein Fehler
// erreicht den Nutzer als Satz.
export function offerDownload(bundle: TaskBundle): void {
  // Eingerückt gespeichert: die Datei soll sich in Git lesen und diffen lassen.
  const blob = new Blob([JSON.stringify(bundle, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)

  const heute = new Date().toISOString().slice(0, 10)
  const link = document.createElement('a')
  link.href = url
  link.download = `soop-bestand-${heute}.json`
  document.body.append(link)
  link.click()
  link.remove()

  // Sonst hält der Browser den Blob bis zum Neuladen der Seite fest.
  URL.revokeObjectURL(url)
}
