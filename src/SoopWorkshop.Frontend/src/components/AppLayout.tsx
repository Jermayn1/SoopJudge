import { useCallback, useEffect, useRef, useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { Menu, X } from 'lucide-react'
import { Sidebar } from './Sidebar'
import { BrandMark } from './BrandMark'
import { fetchCategories } from '../api/endpoints'
import type { Category } from '../api/types'

export function AppLayout() {
  const [categories, setCategories] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [attempt, setAttempt] = useState(0)
  const [menuOpen, setMenuOpen] = useState(false)

  const location = useLocation()
  const closeButtonRef = useRef<HTMLButtonElement>(null)

  const retry = useCallback(() => setAttempt((n) => n + 1), [])

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError(null)

    fetchCategories(controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return
        if (result.kind === 'ok') setCategories(result.value)
        else setError(result.message)
      })
      .catch((cause) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') return
        setError('Die Aufgabenliste konnte nicht geladen werden.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })

    return () => controller.abort()
  }, [attempt])

  // Nach einem Klick auf eine Aufgabe soll die Überlagerung nicht stehen
  // bleiben — sonst verdeckt sie genau das, was man sehen wollte.
  useEffect(() => setMenuOpen(false), [location.pathname])

  useEffect(() => {
    if (!menuOpen) return

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setMenuOpen(false)
    }

    window.addEventListener('keydown', onKeyDown)
    // Den Fokus in die Überlagerung holen, sonst tabbt man ins Nichts.
    closeButtonRef.current?.focus()

    return () => window.removeEventListener('keydown', onKeyDown)
  }, [menuOpen])

  return (
    <div className="flex w-full h-screen bg-white font-sans text-slate-900">
      {/* Ab lg steht die Leiste fest. Darunter — schmales Fenster, geteilter
          Bildschirm, kleines Notebook — wird sie zur Überlagerung. */}
      <div className="hidden lg:flex">
        <Sidebar categories={categories} loading={loading} error={error} onRetry={retry} />
      </div>

      {menuOpen && (
        <>
          <button
            type="button"
            aria-label="Menü schließen"
            onClick={() => setMenuOpen(false)}
            className="fixed inset-0 z-40 bg-slate-900/40 lg:hidden"
          />
          <div className="fixed inset-y-0 left-0 z-50 shadow-2xl lg:hidden">
            <button
              ref={closeButtonRef}
              type="button"
              onClick={() => setMenuOpen(false)}
              aria-label="Menü schließen"
              className="absolute right-2 top-2 z-10 rounded-lg p-2 text-slate-600 hover:bg-slate-200"
            >
              <X className="w-5 h-5" aria-hidden="true" />
            </button>
            <Sidebar categories={categories} loading={loading} error={error} onRetry={retry} />
          </div>
        </>
      )}

      <div className="flex flex-1 flex-col min-w-0">
        {/* Kopfzeile nur unterhalb von lg — auf dem Laptop ist die Leiste ja da. */}
        <header className="flex items-center gap-3 border-b border-slate-200 px-4 py-3 lg:hidden">
          <button
            type="button"
            onClick={() => setMenuOpen(true)}
            aria-label="Aufgabenliste öffnen"
            aria-expanded={menuOpen}
            className="rounded-lg p-2 text-slate-700 hover:bg-slate-100"
          >
            <Menu className="w-5 h-5" aria-hidden="true" />
          </button>
          <span className="flex items-center gap-2 font-bold text-slate-800">
            <BrandMark size={24} />
            Soop Judge
          </span>
        </header>

        {/* Solange die Überlagerung offen ist, ist der Inhalt dahinter weder
            anklickbar noch mit der Tastatur erreichbar. Ohne das tabbt man aus
            dem Menü heraus in verdeckte Bedienelemente. */}
        <main className="flex flex-1 flex-col min-h-0 overflow-hidden" inert={menuOpen}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
