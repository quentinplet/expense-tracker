/**
 * Le mois circule partout sous forme de chaîne `YYYY-MM` : c'est le format de l'URL,
 * celui de l'API et celui des points de la courbe. Les conversions passent toutes
 * par ici pour éviter les décalages de fuseau — un `new Date('2026-08')` est
 * interprété en UTC et peut reculer d'un mois selon le fuseau du navigateur.
 */
export type MonthKey = string;

/**
 * Portée d'un widget : le mois affiché, ou les douze derniers mois glissants
 * (« Année », valeur `'all'` conservée en interne). C'est un réglage local à
 * chaque graphique — la période globale, elle, reste toujours un mois.
 */
export type Scope = 'month' | 'all';

export function toMonthKey(date: Date): MonthKey {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
}

export function currentMonthKey(): MonthKey {
  return toMonthKey(new Date());
}

export function isValidMonthKey(value: string | null | undefined): value is MonthKey {
  return !!value && /^\d{4}-(0[1-9]|1[0-2])$/.test(value);
}

/** Construit une date locale au premier du mois, sans passer par le parsing ISO. */
export function monthKeyToDate(month: MonthKey): Date {
  const [year, monthNumber] = month.split('-').map(Number);
  return new Date(year, monthNumber - 1, 1);
}

/**
 * Même précaution pour un jour : `new Date('2026-08-01')` est interprété en UTC,
 * donc affiché la veille dans tout fuseau négatif.
 */
export function dayKeyToDate(day: string): Date {
  const [year, month, date] = day.split('-').map(Number);
  return new Date(year, month - 1, date);
}

export function shiftMonth(month: MonthKey, offset: number): MonthKey {
  const date = monthKeyToDate(month);
  date.setMonth(date.getMonth() + offset);
  return toMonthKey(date);
}

/** « Suivant » est désactivé au-delà du mois courant : il n'y a rien à y voir. */
export function isAtOrAfterCurrentMonth(month: MonthKey): boolean {
  return month >= currentMonthKey();
}
