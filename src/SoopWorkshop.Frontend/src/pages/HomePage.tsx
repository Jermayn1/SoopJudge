import { FileText } from 'lucide-react'

export function HomePage() {
  return (
    <div className="flex-1 flex items-center justify-center bg-white p-8 text-center">
      <div className="anim-auf max-w-md">
        <div className="w-24 h-24 bg-slate-50 rounded-3xl flex items-center justify-center mx-auto mb-6">
          <FileText className="w-10 h-10 text-slate-400" aria-hidden="true" />
        </div>
        <h1 className="text-2xl font-bold text-slate-800 mb-2">
          Bereit für die nächste Herausforderung?
        </h1>
        <p className="text-slate-600">Wähle links eine Aufgabe aus der Liste aus, um zu beginnen.</p>
      </div>
    </div>
  )
}
