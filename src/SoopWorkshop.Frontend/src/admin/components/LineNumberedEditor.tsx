import { useId, useMemo, useRef, useState } from 'react'

type LineNumberedEditorProps = {
  label: string
  hint?: string
  value: string
  onChange: (value: string) => void
  rows?: number
}

const EINRUECKUNG = '  '

// Monospace-Feld mit Zeilennummern. Bewusst kein Monaco und kein CodeMirror:
// beides bräuchte ein weiteres Paket mit eigener Meinung, und was hier gebraucht
// wird - Zeilennummern und eine brauchbare Tab-Taste - sind zwanzig Zeilen.
// Syntaxhervorhebung gibt es dafür nicht.
export function LineNumberedEditor({
  label,
  hint,
  value,
  onChange,
  rows = 18,
}: LineNumberedEditorProps) {
  const id = useId()
  const nummernRef = useRef<HTMLDivElement>(null)
  const textRef = useRef<HTMLTextAreaElement>(null)

  // Solange true, fügt Tab ein. Escape schaltet es für den nächsten
  // Tastendruck ab - siehe unten.
  const [tabRueckt, setTabRueckt] = useState(true)

  const zeilen = useMemo(() => value.split('\n').length, [value])

  const onKeyDown = (event: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Escape') {
      // Ohne diesen Ausweg ist das Feld eine Tastaturfalle: Tab rückt ein,
      // also kommt man mit der Tastatur nie wieder heraus. Escape gibt den
      // nächsten Tab frei, danach fängt das Feld ihn wieder.
      setTabRueckt(false)
      return
    }

    if (event.key !== 'Tab') {
      if (!tabRueckt) setTabRueckt(true)
      return
    }

    if (!tabRueckt) {
      // Durchlassen: der Fokus wandert weiter. Für das nächste Mal wieder
      // scharf schalten.
      setTabRueckt(true)
      return
    }

    event.preventDefault()

    const feld = event.currentTarget
    const { selectionStart, selectionEnd } = feld
    const neu = value.slice(0, selectionStart) + EINRUECKUNG + value.slice(selectionEnd)

    onChange(neu)

    // Den Cursor hinter die Einrückung setzen. Erst nach dem Neuzeichnen,
    // sonst überschreibt React die Position wieder.
    requestAnimationFrame(() => {
      const ziel = selectionStart + EINRUECKUNG.length
      feld.setSelectionRange(ziel, ziel)
    })
  }

  return (
    <div>
      <label htmlFor={id} className="block text-sm font-semibold text-slate-700">
        {label}
      </label>
      {hint && <p className="mt-0.5 text-xs text-slate-500">{hint}</p>}

      <div className="mt-1.5 flex overflow-hidden rounded-xl border border-slate-300 bg-slate-900 font-mono text-sm">
        {/* Die Nummernspalte scrollt nicht selbst, sie wird vom Textfeld
            mitgezogen — deshalb overflow-hidden und der onScroll unten. */}
        <div
          ref={nummernRef}
          aria-hidden="true"
          className="max-h-[32rem] shrink-0 select-none overflow-hidden bg-slate-800 px-3 py-3 text-right leading-6 text-slate-500 tabular-nums"
        >
          {Array.from({ length: zeilen }, (_, i) => (
            <div key={i}>{i + 1}</div>
          ))}
        </div>

        <textarea
          id={id}
          ref={textRef}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={onKeyDown}
          onScroll={(event) => {
            if (nummernRef.current) nummernRef.current.scrollTop = event.currentTarget.scrollTop
          }}
          rows={rows}
          spellCheck={false}
          // Java-Quelltext wird nicht umbrochen: eine automatisch umgebrochene
          // Zeile stimmt nicht mehr mit ihrer Nummer daneben überein.
          wrap="off"
          className="max-h-[32rem] flex-1 resize-y bg-slate-900 px-3 py-3 leading-6 text-slate-100 outline-none"
        />
      </div>

      <p className="mt-1 text-xs text-slate-500">
        Tab rückt ein. Mit Escape und dann Tab springst du aus dem Feld heraus.
      </p>
    </div>
  )
}
