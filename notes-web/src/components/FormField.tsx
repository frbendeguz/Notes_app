import { useId, type ReactNode } from 'react'

interface FormFieldProps {
  label: string
  type?: 'text' | 'password'
  value: string
  onChange: (value: string) => void
  autoComplete: string
  /** Shown under the label before typing (e.g. the password rules). */
  hint?: ReactNode
  /** The server's messages for this field (`errors.<field>`). */
  errors?: string[]
}

/** Labelled input on a "typed form line"; hint and server errors are linked with aria-describedby. */
export function FormField({ label, type = 'text', value, onChange, autoComplete, hint, errors = [] }: FormFieldProps) {
  const id = useId()
  const hintId = `${id}-hint`
  const errorId = `${id}-error`
  const describedBy = [hint ? hintId : null, errors.length > 0 ? errorId : null].filter(Boolean).join(' ')

  return (
    <div className="mb-6">
      <label htmlFor={id} className="block font-bold">
        {label}
      </label>
      {hint && (
        <div id={hintId} className="mt-1 text-sm text-faded">
          {hint}
        </div>
      )}
      <input
        id={id}
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        autoComplete={autoComplete}
        aria-invalid={errors.length > 0}
        aria-describedby={describedBy || undefined}
        className="mt-2 w-full border-b-2 border-ink bg-transparent px-1 py-1.5 text-lg aria-invalid:border-stamp"
      />
      {errors.length > 0 && (
        <ul id={errorId} className="mt-2 text-sm text-stamp">
          {errors.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      )}
    </div>
  )
}
