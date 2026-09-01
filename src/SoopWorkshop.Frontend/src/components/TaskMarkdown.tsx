import type { ComponentProps } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'

// Die Aufgabenbeschreibung als Markdown - an genau einer Stelle gesetzt.
//
// Teilnehmersicht (TaskView) und Vorschau im Aufgaben-Editor haben die
// prose-Klassen früher jede für sich getragen und liefen damit auseinander:
// beim Teilnehmer verschwanden die Backticks um Inline-Code, in der Vorschau
// standen sie sichtbar im Text. Eine Vorschau, die etwas anderes zeigt als der
// Teilnehmer sieht, ist wertlos - deshalb dieselbe Komponente für beide.
//
// Zu remarkGfm:
//
//   react-markdown kann von sich aus nur CommonMark, und darin gibt es KEINE
//   Tabellen - die sind eine Erweiterung von GitHub Flavored Markdown. Ohne
//   das Plugin fällt eine Tabelle als gewöhnlicher Absatz durch: alle Zeilen
//   stehen mitsamt ihren Pipe-Zeichen aneinandergereiht im Fließtext. Kein
//   Fehler, keine Warnung - dieselbe Falle wie bei prose und ::deep. Betrifft
//   ebenso Durchstreichungen, Aufgabenlisten und nackte Links.
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

// Eine breite Tabelle darf die Karte nicht sprengen: sie scrollt in ihrem
// eigenen Rahmen, statt die ganze Seite quer zu schieben. Deshalb steht die
// Tabelle außerhalb von prose (not-prose) und bringt ihre Klassen selbst mit.
const KOMPONENTEN = {
  table: ({ children, ...rest }: ComponentProps<'table'>) => (
    <div className="not-prose my-6 overflow-x-auto rounded-lg border border-slate-200">
      <table {...rest} className="w-full border-collapse text-left text-sm">
        {children}
      </table>
    </div>
  ),
  thead: ({ children, ...rest }: ComponentProps<'thead'>) => (
    <thead {...rest} className="bg-slate-50 text-slate-900">
      {children}
    </thead>
  ),
  th: ({ children, ...rest }: ComponentProps<'th'>) => (
    <th {...rest} className="border-b border-slate-200 px-3 py-2 font-semibold">
      {children}
    </th>
  ),
  td: ({ children, ...rest }: ComponentProps<'td'>) => (
    <td
      {...rest}
      className="border-b border-slate-100 px-3 py-2 align-top text-slate-700"
    >
      {children}
    </td>
  ),
}

export function TaskMarkdown({ children }: { children: string }) {
  return (
    <div className={KLASSEN}>
      <ReactMarkdown remarkPlugins={[remarkGfm]} components={KOMPONENTEN}>
        {children}
      </ReactMarkdown>
    </div>
  )
}
