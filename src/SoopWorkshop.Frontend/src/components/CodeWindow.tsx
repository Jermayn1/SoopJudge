import { useEffect, useId, useMemo, useState } from 'react'
import { Check, ChevronDown, Copy } from 'lucide-react'
import hljs from 'highlight.js/lib/core'
import java from 'highlight.js/lib/languages/java'

// Nur der Kern und die Java-Grammatik, kein Sammelpaket: das Werkzeug zeigt
// ausschließlich Java. Registriert wird einmal beim Laden des Moduls.
hljs.registerLanguage('java', java)

// Legt den Text in die Zwischenablage und sagt, ob es geklappt hat.
//
// navigator.clipboard gibt es nur in einem "secure context". Der Aufbau im
// Ausbildungsnetz läuft bewusst über reines http auf einer IP — dort ist die
// API schlicht nicht vorhanden, während sie auf localhost einwandfrei
// funktioniert. Ein Knopf, der nur beim Entwickeln geht, ist schlimmer als
// keiner, deshalb der Rückfall über ein kurzlebiges Textfeld.
async function copyToClipboard(text: string): Promise<boolean> {
  if (navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text)
      return true
    } catch {
      // Weiter zum Rückfall. Eine abgelehnte Berechtigung ist kein Grund,
      // hier aufzugeben — aber auch keiner, still nichts zu tun.
    }
  }

  const feld = document.createElement('textarea')
  feld.value = text
  feld.setAttribute('readonly', '')
  feld.style.position = 'fixed'
  feld.style.top = '-1000px'
  document.body.appendChild(feld)
  feld.select()

  try {
    return document.execCommand('copy')
  } catch {
    return false
  } finally {
    document.body.removeChild(feld)
  }
}

type CodeWindowProps = {
  fileName: string
  code: string
}

type CopyState = 'idle' | 'ok' | 'failed'

// Eine Quelltextdatei in einem Fenster: Ampelpunkte, Dateiname, Zeilennummern,
// Syntaxfarben.
//
// Rein darstellend und ohne Datenzugriff, damit die Verwaltung sie für eine
// abgegebene Datei benutzen kann und später etwas anderes für eine andere.
export function CodeWindow({ fileName, code }: CodeWindowProps) {
  const [open, setOpen] = useState(true)
  const [copyState, setCopyState] = useState<CopyState>('idle')
  const regionId = useId()

  // Abschließende Leerzeilen weg, bevor gezählt und gefärbt wird. Fast jede
  // Datei endet mit einem Zeilenumbruch, und ohne diesen Schnitt zeigte das
  // Fenster eine Zeile mehr, als die Datei hat.
  const text = useMemo(() => code.replace(/\n+$/, ''), [code])

  const lineCount = useMemo(() => text.split('\n').length, [text])

  // hljs maskiert <, > und & in seiner Ausgabe — deshalb ist das hier
  // vertretbar, obwohl der Inhalt von Teilnehmern stammt. Ein Test in
  // CodeWindow.test.tsx hält genau das fest, statt es zu glauben.
  const html = useMemo(() => hljs.highlight(text, { language: 'java' }).value, [text])

  // Die Rückmeldung am Kopierknopf verschwindet wieder. Ohne das bliebe
  // "Kopiert" stehen, bis die Seite neu lädt.
  useEffect(() => {
    if (copyState === 'idle') return
    const timer = window.setTimeout(() => setCopyState('idle'), 2500)
    return () => window.clearTimeout(timer)
  }, [copyState])

  const onCopy = async () => {
    setCopyState((await copyToClipboard(text)) ? 'ok' : 'failed')
  }

  // Das „relative" an der Karte ist kein Beiwerk. Die Meldezone weiter unten
  // trägt „sr-only", und diese Klasse arbeitet mit position: absolute — der
  // Name verrät das nicht. Ohne positionierten Vorfahren ist ihr
  // Enthaltungsblock der Ursprungsblock des Dokuments, und weil
  // overflow-hidden nur positionierte Nachfahren beschneidet, entkam sie jedem
  // Scroll-Container darüber: das Dokument wuchs bis auf ihre Position mit und
  // ließ sich als Ganzes scrollen, wodurch beim Blättern die Kopfleiste der
  // Verwaltung wegrutschte.
  //
  // Der Dateiname darüber hängt dagegen an der Titelleiste, die selbst
  // „relative" trägt — er war nie betroffen.
  return (
    <div className="relative overflow-hidden rounded-xl border border-slate-700 bg-slate-900 shadow-lg dark:border-white/10 dark:bg-neutral-900">
      <div className="relative flex items-center border-b border-slate-700 bg-slate-800 px-4 py-2.5 dark:border-white/10 dark:bg-neutral-800">
        {/* Reine Zier — deshalb aus der Vorlesereihenfolge heraus. */}
        <div className="flex shrink-0 gap-2" aria-hidden="true">
          <span className="h-3 w-3 rounded-full bg-rose-500" />
          <span className="h-3 w-3 rounded-full bg-amber-400" />
          <span className="h-3 w-3 rounded-full bg-emerald-500" />
        </div>

        {/* Mittig über die ganze Leiste, mit Freiraum links und rechts, damit
            ein langer Dateiname nicht unter den Knöpfen verschwindet. */}
        <span className="pointer-events-none absolute inset-x-0 truncate px-28 text-center font-mono text-xs text-slate-300 dark:text-neutral-300">
          {fileName}
        </span>

        <div className="ml-auto flex shrink-0 items-center gap-1">
          <button
            type="button"
            onClick={() => void onCopy()}
            className="flex items-center gap-1.5 rounded-md px-2 py-1 text-xs font-medium text-slate-300 transition-colors hover:bg-slate-700 hover:text-slate-100 dark:text-neutral-300 dark:hover:bg-neutral-700 dark:hover:text-neutral-100"
          >
            {copyState === 'ok' ? (
              <Check className="h-3.5 w-3.5" aria-hidden="true" />
            ) : (
              <Copy className="h-3.5 w-3.5" aria-hidden="true" />
            )}
            {/* Auch nach einem Fehlschlag bleibt „Kopieren" stehen: die
                Beschriftung eines Knopfes sagt, was er tut. Was schiefging,
                steht darunter. */}
            {copyState === 'ok' ? 'Kopiert' : 'Kopieren'}
          </button>

          <button
            type="button"
            onClick={() => setOpen((o) => !o)}
            aria-expanded={open}
            aria-controls={regionId}
            aria-label={open ? `${fileName} einklappen` : `${fileName} ausklappen`}
            className="rounded-md p-1 text-slate-300 transition-colors hover:bg-slate-700 hover:text-slate-100 dark:text-neutral-300 dark:hover:bg-neutral-700 dark:hover:text-neutral-100"
          >
            <ChevronDown
              className={`h-4 w-4 transition-transform duration-200 ${open ? '' : '-rotate-90'}`}
              aria-hidden="true"
            />
          </button>
        </div>
      </div>

      {/* Der Erfolg steht sichtbar am Knopf; für Bildschirmleser braucht er
          eine eigene Ansage. Der Fehlschlag steht darunter im Klartext und ist
          selbst die Meldezone — zwei Sätze mit derselben Aussage wären eine
          Dopplung, die beim Vorlesen doppelt ankommt. */}
      <p className="sr-only" role="status" aria-live="polite">
        {copyState === 'ok' ? `${fileName} liegt in der Zwischenablage.` : ''}
      </p>

      {copyState === 'failed' && (
        <p
          role="status"
          aria-live="polite"
          className="border-b border-slate-700 bg-slate-800 px-4 py-2 text-xs text-amber-200 dark:border-white/10 dark:bg-neutral-800 dark:text-amber-200"
        >
          Das Kopieren hat nicht geklappt — markiere den Text und nimm Strg+C.
        </p>
      )}

      {/* Ein- und Ausklappen über das Raster, wie überall sonst. „inert" nimmt
          den eingeklappten Bereich aus der Tab-Reihenfolge; ohne das bliebe
          der Codeblock antabbar, obwohl niemand ihn sieht. */}
      <div id={regionId} className={`klapp ${open ? '' : 'klapp-zu'}`} inert={!open}>
        <div className="klapp-inhalt">
          <div className="flex">
            <div
              aria-hidden="true"
              className="shrink-0 select-none border-r border-slate-800 px-3 py-4 text-right font-mono text-xs leading-6 text-slate-400 tabular-nums dark:border-white/10 dark:text-neutral-400"
            >
              {Array.from({ length: lineCount }, (_, i) => (
                <div key={i}>{i + 1}</div>
              ))}
            </div>

            {/* Kein Zeilenumbruch: eine umbrochene Zeile stimmt nicht mehr mit
                ihrer Nummer daneben überein. Stattdessen scrollt der Block
                waagerecht — und ein scrollbarer Bereich braucht tabIndex,
                sonst ist er mit der Tastatur nicht erreichbar. */}
            <pre
              tabIndex={0}
              aria-label={`Quelltext von ${fileName}`}
              className="flex-1 overflow-x-auto px-4 py-4 font-mono text-xs leading-6 text-slate-200 dark:text-neutral-200"
            >
              <code
                className="language-java"
                dangerouslySetInnerHTML={{ __html: html }}
              />
            </pre>
          </div>
        </div>
      </div>
    </div>
  )
}
