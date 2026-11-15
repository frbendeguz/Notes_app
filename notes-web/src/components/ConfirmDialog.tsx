import { useEffect, useId, useRef, type ReactNode } from 'react'
import { dangerButton, secondaryButton } from './buttonStyles'
import { ErrorAlert } from './ErrorAlert'

interface ConfirmDialogProps {
  open: boolean
  title: string
  children: ReactNode
  confirmLabel: string
  cancelLabel?: string
  onConfirm: () => void
  onCancel: () => void
  /** The confirmed action is running: both buttons are disabled. */
  busy?: boolean
  /** Message of a failed confirmed action, shown inside the dialog. */
  error?: string | null
}

/**
 * Modal confirmation on the native <dialog> (showModal): the browser traps the focus and closes it with Esc.
 * The safe choice ("Mégse") gets the initial focus, so Enter alone never destroys anything.
 */
export function ConfirmDialog({
  open,
  title,
  children,
  confirmLabel,
  cancelLabel = 'Mégse',
  onConfirm,
  onCancel,
  busy = false,
  error = null,
}: ConfirmDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const cancelRef = useRef<HTMLButtonElement>(null)
  const titleId = useId()

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    if (open && !dialog.open) {
      dialog.showModal()
      cancelRef.current?.focus()
    } else if (!open && dialog.open) {
      dialog.close()
    }
  }, [open])

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby={titleId}
      onCancel={(event) => {
        // Esc: let React state decide whether the dialog is open.
        event.preventDefault()
        if (!busy) onCancel()
      }}
      className="m-auto w-[min(32rem,calc(100%-2rem))] rounded-sm border-2 border-ink bg-manila p-6 text-ink backdrop:bg-ink/70"
    >
      <h2 id={titleId} className="mb-3 text-xl font-bold">
        {title}
      </h2>
      <div className="mb-6">{children}</div>
      {error && <ErrorAlert className="mb-6">{error}</ErrorAlert>}
      <div className="flex flex-wrap justify-end gap-3">
        <button ref={cancelRef} type="button" onClick={onCancel} disabled={busy} className={secondaryButton}>
          {cancelLabel}
        </button>
        <button type="button" onClick={onConfirm} disabled={busy} className={dangerButton}>
          {confirmLabel}
        </button>
      </div>
    </dialog>
  )
}
