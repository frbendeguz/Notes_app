import type { MouseEvent, ReactNode } from 'react'
import { Link } from 'react-router-dom'

interface GuardedLinkProps {
  to: string
  /** Called instead of the normal navigation, so the page can ask about unsaved changes first. */
  onNavigate: (to: string) => void
  className?: string
  'aria-current'?: 'page' | 'true'
  children: ReactNode
}

/**
 * A real link (keyboard, middle click, "open in new tab" keep working), but a plain left click goes through
 * `onNavigate`: the notes page asks before an unsaved note is left (developer decision, task 07).
 */
export function GuardedLink({ to, onNavigate, children, ...rest }: GuardedLinkProps) {
  function handleClick(event: MouseEvent<HTMLAnchorElement>) {
    if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return
    event.preventDefault()
    onNavigate(to)
  }

  return (
    <Link to={to} onClick={handleClick} {...rest}>
      {children}
    </Link>
  )
}
