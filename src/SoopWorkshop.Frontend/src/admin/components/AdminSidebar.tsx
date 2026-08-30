import { useState } from 'react'
import {
  ArrowDownUp,
  ChevronDown,
  EyeOff,
  FolderTree,
  Inbox,
  LayoutList,
  RefreshCw,
} from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { BrandMark } from '../../components/BrandMark'
import { iconByName } from '../icons'
import type { Category } from '../../api/types'

type AdminSidebarProps = {
  categories: Category[]
  loading: boolean
  error: string | null
  onRetry: () => void
}

// Eigene Leiste, nicht die der Teilnehmersicht wiederverwendet: hier stehen
// auch die verborgenen Kategorien und Aufgaben, und die Ziele sind die
// Bearbeitungsseiten statt der Aufgabenstellung.
export function AdminSidebar({ categories, loading, error, onRetry }: AdminSidebarProps) {
  // Gemerkt werden die EINGEKLAPPTEN, nicht die offenen — so ist eine neu
  // angelegte Kategorie von selbst offen, statt sich zu verstecken.
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set())

  const toggle = (id: string) =>
    setCollapsed((current) => {
      const next = new Set(current)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    `block w-full text-left px-3 py-2 rounded-md text-sm font-medium transition-all ${
      isActive ? 'bg-indigo-600 text-white shadow-md shadow-indigo-100' : 'text-slate-600 hover:bg-slate-200'
    }`

  return (
    <div className="flex h-screen w-64 shrink-0 flex-col overflow-y-auto border-r border-slate-200 bg-slate-50">
      <div className="border-b border-slate-200 bg-white p-6">
        <NavLink to="/admin" className="flex items-center gap-2 text-xl font-bold text-slate-800">
          <BrandMark size={32} />
          Soop Judge
        </NavLink>
        {/* Kennzeichnung in Slate. Grün und Rot sind im ganzen Projekt für
            Bewertungen reserviert — ein grüner Rahmen hier hieße "bestanden". */}
        <span className="mt-2 inline-block rounded-md bg-slate-100 px-2 py-0.5 text-xs font-semibold uppercase tracking-wider text-slate-600">
          Verwaltung
        </span>
      </div>

      <nav className="space-y-1 p-4">
        <NavLink to="/admin" end className={linkClass}>
          <span className="flex items-center gap-2">
            <LayoutList className="w-4 h-4" aria-hidden="true" />
            Übersicht
          </span>
        </NavLink>

        <NavLink to="/admin/kategorien" className={linkClass}>
          <span className="flex items-center gap-2">
            <FolderTree className="w-4 h-4" aria-hidden="true" />
            Kategorien
          </span>
        </NavLink>

        <NavLink to="/admin/abgaben" className={linkClass}>
          <span className="flex items-center gap-2">
            <Inbox className="w-4 h-4" aria-hidden="true" />
            Abgaben
          </span>
        </NavLink>

        <NavLink to="/admin/transfer" className={linkClass}>
          <span className="flex items-center gap-2">
            <ArrowDownUp className="w-4 h-4" aria-hidden="true" />
            Transfer
          </span>
        </NavLink>

        <p className="px-3 pb-1 pt-4 text-xs font-semibold uppercase tracking-wider text-slate-500">
          Aufgaben
        </p>

        {loading && (
          <div className="space-y-2" aria-hidden="true">
            {[0, 1, 2].map((i) => (
              <div key={i} className="h-8 rounded-md bg-slate-200 animate-pulse" />
            ))}
          </div>
        )}

        {error && !loading && (
          <div className="rounded-lg border border-rose-200 bg-rose-50 p-3">
            <p className="text-sm text-rose-800">{error}</p>
            <button
              type="button"
              onClick={onRetry}
              className="mt-2 flex items-center gap-1.5 text-sm font-semibold text-rose-800 hover:underline"
            >
              <RefreshCw className="w-3.5 h-3.5" aria-hidden="true" />
              Erneut versuchen
            </button>
          </div>
        )}

        {!loading &&
          !error &&
          categories.map((category) => {
            const isOpen = !collapsed.has(category.id)
            const regionId = `admin-kategorie-${category.id}`
            const Icon = iconByName(category.iconName)

            return (
              <div key={category.id}>
                <button
                  type="button"
                  onClick={() => toggle(category.id)}
                  aria-expanded={isOpen}
                  aria-controls={regionId}
                  className="mb-1 flex w-full items-center gap-2 rounded-md px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-slate-500 transition-colors hover:bg-slate-200 hover:text-slate-700"
                >
                  <Icon className="w-4 h-4 shrink-0" aria-hidden="true" />
                  <span className="flex-1 text-left">{category.name}</span>
                  {!category.isVisible && (
                    <EyeOff className="w-3.5 h-3.5 shrink-0" aria-label="verborgen" />
                  )}
                  <span
                    className={`text-slate-500 transition-transform duration-200 ${isOpen ? '' : '-rotate-90'}`}
                  >
                    <ChevronDown className="w-4 h-4" aria-hidden="true" />
                  </span>
                </button>

                {/* Wie in der Teilnehmerleiste: das Raster fährt von 1fr auf
                    0fr, und "inert" nimmt den eingeklappten Bereich aus der
                    Tab-Reihenfolge. Ohne das blieben die Links antabbar. */}
                <div id={regionId} className={`klapp ${isOpen ? '' : 'klapp-zu'}`} inert={!isOpen}>
                  <div className="klapp-inhalt space-y-1 pt-1">
                    {category.tasks.map((task) => (
                      <NavLink
                        key={task.id}
                        to={`/admin/aufgaben/${task.id}`}
                        className={linkClass}
                      >
                        <span className="flex items-center gap-2">
                          <span className="flex-1 truncate">{task.title}</span>
                          {!task.isVisible && (
                            <EyeOff className="w-3.5 h-3.5 shrink-0" aria-label="verborgen" />
                          )}
                        </span>
                      </NavLink>
                    ))}
                    {category.tasks.length === 0 && (
                      <p className="px-3 py-2 text-sm italic text-slate-500">
                        Noch keine Aufgaben.
                      </p>
                    )}
                  </div>
                </div>
              </div>
            )
          })}

        {!loading && !error && categories.length === 0 && (
          <p className="px-3 py-4 text-sm italic text-slate-500">
            Es gibt noch keine Kategorien.
          </p>
        )}
      </nav>
    </div>
  )
}
