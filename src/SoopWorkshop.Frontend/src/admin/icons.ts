import {
  Activity, AlarmClock, Archive, ArrowDownUp, ArrowLeftRight, AtSign, Award,
  Binary, Blocks, Bolt, Bookmark, BookOpen, Boxes, Brackets, Brain, Bug,
  Calculator, Calendar, ChartBar, ChartLine, Check, CircleDot, Clipboard, Cloud,
  Code, Cog, Compass, Cpu, CreditCard, Crown,
  Database, Dice5, Download, Equal, Eye, Feather, FileCode2, FileText, Filter,
  Flag, FlaskConical, Folder, FolderTree, Gamepad2, Gauge, Gift, GitBranch,
  GitCommitHorizontal, GitMerge, Globe, GraduationCap, Grid3x3,
  Hammer, Hash, Heart, Hourglass, Image,
  // Umbenannt, weil der Name sonst das globale Infinity verdeckt.
  Infinity as InfinityIcon,
  Key, Keyboard, Landmark,
  Layers, LayoutGrid, Library, Lightbulb, ListOrdered, ListTree, Lock,
  Magnet, MapPin, Medal, MessageSquare, Microscope, Milestone, Monitor, Moon,
  MousePointer, Move, Network, Notebook, Package, Palette, PenTool, Percent,
  PiggyBank, Play, Plug, Printer, Puzzle, Radio, Recycle, Regex, Repeat,
  Rocket, Route, Ruler, Save, Scale, Scissors, Search, Server, Settings, Shapes,
  Shield, ShoppingCart, Shuffle, Sigma, Signpost, Smartphone, Sparkles, Split,
  Star, Sun, Table, Tag, Target, Terminal, TestTube, Timer, ToggleLeft,
  Trash2, TrendingUp, Trophy, Truck, Type, Undo2, Upload, Users, Variable,
  Wallet, Wand2, Waypoints, Workflow, Wrench, Zap,
  type LucideIcon,
} from 'lucide-react'

// Auswahl der Symbole, die eine Kategorie tragen kann.
//
// Bewusst kuratiert und nicht "alles was lucide hat": die Bibliothek bringt
// rund 1500 Symbole mit, und ein Sammelimport zöge sie alle ins Bundle. Was
// hier steht, ist nach Themen des Workshops ausgesucht.
//
// Die "suche"-Wörter sind deutsch, weil danach gesucht wird — der englische
// Name ist ohnehin immer mit durchsuchbar.
export type IconEntry = {
  name: string
  icon: LucideIcon
  suche: string
}

// Faltet Umlaute auf ihre Ersatzschreibung und macht klein. Damit findet
// sowohl "prüfung" als auch "pruefung" dasselbe Symbol.
//
// Nötig, seit der Index oben mit echten Umlauten geschrieben ist: ein reiner
// Austausch der Schreibweise hätte "pruefung" ins Leere laufen lassen, ohne
// dass irgendwo ein Fehler entsteht - die Suche hätte einfach nichts mehr
// gefunden. Gehört hierher und nicht in den Dialog, weil sie zum Index
// gehört, nicht zur Darstellung.
export function falte(text: string): string {
  return text
    .toLowerCase()
    .replace(/ä/g, 'ae')
    .replace(/ö/g, 'oe')
    .replace(/ü/g, 'ue')
    .replace(/ß/g, 'ss')
}

// Gesucht wird über den englischen Namen UND die deutschen Stichwörter -
// wer "schleife" tippt, soll Repeat finden, ohne das Wort zu kennen.
// Ein leerer Begriff liefert den vollen Bestand.
//
// Steht hier statt im Dialog, damit die Suche ohne Anmeldung und ohne
// gerendertes <dialog> prüfbar ist - ein Test, der den Filter nachbaut,
// würde nur sich selbst bestätigen.
export function filterIcons(begriff: string): IconGroup[] {
  const gesucht = falte(begriff.trim())
  if (gesucht.length === 0) return ICON_GROUPS

  return ICON_GROUPS
    .map((group) => ({
      titel: group.titel,
      eintraege: group.eintraege.filter(
        (entry) => falte(entry.name).includes(gesucht) || falte(entry.suche).includes(gesucht),
      ),
    }))
    .filter((group) => group.eintraege.length > 0)
}

export type IconGroup = {
  titel: string
  eintraege: IconEntry[]
}

export const ICON_GROUPS: IconGroup[] = [
  {
    titel: 'Programmieren',
    eintraege: [
      { name: 'Terminal', icon: Terminal, suche: 'konsole kommandozeile eingabe' },
      { name: 'Code', icon: Code, suche: 'quelltext programm' },
      { name: 'FileCode2', icon: FileCode2, suche: 'datei quelltext java' },
      { name: 'Brackets', icon: Brackets, suche: 'klammern array' },
      { name: 'Binary', icon: Binary, suche: 'binär bits zahlen' },
      { name: 'Variable', icon: Variable, suche: 'variable wert' },
      { name: 'Regex', icon: Regex, suche: 'regulärer ausdruck muster' },
      { name: 'Bug', icon: Bug, suche: 'fehler debugging käfer' },
      { name: 'TestTube', icon: TestTube, suche: 'test prüfung labor' },
      { name: 'FlaskConical', icon: FlaskConical, suche: 'test experiment labor' },
      { name: 'Keyboard', icon: Keyboard, suche: 'tastatur eingabe' },
      { name: 'Cpu', icon: Cpu, suche: 'prozessor rechner' },
      { name: 'Monitor', icon: Monitor, suche: 'bildschirm ausgabe' },
      { name: 'Smartphone', icon: Smartphone, suche: 'handy mobil' },
    ],
  },
  {
    titel: 'Objektorientierung',
    eintraege: [
      { name: 'Layers', icon: Layers, suche: 'schichten vererbung oop klassen' },
      { name: 'Boxes', icon: Boxes, suche: 'objekte klassen kisten' },
      { name: 'Blocks', icon: Blocks, suche: 'bausteine komposition' },
      { name: 'Package', icon: Package, suche: 'paket modul' },
      { name: 'Puzzle', icon: Puzzle, suche: 'schnittstelle interface teile' },
      { name: 'Shapes', icon: Shapes, suche: 'formen polymorphie' },
      { name: 'Network', icon: Network, suche: 'beziehungen graph' },
      { name: 'Waypoints', icon: Waypoints, suche: 'verbindungen knoten' },
      { name: 'Plug', icon: Plug, suche: 'schnittstelle interface stecker' },
    ],
  },
  {
    titel: 'Ablauf und Logik',
    eintraege: [
      { name: 'Repeat', icon: Repeat, suche: 'schleife wiederholung loop' },
      { name: 'Infinity', icon: InfinityIcon, suche: 'endlos unendlich schleife' },
      { name: 'Split', icon: Split, suche: 'verzweigung bedingung if' },
      { name: 'GitBranch', icon: GitBranch, suche: 'verzweigung zweig branch' },
      { name: 'GitMerge', icon: GitMerge, suche: 'zusammenführen merge' },
      { name: 'GitCommitHorizontal', icon: GitCommitHorizontal, suche: 'schritt commit' },
      { name: 'Workflow', icon: Workflow, suche: 'ablauf prozess' },
      { name: 'Route', icon: Route, suche: 'weg pfad ablauf' },
      { name: 'Shuffle', icon: Shuffle, suche: 'mischen zufall tauschen' },
      { name: 'ArrowLeftRight', icon: ArrowLeftRight, suche: 'tauschen swap' },
      { name: 'ArrowDownUp', icon: ArrowDownUp, suche: 'sortieren tauschen' },
      { name: 'Undo2', icon: Undo2, suche: 'rekursion zurück' },
      { name: 'ToggleLeft', icon: ToggleLeft, suche: 'schalter boolean wahr falsch' },
      { name: 'CircleDot', icon: CircleDot, suche: 'zustand punkt' },
      { name: 'Signpost', icon: Signpost, suche: 'entscheidung wegweiser' },
      { name: 'Milestone', icon: Milestone, suche: 'meilenstein schritt' },
    ],
  },
  {
    titel: 'Daten und Struktur',
    eintraege: [
      { name: 'Database', icon: Database, suche: 'datenbank speicher' },
      { name: 'Table', icon: Table, suche: 'tabelle array zweidimensional' },
      { name: 'Grid3x3', icon: Grid3x3, suche: 'raster matrix array' },
      { name: 'LayoutGrid', icon: LayoutGrid, suche: 'raster felder' },
      { name: 'ListOrdered', icon: ListOrdered, suche: 'liste nummeriert reihenfolge' },
      { name: 'ListTree', icon: ListTree, suche: 'baum struktur verschachtelt' },
      { name: 'FolderTree', icon: FolderTree, suche: 'baum ordner struktur' },
      { name: 'Folder', icon: Folder, suche: 'ordner ablage' },
      { name: 'Archive', icon: Archive, suche: 'archiv sammlung' },
      { name: 'Filter', icon: Filter, suche: 'filtern auswählen' },
      { name: 'Search', icon: Search, suche: 'suchen finden' },
      { name: 'Hash', icon: Hash, suche: 'hash schlüssel map' },
      { name: 'Key', icon: Key, suche: 'schlüssel map' },
      { name: 'Server', icon: Server, suche: 'server speicher' },
      { name: 'Cloud', icon: Cloud, suche: 'wolke netz' },
      { name: 'Save', icon: Save, suche: 'speichern datei' },
      { name: 'Download', icon: Download, suche: 'laden herunter' },
      { name: 'Upload', icon: Upload, suche: 'hochladen abgeben' },
    ],
  },
  {
    titel: 'Rechnen und Messen',
    eintraege: [
      { name: 'Calculator', icon: Calculator, suche: 'rechnen taschenrechner' },
      { name: 'Sigma', icon: Sigma, suche: 'summe rechnen' },
      { name: 'Percent', icon: Percent, suche: 'prozent anteil' },
      { name: 'Equal', icon: Equal, suche: 'gleich vergleich' },
      { name: 'Scale', icon: Scale, suche: 'waage vergleich gewicht' },
      { name: 'Ruler', icon: Ruler, suche: 'messen länge' },
      { name: 'Gauge', icon: Gauge, suche: 'anzeige messen tempo' },
      { name: 'ChartBar', icon: ChartBar, suche: 'diagramm balken statistik' },
      { name: 'ChartLine', icon: ChartLine, suche: 'diagramm linie verlauf' },
      { name: 'TrendingUp', icon: TrendingUp, suche: 'wachstum steigend' },
      { name: 'Activity', icon: Activity, suche: 'verlauf puls' },
      { name: 'Dice5', icon: Dice5, suche: 'würfel zufall random' },
      { name: 'Timer', icon: Timer, suche: 'zeit stoppuhr' },
      { name: 'Hourglass', icon: Hourglass, suche: 'sanduhr warten zeit' },
      { name: 'AlarmClock', icon: AlarmClock, suche: 'wecker zeit' },
      { name: 'Calendar', icon: Calendar, suche: 'kalender datum' },
    ],
  },
  {
    titel: 'Werkzeug und Einstellung',
    eintraege: [
      { name: 'Wrench', icon: Wrench, suche: 'werkzeug schlüssel' },
      { name: 'Hammer', icon: Hammer, suche: 'werkzeug hammer bauen' },
      { name: 'Settings', icon: Settings, suche: 'einstellungen zahnrad' },
      { name: 'Cog', icon: Cog, suche: 'zahnrad technik' },
      { name: 'Bolt', icon: Bolt, suche: 'schraube technik' },
      { name: 'PenTool', icon: PenTool, suche: 'zeichnen stift' },
      { name: 'Palette', icon: Palette, suche: 'farben gestaltung' },
      { name: 'Scissors', icon: Scissors, suche: 'schneiden teilen' },
      { name: 'Magnet', icon: Magnet, suche: 'anziehen magnet' },
      { name: 'Recycle', icon: Recycle, suche: 'wiederverwenden kreislauf' },
      { name: 'Move', icon: Move, suche: 'verschieben bewegen' },
      { name: 'MousePointer', icon: MousePointer, suche: 'maus zeiger klicken' },
      { name: 'Printer', icon: Printer, suche: 'drucken ausgabe' },
      { name: 'Trash2', icon: Trash2, suche: 'löschen müll' },
    ],
  },
  {
    titel: 'Lernen und Fortschritt',
    eintraege: [
      { name: 'BookOpen', icon: BookOpen, suche: 'buch lernen lesen' },
      { name: 'Library', icon: Library, suche: 'bibliothek bücher' },
      { name: 'Notebook', icon: Notebook, suche: 'heft notizen' },
      { name: 'GraduationCap', icon: GraduationCap, suche: 'abschluss studium lernen' },
      { name: 'Brain', icon: Brain, suche: 'denken gehirn logik' },
      { name: 'Lightbulb', icon: Lightbulb, suche: 'idee tipp glühbirne' },
      { name: 'Microscope', icon: Microscope, suche: 'untersuchen analyse' },
      { name: 'Compass', icon: Compass, suche: 'orientierung richtung' },
      { name: 'Rocket', icon: Rocket, suche: 'start rakete beginn' },
      { name: 'Trophy', icon: Trophy, suche: 'pokal gewinn abschluss' },
      { name: 'Medal', icon: Medal, suche: 'medaille auszeichnung' },
      { name: 'Award', icon: Award, suche: 'auszeichnung preis' },
      { name: 'Crown', icon: Crown, suche: 'krone könig meister' },
      { name: 'Target', icon: Target, suche: 'ziel zielscheibe' },
      { name: 'Flag', icon: Flag, suche: 'flagge ziel etappe' },
      { name: 'Star', icon: Star, suche: 'stern favorit' },
      { name: 'Sparkles', icon: Sparkles, suche: 'glitzern neu besonders' },
      { name: 'Zap', icon: Zap, suche: 'blitz schnell energie' },
      { name: 'Wand2', icon: Wand2, suche: 'zauberstab magie' },
      { name: 'Check', icon: Check, suche: 'haken erledigt richtig' },
    ],
  },
  {
    titel: 'Anwendungen und Alltag',
    eintraege: [
      { name: 'Landmark', icon: Landmark, suche: 'bank gebäude konto' },
      { name: 'PiggyBank', icon: PiggyBank, suche: 'sparschwein geld konto' },
      { name: 'Wallet', icon: Wallet, suche: 'geldbörse konto' },
      { name: 'CreditCard', icon: CreditCard, suche: 'karte zahlung konto' },
      { name: 'ShoppingCart', icon: ShoppingCart, suche: 'einkauf warenkorb' },
      { name: 'Truck', icon: Truck, suche: 'lieferung transport' },
      { name: 'Users', icon: Users, suche: 'personen kunden gruppe' },
      { name: 'MessageSquare', icon: MessageSquare, suche: 'nachricht chat' },
      { name: 'AtSign', icon: AtSign, suche: 'mail adresse at' },
      { name: 'Globe', icon: Globe, suche: 'welt netz international' },
      { name: 'MapPin', icon: MapPin, suche: 'ort karte position' },
      { name: 'Gamepad2', icon: Gamepad2, suche: 'spiel game steuerung' },
      { name: 'Play', icon: Play, suche: 'abspielen start' },
      { name: 'Radio', icon: Radio, suche: 'funk signal' },
      { name: 'Image', icon: Image, suche: 'bild grafik' },
      { name: 'FileText', icon: FileText, suche: 'text datei dokument' },
      { name: 'Clipboard', icon: Clipboard, suche: 'zwischenablage notiz' },
      { name: 'Bookmark', icon: Bookmark, suche: 'lesezeichen merken' },
      { name: 'Tag', icon: Tag, suche: 'etikett schild' },
      { name: 'Gift', icon: Gift, suche: 'geschenk paket' },
      { name: 'Heart', icon: Heart, suche: 'herz beliebt' },
      { name: 'Shield', icon: Shield, suche: 'schutz sicherheit' },
      { name: 'Lock', icon: Lock, suche: 'schloss sicherheit privat' },
      { name: 'Eye', icon: Eye, suche: 'auge sichtbar' },
      { name: 'Sun', icon: Sun, suche: 'sonne hell tag' },
      { name: 'Moon', icon: Moon, suche: 'mond dunkel nacht' },
      { name: 'Feather', icon: Feather, suche: 'feder leicht' },
      { name: 'Type', icon: Type, suche: 'schrift text buchstabe' },
    ],
  },
]

// Flache Suchtabelle, einmal aufgebaut.
const BY_NAME = new Map<string, LucideIcon>(
  ICON_GROUPS.flatMap((group) => group.eintraege).map((entry) => [entry.name, entry.icon]),
)

export const DEFAULT_ICON = BookOpen

// Liefert das Symbol zum Namen. Unbekannte Namen und ein leeres Feld ergeben
// das Standardsymbol — ein fehlendes Icon darf die Seitenleiste nicht zerlegen,
// und ein Name aus einer älteren Sammlung soll sie ebenso wenig stören.
export function iconByName(name: string | null | undefined): LucideIcon {
  if (!name) return DEFAULT_ICON
  return BY_NAME.get(name) ?? DEFAULT_ICON
}

export const ICON_COUNT = BY_NAME.size
