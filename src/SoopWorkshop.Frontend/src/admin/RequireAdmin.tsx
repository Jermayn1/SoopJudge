import { Outlet } from 'react-router-dom'
import { RefreshCw, ServerCrash } from 'lucide-react'
import { useAdminSession } from './useAdminSession'
import { LoginPage } from './pages/LoginPage'
import type { AdminSessionContext } from './adminOutlet'

// Wache vor dem gesamten Verwaltungsbereich.
//
// Bewusst ohne Umleitung auf eine eigene Anmeldeadresse: die Anmeldung wird an
// Ort und Stelle gezeigt. Damit bleibt die aufgerufene Adresse erhalten, und
// nach dem Anmelden steht man dort, wo man hinwollte — statt auf einer
// Startseite, von der aus man den Weg noch einmal suchen muss.
export function RequireAdmin() {
  const { state, signIn, signOut, recheck } = useAdminSession()

  if (state.kind === 'checking') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
        <div className="w-full max-w-sm space-y-3" aria-hidden="true">
          <div className="h-10 animate-pulse rounded-xl bg-slate-200" />
          <div className="h-32 animate-pulse rounded-2xl bg-slate-200" />
        </div>
        <span className="sr-only" role="status" aria-live="polite">
          Anmeldestatus wird geprüft.
        </span>
      </div>
    )
  }

  // Ein nicht erreichbares Backend ist kein fehlendes Passwort. Hier eine
  // Anmeldemaske zu zeigen wäre die irreführendste Auskunft: man tippt
  // richtig und kommt trotzdem nicht hinein.
  if (state.kind === 'unreachable') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
        <div className="max-w-md rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm anim-auf">
          <div className="mx-auto mb-4 flex w-16 h-16 items-center justify-center rounded-3xl bg-slate-100">
            <ServerCrash className="w-8 h-8 text-slate-500" aria-hidden="true" />
          </div>
          <h2 className="mb-2 text-2xl font-bold text-slate-800">Der Server antwortet nicht</h2>
          <p className="text-slate-600">{state.message}</p>
          <button
            type="button"
            onClick={recheck}
            className="mt-6 inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700 hover:-translate-y-0.5 active:translate-y-0"
          >
            <RefreshCw className="w-4 h-4" aria-hidden="true" />
            Erneut versuchen
          </button>
        </div>
      </div>
    )
  }

  if (state.kind === 'anonymous') {
    return <LoginPage onSignIn={signIn} />
  }

  return <Outlet context={{ signOut } satisfies AdminSessionContext} />
}
