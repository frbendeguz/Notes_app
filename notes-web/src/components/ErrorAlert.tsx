import type { ReactNode } from 'react'

/** The server's (or the client check's) message as a stamp-red margin note; announced by screen readers. */
export function ErrorAlert({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <p role="alert" className={`border-l-4 border-stamp pl-3 font-bold text-stamp ${className}`}>
      {children}
    </p>
  )
}
