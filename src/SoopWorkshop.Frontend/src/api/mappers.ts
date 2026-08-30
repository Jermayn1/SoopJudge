import type { components } from './schema'
import type {
  Category,
  CategoryResult,
  EvaluationResult,
  ExpectedType,
  Hint,
  Submission,
  SubmissionState,
  Task,
  TaskCategoryWeight,
  TaskTest,
  TestCaseResult,
  UnitTestFile,
} from './types'

type Schemas = components['schemas']

// Setzt die erzeugten Vertragstypen in die sauberen Typen aus types.ts um.
//
// Alles hier ist reine Übersetzung ohne Fachlogik. Die Standardwerte sind
// bewusst harmlos: das Backend füllt jedes dieser Felder, der Vertrag sagt
// nur nicht, dass es das tut (kein "required" in der Ausgabe von .NET).

// ASP.NET gibt int32 im Vertrag als "integer | string" an, weil es beim
// Binden auch Zahlen als Zeichenkette annimmt. In Antworten kommt immer eine
// Zahl — hier trotzdem beide Fälle behandeln, statt es zu glauben.
export function toNumber(value: number | string | undefined, fallback = 0): number {
  if (typeof value === 'number') return value
  if (typeof value === 'string') {
    const parsed = Number.parseInt(value, 10)
    return Number.isNaN(parsed) ? fallback : parsed
  }
  return fallback
}

function toHint(dto: Schemas['TaskHintDto']): Hint {
  return {
    id: dto.id ?? '',
    content: dto.content ?? '',
    order: toNumber(dto.order),
  }
}

export function toUnitTestFile(dto: Schemas['TaskUnitTestFileDto']): UnitTestFile {
  return {
    id: dto.id ?? '',
    fileName: dto.fileName ?? '',
    content: dto.content ?? '',
    order: toNumber(dto.order),
    isVisibleToParticipant: dto.isVisibleToParticipant ?? false,
  }
}

export function toTaskTest(dto: Schemas['TaskTestDto']): TaskTest {
  return {
    id: dto.id ?? '',
    taskItemId: dto.taskItemId ?? '',
    input: dto.input ?? '',
    expectedOutput: dto.expectedOutput ?? '',
    description: dto.description ?? '',
    order: toNumber(dto.order),
  }
}

export function toTaskCategoryWeight(
  dto: Schemas['TaskCategoryWeightDto'],
): TaskCategoryWeight {
  return {
    id: dto.id ?? '',
    taskItemId: dto.taskItemId ?? '',
    category: dto.category ?? 'CleanCode',
    // Gewichte sind Kommazahlen, nicht ganzzahlig — toNumber würde die
    // Nachkommastellen abschneiden.
    weight: typeof dto.weight === 'number' ? dto.weight : 0,
  }
}

function toExpectedType(dto: Schemas['TaskExpectedTypeDto']): ExpectedType {
  return {
    id: dto.id ?? '',
    name: dto.name ?? '',
    order: toNumber(dto.order),
    methods: dto.methods ?? [],
  }
}

export function toTask(dto: Schemas['TaskItemDto']): Task {
  return {
    id: dto.id ?? '',
    categoryId: dto.taskCategoryId ?? '',
    title: dto.title ?? '',
    description: dto.description ?? '',
    difficulty: dto.difficulty ?? 'Easy',
    order: toNumber(dto.order),
    isVisible: dto.isVisible ?? false,
    evaluationMode: dto.evaluationMode ?? 'ConsoleOnly',
    expectedTypes: (dto.expectedTypes ?? [])
      .map(toExpectedType)
      .sort((a, b) => a.order - b.order),
    // Die API liefert bereits sortiert. Das Frontend sortiert trotzdem selbst:
    // Sortieren ist billig, eine wechselnde Reihenfolge verwirrt.
    hints: (dto.hints ?? []).map(toHint).sort((a, b) => a.order - b.order),
    visibleUnitTestFiles: (dto.visibleUnitTestFiles ?? [])
      .map(toUnitTestFile)
      .sort((a, b) => a.order - b.order),
  }
}

export function toCategory(dto: Schemas['TaskCategoryDto']): Category {
  return {
    id: dto.id ?? '',
    name: dto.name ?? '',
    order: toNumber(dto.order),
    isVisible: dto.isVisible ?? false,
    iconName: dto.iconName ?? '',
    tasks: (dto.tasks ?? []).map(toTask).sort((a, b) => a.order - b.order),
  }
}

function toTestCaseResult(dto: Schemas['TestCaseResultDto']): TestCaseResult {
  return {
    id: dto.id ?? '',
    description: dto.description ?? '',
    input: dto.input ?? '',
    expectedOutput: dto.expectedOutput ?? '',
    actualOutput: dto.actualOutput ?? '',
    passed: dto.passed ?? false,
    order: toNumber(dto.order),
  }
}

function toCategoryResult(dto: Schemas['CategoryResultDto']): CategoryResult {
  return {
    id: dto.id ?? '',
    category: dto.category ?? 'CleanCode',
    passed: dto.passed ?? false,
    points: toNumber(dto.points),
    maxPoints: toNumber(dto.maxPoints),
    errorTip: dto.errorTip ?? '',
    testCaseResults: (dto.testCaseResults ?? [])
      .map(toTestCaseResult)
      .sort((a, b) => a.order - b.order),
  }
}

export function toEvaluationResult(dto: Schemas['EvaluationResultDto']): EvaluationResult {
  return {
    id: dto.id ?? '',
    submissionId: dto.submissionId ?? '',
    totalScore: toNumber(dto.totalScore),
    maxScore: toNumber(dto.maxScore, 100),
    categoryResults: (dto.categoryResults ?? []).map(toCategoryResult),
  }
}

export function toSubmission(dto: Schemas['SubmissionDto']): Submission {
  return {
    id: dto.id ?? '',
    taskItemId: dto.taskItemId ?? '',
    submittedAt: dto.submittedAt ?? '',
    status: dto.status ?? 'Pending',
  }
}

export function toSubmissionState(dto: Schemas['SubmissionStatusDto']): SubmissionState {
  return {
    id: dto.id ?? '',
    taskItemId: dto.taskItemId ?? '',
    status: dto.status ?? 'Pending',
    submittedAt: dto.submittedAt ?? '',
    errorMessage: dto.errorMessage ?? '',
  }
}
