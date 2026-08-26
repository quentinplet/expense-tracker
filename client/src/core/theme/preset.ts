import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

/**
 * Aura est livré avec `emerald` en primaire ; la maquette est en indigo.
 *
 * On surcharge la palette **sémantique**, pas les couleurs de chaque composant :
 * les deux schémas d'Aura dérivent de `{primary.500}` en clair et `{primary.400}`
 * en sombre, donc un seul point de surcharge suffit pour les deux.
 */
export const ExpenseTrackerPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{indigo.50}',
      100: '{indigo.100}',
      200: '{indigo.200}',
      300: '{indigo.300}',
      400: '{indigo.400}',
      500: '{indigo.500}',
      600: '{indigo.600}',
      700: '{indigo.700}',
      800: '{indigo.800}',
      900: '{indigo.900}',
      950: '{indigo.950}',
    },
  },
});
