import { FileText } from 'lucide-react'

export function HomePage() {
  return (
    <div className="flex-1 flex items-center justify-center bg-white p-8 text-center dark:bg-neutral-950">
      <div className="anim-auf max-w-md">
        <div className="w-24 h-24 bg-slate-50 rounded-3xl flex items-center justify-center mx-auto mb-6 dark:bg-neutral-900">
          <FileText className="w-10 h-10 text-slate-400 dark:text-neutral-500" aria-hidden="true" />
        </div>
        <h1 className="text-2xl font-bold text-slate-800 mb-2 dark:text-neutral-100">
          Bereit für die nächste Herausforderung?
        </h1>
        <p className="text-slate-600 dark:text-neutral-400">Wähle links eine Aufgabe aus der Liste aus, um zu beginnen.</p>
      </div>
    </div>
  )
}
