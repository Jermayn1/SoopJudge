import { HintPanel } from './HintPanel'
import { TaskMarkdown } from './TaskMarkdown'
import { DIFFICULTY_CLASSES, DIFFICULTY_LABELS, MODE_LABELS } from '../api/labels'
import type { Task } from '../api/types'

type TaskViewProps = {
  task: Task
}

// Die Aufgabe, wie der Teilnehmer sie liest — ohne den Abgabeteil.
//
// Herausgelöst aus TaskPage, damit die Vorschau im Verwaltungsbereich genau
// dieselbe Darstellung benutzt. Eine nachgebaute Vorschau wäre wertlos: sie
// würde beim ersten Umbau der Teilnehmersicht auseinanderlaufen, und man
// merkte es erst, wenn ein Teilnehmer etwas anderes sieht als angekündigt.
//
// Die Komponente lädt nichts. Wer sie benutzt, reicht die Aufgabe hinein -
// die Vorschau also mit denselben Filtern, die die öffentliche API anlegt.
export function TaskView({ task }: TaskViewProps) {
  const hasContract = task.expectedTypes.length > 0

  return (
    <>
      <header className="mb-10 border-b pb-8 border-slate-100">
        <div className="anim-auf flex items-center gap-2 mb-3 flex-wrap">
          <span
            className={`px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider ${DIFFICULTY_CLASSES[task.difficulty]}`}
          >
            {DIFFICULTY_LABELS[task.difficulty]}
          </span>
          <span className="px-3 py-1 bg-slate-100 text-slate-700 rounded-full text-xs font-bold uppercase tracking-wider">
            {MODE_LABELS[task.evaluationMode]}
          </span>
        </div>
        <h1
          className="anim-links text-4xl font-extrabold text-slate-900 leading-tight"
          style={{ animationDelay: '90ms' }}
        >
          {task.title}
        </h1>
      </header>

      <section style={{ animationDelay: '180ms' }} className="anim-ein mb-10">
        <div className="bg-slate-50/50 p-8 rounded-2xl border border-slate-100 shadow-sm">
          <TaskMarkdown>{task.description}</TaskMarkdown>
        </div>
      </section>

      {/* Der Aufgaben-Vertrag wurde bisher nie angezeigt — der ContractChecker
          bewertete also gegen eine Vorgabe, die der Teilnehmer nicht lesen
          konnte. */}
      {hasContract && (
        <section className="anim-auf mb-10" style={{ animationDelay: '260ms' }}>
          <h2 className="text-sm font-bold uppercase tracking-wider text-slate-500 mb-3">
            Was geprüft wird
          </h2>
          {/* Je geforderte Klasse ein Block mit IHREN Methoden. Eine flache
              Liste daneben ließe offen, welche Methode wohin gehört — und
              genau danach wird bewertet. */}
          <dl className="rounded-2xl border border-slate-200 bg-white divide-y divide-slate-100 shadow-sm">
            {task.expectedTypes.map((type) => (
              <div key={type.id} className="flex flex-col sm:flex-row gap-1 sm:gap-4 px-5 py-3">
                <dt className="font-mono text-sm text-slate-900 sm:w-40 shrink-0">{type.name}</dt>
                <dd className="min-w-0 space-y-1 font-mono text-sm text-slate-700">
                  {type.methods.length === 0 ? (
                    <span className="font-sans text-slate-500 italic">
                      keine bestimmten Methoden gefordert
                    </span>
                  ) : (
                    type.methods.map((signature) => (
                      <div key={signature} className="break-words">
                        {signature}
                      </div>
                    ))
                  )}
                </dd>
              </div>
            ))}
          </dl>
        </section>
      )}

      {task.visibleUnitTestFiles.length > 0 && (
        <section className="mb-10">
          <h2 className="text-sm font-bold uppercase tracking-wider text-slate-500 mb-3">
            Diese Tests laufen gegen deine Abgabe
          </h2>
          <div className="space-y-3">
            {task.visibleUnitTestFiles.map((file) => (
              <details
                key={file.id}
                className="rounded-2xl border border-slate-200 overflow-hidden shadow-sm"
              >
                <summary className="cursor-pointer px-5 py-3 font-mono text-sm bg-slate-50 hover:bg-slate-100 transition-colors">
                  {file.fileName}
                </summary>
                <pre className="overflow-x-auto bg-slate-800 p-4 text-xs leading-relaxed text-slate-100">
                  {file.content}
                </pre>
              </details>
            ))}
          </div>
        </section>
      )}

      <section className="mb-10">
        <HintPanel hints={task.hints} />
      </section>
    </>
  )
}
