import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ApiError } from '../api'
import { useAuth } from '../auth/useAuth'
import { AuthLayout } from '../components/AuthLayout'
import { primaryButton } from '../components/buttonStyles'
import { FormField } from '../components/FormField'
import type { FieldErrors } from '../types'

// Shown before typing (design 6). The server stays the judge: its errors.password messages appear under the field.
const passwordRules = [
  '8–128 karakter',
  'legalább egy kisbetű',
  'legalább egy nagybetű',
  'legalább egy számjegy (0–9)',
]

export function RegisterPage() {
  const { register } = useAuth()
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    setFieldErrors({})
    try {
      await register(userName, password)
      // 201 comes with a token (V11): the GuestOnly guard sends the user to /notes.
    } catch (caught: unknown) {
      if (caught instanceof ApiError) {
        setError(caught.message) // 400: "A megadott adatok érvénytelenek.", 409: name taken
        setFieldErrors(caught.errors)
      } else {
        setError('Váratlan hiba történt.')
      }
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout title="Fiók létrehozása">
      {error && (
        <p role="alert" className="mb-6 border-l-4 border-stamp pl-3 font-bold text-stamp">
          {error}
        </p>
      )}
      <form onSubmit={handleSubmit} noValidate>
        <FormField
          label="Felhasználónév"
          value={userName}
          onChange={setUserName}
          autoComplete="username"
          hint="3–32 karakter: angol betű, számjegy, _ . -"
          errors={fieldErrors.userName}
        />
        <FormField
          label="Jelszó"
          type="password"
          value={password}
          onChange={setPassword}
          autoComplete="new-password"
          hint={
            <ul className="list-inside list-['–_']">
              {passwordRules.map((rule) => (
                <li key={rule}>{rule}</li>
              ))}
            </ul>
          }
          errors={fieldErrors.password}
        />
        <button type="submit" disabled={submitting} className={primaryButton}>
          {submitting ? 'Fiók létrehozása…' : 'Fiók létrehozása'}
        </button>
      </form>
      <p className="mt-8 text-sm">
        Már van fiókod?{' '}
        <Link to="/login" className="font-bold underline underline-offset-4 hover:text-stamp">
          Belépés
        </Link>
      </p>
    </AuthLayout>
  )
}
