import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, Outlet, useLocation } from 'react-router-dom'
import { ArrowLeft, LogOut, Menu, X } from 'lucide-react'
import { AdminSidebar } from './AdminSidebar'
import { BrandMark } from '../../components/BrandMark'
import { ThemeToggle } from '../../theme/ThemeToggle'
import { fetchAdminCategories } from '../api/catalog'
import { useAdminSessionContext, type AdminCatalogContext } from '../adminOutlet'
import type { Category } from '../../api/types'

// Rahmen des Verwaltungsbereichs. Lädt den Bestand einmal und reicht ihn nach
// unten weiter — die Seitenleiste und die Seiten darunter zeigen dieselben
// Daten, also soll auch nur einmal geladen werden.
export function AdminLayout() {
  const { signOut } = useAdminSessionContext()

  const [categories, setCategories] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [attempt, setAttempt] = useState(0)
  const [menuOpen, setMenuOpen] = useState(false)

  const location = useLocation()
  const closeButtonRef = useRef<HTMLButtonElement>(null)

  const reload = useCallback(() => setAttempt((n) => n + 1), [])

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError(null)

    fetchAdminCategories(controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return
        if (result.kind === 'ok') setCategories(result.value)
        else setError(result.message)
      })
      .catch((cause) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') return
        setError('Der Aufgabenbestand konnte nicht geladen werden.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })

    return () => controller.abort()
  }, [attempt])

  useEffect(() => setMenuOpen(false), [location.pathname])

  useEffect(() => {
    if (!menuOpen) return

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setMenuOpen(false)
    }

    window.addEventListener('keydown', onKeyDown)
    closeButtonRef.current?.focus()

    return () => window.removeEventListener('keydown', onKeyDown)
  }, [menuOpen])

  const context: AdminCatalogContext = { signOut, categories, loading, error, reload }

  return (
    <div className="flex h-screen w-full bg-white font-sans text-slate-900 dark:bg-neutral-950 dark:text-neutral-100">
      <div className="hidden lg:flex">
        <AdminSidebar categories={categories} loading={loading} error={error} onRetry={reload} />
      </div>

      {menuOpen && (
        <>
          <button
            type="button"
            aria-label="Menü schließen"
            onClick={() => setMenuOpen(false)}
            className="fixed inset-0 z-40 bg-slate-900/40 lg:hidden dark:bg-black/60"
          />
          <div className="fixed inset-y-0 left-0 z-50 shadow-2xl lg:hidden">
            <button
              ref={closeButtonRef}
              type="button"
              onClick={() => setMenuOpen(false)}
              aria-label="Menü schließen"
              className="absolute right-2 top-2 z-10 rounded-lg p-2 text-slate-600 hover:bg-slate-200 dark:text-neutral-300 dark:hover:bg-neutral-800"
            >
              <X className="w-5 h-5" aria-hidden="true" />
            </button>
            <AdminSidebar
              categories={categories}
              loading={loading}
              error={error}
              onRetry={reload}
            />
          </div>
        </>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        {/* Gesperrt, solange das Menü offen ist — wie der Inhalt darunter.
            Sonst tabbt man hinter der Überlagerung in die Kopfzeile. */}
        <header
          className="flex items-center gap-3 border-b border-slate-200 px-4 py-3 dark:border-white/10"
          inert={menuOpen}
        >
          <button
            type="button"
            onClick={() => setMenuOpen(true)}
            aria-label="Navigation öffnen"
            aria-expanded={menuOpen}
            className="rounded-lg p-2 text-slate-700 hover:bg-slate-100 lg:hidden dark:text-neutral-300 dark:hover:bg-neutral-800"
          >
            <Menu className="w-5 h-5" aria-hidden="true" />
          </button>

          <span className="flex items-center gap-2 font-bold text-slate-800 lg:hidden dark:text-neutral-100">
            <BrandMark size={24} />
            Soop Judge
          </span>

          <div className="ml-auto flex items-center gap-2 sm:gap-4">
            <Link
              to="/"
              className="hidden items-center gap-1.5 text-sm font-medium text-slate-600 transition-colors hover:text-slate-900 hover:underline sm:flex dark:text-neutral-400 dark:hover:text-neutral-100"
            >
              <ArrowLeft className="w-4 h-4" aria-hidden="true" />
              Teilnehmersicht
            </Link>

            <ThemeToggle variant="icon" />

            <button
              type="button"
              onClick={signOut}
              className="flex items-center gap-1.5 rounded-xl border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100 dark:border-white/15 dark:text-neutral-300 dark:hover:bg-neutral-800"
            >
              <LogOut className="w-4 h-4" aria-hidden="true" />
              Abmelden
            </button>
          </div>
        </header>

        <main className="flex min-h-0 flex-1 flex-col overflow-hidden" inert={menuOpen}>
          <Outlet context={context} />
        </main>
      </div>
    </div>
  )
}
