import { useId } from 'react'

type BrandMarkProps = {
  size?: number
}

// Das Zeichen der Anwendung. Bewusst die einzige blaue Fläche im Produkt:
// es kennzeichnet die Marke, während Bernstein die Bedienung führt und
// Grün und Rot ausschließlich Bewertungen tragen.
//
// Form und Farben sind aus der Vorlage des Betreuers abgelesen, nicht
// geschätzt (S SoopJudge Fav Idee.png, 1024 px):
//
// • Eckradius 224 von 1024, also genau 7 bei viewBox 32.
// • Der Verlauf läuft über die Diagonale — das ist bei einer quadratischen
//   Fläche dasselbe wie die Vorgabe „135 Grad".
// • Der Buchstabe ist Sora 700. Sora liegt nicht im Projekt, und ein
//   Schriftpaket für einen einzigen Buchstaben wäre teuer bezahlt, deshalb
//   steht er als Pfad da. Er weicht nirgends mehr als 3,8 von 1024 Pixeln ab.
//
// Die fünf Stopps sehen umständlich aus, sind aber gemessen: die Vorlage
// verläuft nicht gerade zwischen ihren Endfarben, in der Mitte liegt Grün
// rund 4 Stufen darüber. Die genannte Vorgabe #1B3FEE nach #22D3EE trifft
// die Ecken bis auf den Blaukanal (245 statt 238) und ergibt über die Fläche
// eine mittlere Abweichung von 5,7 von 255; mit diesen Stopps sind es 1,7.
//
// Dieselbe Form liegt unter public/ noch einmal als Rastergrafik für den
// Browser-Tab — ein Favicon lässt sich nicht aus einer React-Komponente
// beziehen. Wird hier etwas geändert, gehören die Dateien dort mit erneuert,
// sonst zeigen Tab und Seitenleiste zweierlei.
export function BrandMark({ size = 28 }: BrandMarkProps) {
  // Bei mehreren Marken auf einer Seite dürfen sich die Verlaufs-IDs nicht
  // überschneiden, sonst zieht die zweite die Füllung der ersten.
  const gradientId = useId()

  return (
    <svg width={size} height={size} viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#1d41f5" />
          <stop offset="0.25" stopColor="#2068f3" />
          <stop offset="0.5" stopColor="#228ff1" />
          <stop offset="0.75" stopColor="#22b3f0" />
          <stop offset="1" stopColor="#22d3ef" />
        </linearGradient>
      </defs>
      <rect width="32" height="32" rx="7" fill={`url(#${gradientId})`} />
      <path
        d="M18.53 19.22C19.01 21.76 15.5 21.87 14.03 20.89C12.95 20.05 12.69 18.02 10.88 18.61C10.65 18.68 10.46 18.79 10.27 18.94C8.66 20.5 10.7 22.85 12.13 23.68C14.93 25.3 19.79 25.04 21.6 22.03C21.99 21.37 22.22 20.62 22.28 19.85C22.53 16.7 20.15 15.17 17.43 14.45C16.24 14.14 14.25 14.04 13.74 12.69C13.28 10.48 16.26 9.97 17.7 10.92C18.81 11.7 18.95 13.85 20.8 13.27C21.02 13.2 21.21 13.09 21.4 12.95C22.99 11.56 21.22 9.23 19.91 8.39C17.21 6.66 12.34 6.92 10.62 9.95C9.61 11.74 9.76 14.25 11.34 15.67C12.29 16.54 13.51 16.93 14.73 17.27C15.98 17.62 18.11 17.73 18.53 19.22Z"
        fill="#ffffff"
      />
    </svg>
  )
}
