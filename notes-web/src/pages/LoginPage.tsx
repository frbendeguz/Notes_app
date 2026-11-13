import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ApiError } from '../api'
import { useAuth } from '../auth/useAuth'
import { AuthLayout } from '../components/AuthLayout'
import { primaryButton } from '../components/buttonStyles'
import { FormField } from '../components/FormField'

export function LoginPage() {
  const { status, login } = useAuth()
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const sessionEnded = status.kind === 'anonymous' && status.sessionEnded

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await login(userName, password)
      // On success the GuestOnly guard sends the user to /notes.
    } catch (caught: unknown) {
      // Wrong password, unknown user, empty field: the server's single 401 message (design 3, V15).
      setError(caught instanceof ApiError ? caught.message : 'Váratlan hiba történt.')
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout title="Belépés">
      {sessionEnded && !error && (
        <p role="status" className="mb-6 border-l-4 border-stamp pl-3">
          A munkamenet lejárt vagy érvénytelen. Lépj be újra.
        </p>
      )}
      {error && (
        <p role="alert" className="mb-6 border-l-4 border-stamp pl-3 font-bold text-stamp">
          {error}
        </p>
      )}
      <form onSubmit={handleSubmit} noValidate>
        <FormField label="Felhasználónév" value={userName} onChange={setUserName} autoComplete="username" />
        <FormField
          label="Jelszó"
          type="password"
          value={password}
          onChange={setPassword}
          autoComplete="current-password"
        />
        <button type="submit" disabled={submitting} className={primaryButton}>
          {submitting ? 'Belépés…' : 'Belépés'}
        </button>
      </form>
      <p className="mt-8 text-sm">
        Még nincs fiókod?{' '}
        <Link to="/register" className="font-bold underline underline-offset-4 hover:text-stamp">
          Fiók létrehozása
        </Link>
      </p>
    </AuthLayout>
  )
}
