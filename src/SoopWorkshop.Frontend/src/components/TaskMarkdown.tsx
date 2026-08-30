import ReactMarkdown from 'react-markdown'

// Die Aufgabenbeschreibung als Markdown - an genau einer Stelle gesetzt.
//
// Teilnehmersicht (TaskView) und Vorschau im Aufgaben-Editor haben die
// prose-Klassen früher jede für sich getragen und liefen damit auseinander:
// beim Teilnehmer verschwanden die Backticks um Inline-Code, in der Vorschau
// standen sie sichtbar im Text. Eine Vorschau, die etwas anderes zeigt als der
// Teilnehmer sieht, ist wertlos - deshalb dieselbe Komponente für beide.
//
// Zu den Klassen:
//
//   [&_:not(pre)>code] statt prose-code
//     "prose-code:" hängt an JEDEM <code>, auch dem in einem <pre>. Die
//     Pille für Inline-Code landete damit auf jedem Codeblock: eine Inline-Box
//     mit white-space: pre bricht in ein Fragment je Zeile, und jedes davon
//     zeichnet den Hintergrund selbst. Das ergab helle Kästchen Zeile für
//     Zeile, darauf den hellen Text des dunklen Blocks. Dazu gilt das
//     padding-left nur beim ersten Fragment - eine ASCII-Pyramide stand
//     dadurch mit der Spitze 6px zu weit rechts.
//
//   prose-pre:leading-snug
//     Typography setzt Codeblöcke auf 1,714. Ein Zeichen ist 0,6em breit,
//     eine Zeile war damit 2,86-mal so hoch wie breit - ASCII-Kunst wird so
//     um rund 40% überdehnt. 1,375 liegt zwischen Terminal (~1,2) und
//     Editor (~1,5): die Pyramide behält ihre Form, Java-Code bleibt luftig.
const KLASSEN = [
  'prose prose-slate max-w-none',
  'prose-p:text-slate-700 prose-p:leading-relaxed',
  'prose-pre:leading-snug',
  '[&_:not(pre)>code]:before:content-none [&_:not(pre)>code]:after:content-none',
  '[&_:not(pre)>code]:rounded [&_:not(pre)>code]:bg-slate-100',
  '[&_:not(pre)>code]:px-1.5 [&_:not(pre)>code]:py-0.5 [&_:not(pre)>code]:font-normal',
].join(' ')

export function TaskMarkdown({ children }: { children: string }) {
  return (
    <div className={KLASSEN}>
      <ReactMarkdown>{children}</ReactMarkdown>
    </div>
  )
}
