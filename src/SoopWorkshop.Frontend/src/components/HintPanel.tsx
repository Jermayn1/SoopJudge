import { useState } from 'react'
import { ChevronDown, HelpCircle } from 'lucide-react'
import type { Hint } from '../api/types'

export function HintPanel({ hints }: { hints: Hint[] }) {
  const [isOpen, setIsOpen] = useState(false)

  if (hints.length === 0) return null

  return (
    <div className="bg-slate-50 border border-slate-200 rounded-xl overflow-hidden shadow-sm hover:shadow-md transition-all duration-300 dark:bg-neutral-900 dark:border-white/10">
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
        aria-controls="tipps"
        className="w-full flex items-center justify-between p-5 hover:bg-slate-100 transition-colors dark:hover:bg-neutral-800"
      >
        <div className="flex items-center gap-3 font-semibold text-slate-800 dark:text-neutral-100">
          <div className="w-8 h-8 bg-indigo-100 rounded-lg flex items-center justify-center dark:bg-indigo-500/15">
            <HelpCircle className="w-5 h-5 text-indigo-700 dark:text-indigo-200" aria-hidden="true" />
          </div>
          Tipps &amp; Hilfestellungen
        </div>
        <ChevronDown
          className={`w-5 h-5 text-slate-500 transition-transform duration-300 dark:text-neutral-400 ${
            isOpen ? 'rotate-180' : ''
          }`}
          aria-hidden="true"
        />
      </button>

      {/* Eingeklappt bleibt der Bereich im Baum, fährt aber über das Raster
          auf 0fr. "inert" hält die Tipps solange aus der Tab-Reihenfolge. */}
      <div id="tipps" className={`klapp ${isOpen ? '' : 'klapp-zu'}`} inert={!isOpen}>
        <div className="klapp-inhalt">
          <div className="px-5 pb-5 border-t border-slate-100 pt-4 dark:border-white/5">
            <ul className="space-y-3">
              {hints.map((hint, index) => (
                <li
                  key={hint.id}
                  className="anim-links flex gap-3 text-slate-700 leading-relaxed dark:text-neutral-300"
                  style={{ animationDelay: `${index * 70}ms` }}
                >
                  <span className="text-indigo-600 font-bold dark:text-indigo-300" aria-hidden="true">
                    •
                  </span>
                  {hint.content}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </div>
  )
}
