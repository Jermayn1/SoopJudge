import { useEffect, useState } from 'react'
import { ChevronDown, RefreshCw } from 'lucide-react'
import { NavLink, useParams } from 'react-router-dom'
import { BrandMark } from './BrandMark'
import { iconByName } from '../admin/icons'
import type { Category } from '../api/types'

type SidebarProps = {
  categories: Category[]
  loading: boolean
  error: string | null
  onRetry: () => void
}

export function Sidebar({ categories, loading, error, onRetry }: SidebarProps) {
  const { taskId } = useParams()

  // Gemerkt werden die EINGEKLAPPTEN Kategorien, nicht die offenen. Damit ist
  // eine neu hinzukommende Kategorie von selbst offen, statt sich zu
  // verstecken, weil sie beim letzten Mal noch nicht existierte.
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set())

  // Die Kategorie der geöffneten Aufgabe wird aufgeklappt — sonst zeigt die
  // Navigation nicht, wo man gerade steht.
  useEffect(() => {
    if (!taskId) return
    const owner = categories.find((c) => c.tasks.some((t) => t.id === taskId))
    if (!owner) return
    setCollapsed((current) => {
      if (!current.has(owner.id)) return current
      const next = new Set(current)
      next.delete(owner.id)
      return next
    })
  }, [taskId, categories])

  const toggle = (id: string) =>
    setCollapsed((current) => {
      const next = new Set(current)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })

  return (
    <div className="w-64 shrink-0 bg-slate-50 h-screen border-r border-slate-200 flex flex-col overflow-y-auto">
      <div className="p-6 border-b border-slate-200 bg-white">
        <NavLink to="/" className="text-xl font-bold text-slate-800 flex items-center gap-2">
          <BrandMark size={32} />
          Soop Judge
        </NavLink>
      </div>

      <nav className="p-4 space-y-1">
        {loading && (
          <div className="space-y-2" aria-hidden="true">
            {[0, 1, 2].map((i) => (
              <div key={i} className="h-8 rounded-md bg-slate-200 animate-pulse" />
            ))}
          </div>
        )}

        {/* Ein Fehler in der Navigation darf den Rest der Seite nicht mitreißen —
            und "nicht erreichbar" steht hier im Wortlaut, statt als "keine
            Aufgaben vorhanden" verkleidet zu werden. */}
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
            const regionId = `kategorie-${category.id}`
            // Das Symbol steht an der Kategorie. Früher wurde es aus ihrem
            // Namen erraten - beim Umbenennen wechselte es damit stillschweigend.
            const Icon = iconByName(category.iconName)

            return (
              <div key={category.id}>
                <button
                  type="button"
                  onClick={() => toggle(category.id)}
                  aria-expanded={isOpen}
                  aria-controls={regionId}
                  className="w-full flex items-center gap-2 px-3 py-1.5 mb-1 rounded-md text-xs font-semibold text-slate-500 uppercase tracking-wider transition-colors hover:bg-slate-200 hover:text-slate-700"
                >
                  <Icon className="w-4 h-4" aria-hidden="true" />
                  <span className="flex-1 text-left">{category.name}</span>
                  <span
                    className={`text-slate-500 transition-transform duration-200 ${
                      isOpen ? '' : '-rotate-90'
                    }`}
                  >
                    <ChevronDown className="w-4 h-4" aria-hidden="true" />
                  </span>
                </button>

                {/* Der Bereich bleibt im Baum und wird über das Raster von 1fr
                    auf 0fr gefahren — das braucht keine gemessene Höhe.
                    "inert" nimmt den eingeklappten Bereich zusätzlich aus der
                    Tab- und der Vorlesereihenfolge heraus; ohne das blieben die
                    Links darin antabbar, also eine unsichtbare Tastaturfalle. */}
                <div
                  id={regionId}
                  className={`klapp ${isOpen ? '' : 'klapp-zu'}`}
                  inert={!isOpen}
                >
                  <div className="klapp-inhalt space-y-1 pt-1">
                    {category.tasks.map((task) => (
                      <NavLink
                        key={task.id}
                        to={`/aufgaben/${task.id}`}
                        className={({ isActive }) =>
                          `block w-full text-left px-3 py-2 rounded-md text-sm font-medium transition-all ${
                            isActive
                              ? 'bg-indigo-600 text-white shadow-md shadow-indigo-100'
                              : 'text-slate-600 hover:bg-slate-200'
                          }`
                        }
                      >
                        {task.title}
                      </NavLink>
                    ))}
                    {category.tasks.length === 0 && (
                      <p className="px-3 py-2 text-sm text-slate-500 italic">
                        Noch keine Aufgaben freigeschaltet.
                      </p>
                    )}
                  </div>
                </div>
              </div>
            )
          })}

        {!loading && !error && categories.length === 0 && (
          <p className="px-3 py-4 text-slate-500 text-sm italic">
            Es sind noch keine Aufgaben sichtbar geschaltet.
          </p>
        )}
      </nav>
    </div>
  )
}
