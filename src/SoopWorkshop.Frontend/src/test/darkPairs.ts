import ts from 'typescript'

// Findet Farbklassen ohne dunkles Gegenstück.
//
// Die dunklen Farben stehen als dark:-Klassen direkt neben den hellen, ohne
// Zwischenschicht. Das hält jede Stelle von Hand einstellbar,
// hat aber einen Preis: vergisst man eine, bleibt im Dunkeln eine weiße
// Fläche oder dunkle Schrift auf dunklem Grund stehen. Kein Fehler, keine
// Warnung. Dieser Wächter macht daraus einen roten Test.
//
// Die Regel: jede Farbklasse, die im Dunkeln nicht mehr passt, braucht in
// DERSELBEN Zeichenkette eine dark:-Klasse für dieselbe Eigenschaft mit
// denselben Varianten. Bei hover:bg-slate-200 also dark:hover:bg-…
//
// Geprüft wird jedes Zeichenketten-Literal einzeln, auch jeder Teil eines
// Template-Strings. Im Bestand steht jede Farbe in ihrem eigenen Literal,
// auch in Ternärzweigen, deshalb trägt die Regel.

export type Finding = {
  line: number
  token: string
  reason: string
}

const GRAYS = new Set(['slate', 'gray', 'zinc', 'neutral', 'stone'])

const COLOR =
  '(white|black|(slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose)-(\\d{2,3}))'

const PROPERTY =
  '(bg|text|border(?:-[trblxyse])?|divide|ring-offset|ring|outline|shadow|from|via|to|fill|stroke|accent|caret|decoration)'

const UTILITY = new RegExp(`^${PROPERTY}-${COLOR}(?:/\\d+)?$`)

// Flächen, Kanten und Verläufe: kräftige Akzente (400 bis 900) stehen in
// beiden Modi, etwa bg-indigo-600 unter weißer Schrift oder ein grüner Punkt.
// Schrift dagegen muss sich immer umstellen: text-indigo-600 ist auf Weiß gut
// lesbar und auf Dunkelgrau nicht.
const SURFACE_PROPERTIES = new Set(['bg', 'border', 'divide', 'ring', 'ring-offset', 'outline', 'from', 'via', 'to'])

// Eine Klasse zerlegen in Varianten und Utility. Doppelpunkte in eckigen
// Klammern gehören zur Variante: [&_:not(pre)>code]:bg-slate-100.
function splitVariants(token: string): string[] {
  const parts: string[] = []
  let depth = 0
  let current = ''
  for (const char of token) {
    if (char === '[') depth++
    if (char === ']') depth--
    if (char === ':' && depth === 0) {
      parts.push(current)
      current = ''
    } else {
      current += char
    }
  }
  parts.push(current)
  return parts
}

type ColorClass = {
  token: string
  variants: string[]
  property: string
  needsPair: boolean
}

function parse(token: string): ColorClass | null {
  const parts = splitVariants(token)
  const utility = parts[parts.length - 1].replace(/^!/, '')
  const match = UTILITY.exec(utility)
  if (!match) return null

  const property = match[1]
  const family = match[3]
  const shade = match[4] ? Number(match[4]) : null
  const variants = parts.slice(0, -1)

  let needsPair: boolean
  if (property === 'text' && match[2] === 'white') needsPair = false
  else if (property === 'shadow') needsPair = true
  else if (!SURFACE_PROPERTIES.has(property.replace(/^border-.*/, 'border'))) needsPair = true
  else if (family === undefined || GRAYS.has(family)) needsPair = true
  else needsPair = shade !== null && (shade <= 300 || shade >= 950)

  return { token, variants, property, needsPair }
}

function sameVariants(a: string[], b: string[]): boolean {
  if (a.length !== b.length) return false
  const set = new Set(a)
  return b.every((variant) => set.has(variant))
}

export function checkClassString(text: string): Omit<Finding, 'line'>[] {
  const findings: Omit<Finding, 'line'>[] = []
  const classes = text
    .split(/\s+/)
    .filter(Boolean)
    .map(parse)
    .filter((c): c is ColorClass => c !== null)

  const dark = classes.filter((c) => c.variants.includes('dark'))

  for (const c of dark) {
    // backdrop:dark:bg-… wird zu ::backdrop:where(.dark …), und das ist
    // ungültiges CSS. Der Browser verwirft die Regel still.
    if (c.variants[0] !== 'dark') {
      findings.push({ token: c.token, reason: 'dark: muss die erste Variante sein' })
    }
  }

  for (const c of classes) {
    if (c.variants.includes('dark') || !c.needsPair) continue
    const hasPair = dark.some(
      (d) => d.variants[0] === 'dark' && d.property === c.property && sameVariants(d.variants.slice(1), c.variants),
    )
    if (!hasPair) findings.push({ token: c.token, reason: 'kein dunkles Gegenstück' })
  }

  return findings
}

// Ein Klassenname, der erst zur Laufzeit zusammengesetzt wird (bg-${farbe}),
// entzieht sich jeder Prüfung. Das soll gar nicht erst entstehen.
const DYNAMIC_FRAGMENT = new RegExp(`(^|\\s)(dark:)?([a-z-]+:)*${PROPERTY}(-[a-z]+)*-$`)

export function checkSource(fileName: string, source: string): Finding[] {
  const file = ts.createSourceFile(fileName, source, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX)
  const findings: Finding[] = []

  const visit = (node: ts.Node) => {
    let text: string | null = null
    if (ts.isStringLiteral(node) || ts.isNoSubstitutionTemplateLiteral(node)) text = node.text
    else if (ts.isTemplateHead(node) || ts.isTemplateMiddle(node) || ts.isTemplateTail(node)) text = node.text

    if (text !== null) {
      const line = file.getLineAndCharacterOfPosition(node.getStart()).line + 1

      if ((ts.isTemplateHead(node) || ts.isTemplateMiddle(node)) && DYNAMIC_FRAGMENT.test(text)) {
        const token = text.trim().split(/\s+/).pop() ?? text
        findings.push({ line, token, reason: 'Farbklasse wird zur Laufzeit zusammengesetzt' })
      }

      for (const finding of checkClassString(text)) findings.push({ line, ...finding })
    }

    ts.forEachChild(node, visit)
  }

  visit(file)
  return findings
}
