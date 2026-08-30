import { ChevronDown, ChevronUp } from 'lucide-react'

type OrderButtonsProps = {
  /** Beschreibt, was verschoben wird — landet in der Vorlesehilfe. */
  label: string
  onUp: () => void
  onDown: () => void
  canMoveUp: boolean
  canMoveDown: boolean
}

// Hoch und runter statt Ziehen und Ablegen.
//
// Drag & Drop löst dasselbe Problem schlechter: es braucht entweder ein Paket
// oder eigene Pointer-Logik, und es ist mit der Tastatur nicht bedienbar. Zwei
// Knöpfe sind es dagegen von selbst.
export function OrderButtons({ label, onUp, onDown, canMoveUp, canMoveDown }: OrderButtonsProps) {
  const buttonClass =
    'rounded-md p-1 text-slate-500 transition-colors hover:bg-slate-200 hover:text-slate-800 disabled:cursor-not-allowed disabled:text-slate-300 disabled:hover:bg-transparent'

  return (
    <div className="flex shrink-0 flex-col">
      <button
        type="button"
        onClick={onUp}
        disabled={!canMoveUp}
        aria-label={`${label} nach oben`}
        className={buttonClass}
      >
        <ChevronUp className="w-4 h-4" aria-hidden="true" />
      </button>
      <button
        type="button"
        onClick={onDown}
        disabled={!canMoveDown}
        aria-label={`${label} nach unten`}
        className={buttonClass}
      >
        <ChevronDown className="w-4 h-4" aria-hidden="true" />
      </button>
    </div>
  )
}
