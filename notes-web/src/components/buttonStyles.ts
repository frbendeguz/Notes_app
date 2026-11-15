// Shared Tailwind class lists, so the button look is defined once.

/** Main action on a manila sheet (e.g. "Belépés"). */
export const primaryButton =
  'rounded-sm bg-ink px-5 py-2 font-bold text-manila hover:bg-stamp disabled:cursor-wait disabled:opacity-70'

/** Secondary action on a manila sheet (e.g. "Frissítés"). */
export const secondaryButton =
  'rounded-sm border-2 border-ink px-3 py-1 font-bold hover:bg-manila-dark disabled:cursor-wait disabled:opacity-70'

/** Destructive action (e.g. "Törlés"): stamp red, used only behind a confirmation. */
export const dangerButton =
  'rounded-sm border-2 border-stamp px-3 py-1 font-bold text-stamp hover:bg-stamp hover:text-manila disabled:cursor-wait disabled:opacity-70'
