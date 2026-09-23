import { Moon, Sun } from 'lucide-react'
import { useTheme } from './useTheme'

type ThemeToggleProps = {
  // "switch" für die Fußzeile der Seitenleiste, wo Platz für eine
  // Beschriftung ist. "icon" für die Kopfzeilen.
  variant: 'switch' | 'icon'
}

export function ThemeToggle({ variant }: ThemeToggleProps) {
  const { theme, toggleTheme } = useTheme()
  const dark = theme === 'dark'

  if (variant === 'icon') {
    // Ein Symbolknopf nennt die Handlung, nicht den Zustand. "Dunkles Design"
    // allein ließe offen, ob ein Klick es ein- oder ausschaltet.
    const label = dark ? 'Helles Design einschalten' : 'Dunkles Design einschalten'
    const Icon = dark ? Sun : Moon

    return (
      <button
        type="button"
        onClick={toggleTheme}
        aria-label={label}
        title={label}
        className="rounded-lg p-2 text-slate-700 transition-colors hover:bg-slate-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
      >
        <Icon className="w-5 h-5" aria-hidden="true" />
      </button>
    )
  }

  // Zwei Zustände, also ein Schalter: role="switch" sagt einem Screenreader
  // "Dunkles Design, an" oder "aus", und die Beschriftung bleibt dieselbe.
  return (
    <button
      type="button"
      role="switch"
      aria-checked={dark}
      onClick={toggleTheme}
      className="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-200 dark:text-neutral-400 dark:hover:bg-neutral-800 dark:hover:text-neutral-200"
    >
      <Moon className="w-4 h-4" aria-hidden="true" />
      <span className="flex-1 text-left">Dunkles Design</span>
      {/* Aus ist nur im hellen Modus zu sehen und an nur im dunklen. Die
          Spur hält deshalb gegen ihren jeweiligen Grund die 3:1, die ein
          Bedienelement braucht: slate-500 auf slate-50 (4,6:1), indigo-500
          auf neutral-950 (4,3:1). indigo-600 läge dort mit 3,1:1 gerade
          noch drüber; -500 ist derselbe Ton wie der aktive Listeneintrag. */}
      <span
        aria-hidden="true"
        className={`relative h-5 w-9 shrink-0 rounded-full transition-colors ${
          dark ? 'bg-indigo-600 dark:bg-indigo-500' : 'bg-slate-500 dark:bg-neutral-600'
        }`}
      >
        <span
          className={`absolute left-0.5 top-0.5 h-4 w-4 rounded-full bg-white shadow-sm transition-transform dark:bg-neutral-100 ${
            dark ? 'translate-x-4' : ''
          }`}
        />
      </span>
    </button>
  )
}
