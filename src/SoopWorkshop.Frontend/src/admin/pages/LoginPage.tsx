import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, KeyRound, Loader2 } from 'lucide-react'
import { BrandMark } from '../../components/BrandMark'

type LoginPageProps = {
  /** Liefert die Begründung des Servers, oder null bei Erfolg. */
  onSignIn: (password: string) => Promise<string | null>
}

export function LoginPage({ onSignIn }: LoginPageProps) {
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [sending, setSending] = useState(false)

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (sending || password.length === 0) return

    setSending(true)
    setError(null)

    const problem = await onSignIn(password)

    setSending(false)

    if (problem !== null) {
      setError(problem)
      // Das falsche Passwort stehen zu lassen hilft niemandem — der nächste
      // Versuch fängt von vorn an.
      setPassword('')
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6 dark:bg-neutral-950">
      <div className="w-full max-w-sm anim-auf">
        <div className="mb-8 flex flex-col items-center gap-3">
          <BrandMark size={44} />
          <h1 className="text-2xl font-bold text-slate-800 dark:text-neutral-100">Verwaltung</h1>
          <p className="text-center text-sm text-slate-600 dark:text-neutral-400">
            Dieser Bereich ist dem Betreuer vorbehalten.
          </p>
        </div>

        <form
          onSubmit={submit}
          className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-white/10 dark:bg-neutral-900"
        >
          <label htmlFor="admin-passwort" className="block text-sm font-semibold text-slate-700 dark:text-neutral-300">
            Passwort
          </label>

          <div className="relative mt-2">
            <KeyRound
              className="pointer-events-none absolute left-3 top-1/2 w-4 h-4 -translate-y-1/2 text-slate-400 dark:text-neutral-500"
              aria-hidden="true"
            />
            <input
              id="admin-passwort"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="current-password"
              autoFocus
              aria-describedby={error ? 'admin-passwort-fehler' : undefined}
              aria-invalid={error !== null}
              className="w-full rounded-xl border border-slate-300 bg-white py-2.5 pl-9 pr-3 text-slate-900 transition-colors placeholder:text-slate-400 hover:border-slate-400 focus:border-indigo-500 dark:border-white/15 dark:bg-neutral-950 dark:text-neutral-100 dark:placeholder:text-neutral-500 dark:hover:border-white/25"
              placeholder="••••••••"
            />
          </div>

          {error && (
            <p
              id="admin-passwort-fehler"
              role="alert"
              className="mt-3 rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-800 dark:border-rose-400/30 dark:bg-rose-500/10 dark:text-rose-200"
            >
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={sending || password.length === 0}
            className="mt-5 flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700 hover:-translate-y-0.5 active:translate-y-0 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:shadow-none disabled:hover:translate-y-0 dark:shadow-black/30 dark:disabled:bg-neutral-800"
          >
            {sending && <Loader2 className="w-4 h-4 animate-spin" aria-hidden="true" />}
            {sending ? 'Wird geprüft …' : 'Anmelden'}
          </button>
        </form>

        <Link
          to="/"
          className="mt-6 flex items-center justify-center gap-1.5 text-sm font-medium text-slate-600 hover:text-slate-900 hover:underline dark:text-neutral-400 dark:hover:text-neutral-100"
        >
          <ArrowLeft className="w-4 h-4" aria-hidden="true" />
          Zurück zu den Aufgaben
        </Link>
      </div>
    </div>
  )
}
