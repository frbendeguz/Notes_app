import type { ReactNode } from 'react'

/** Login / register frame: a narrow index card on the cabinet background. */
export function AuthLayout({ title, children }: { title: string; children: ReactNode }) {
  return (
    <main className="mx-auto max-w-md px-4 py-12 sm:py-20">
      <p className="mb-4 text-manila">Jegyzetek</p>
      <div className="rounded-sm bg-manila px-6 py-8 sm:px-8">
        <h1 className="mb-6 text-3xl font-bold">{title}</h1>
        {children}
      </div>
    </main>
  )
}
