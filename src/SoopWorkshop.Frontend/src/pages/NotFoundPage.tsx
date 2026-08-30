import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <div className="flex-1 flex items-center justify-center bg-white p-8 text-center">
      <div className="anim-auf max-w-md">
        <p className="font-mono text-xs uppercase tracking-widest text-slate-500 mb-2">
          Fehler 404
        </p>
        <h1 className="text-2xl font-bold text-slate-800 mb-2">Diese Seite gibt es nicht</h1>
        <p className="text-slate-600 mb-6">Der Link stimmt nicht oder die Seite wurde umbenannt.</p>
        <Link
          to="/"
          className="inline-block rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700 hover:-translate-y-0.5 active:translate-y-0"
        >
          Zur Aufgabenliste
        </Link>
      </div>
    </div>
  )
}
