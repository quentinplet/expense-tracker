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

    colorScheme: {
      /**
       * Aura livre `color: {surface.700}` / `placeholderColor: {surface.500}` pour
       * un champ de saisie : deux gris de milieu de gamme, trop proches l'un de
       * l'autre pour distinguer une vraie valeur d'un texte d'exemple au premier
       * coup d'œil. `900` aligne la vraie valeur sur le texte du reste de l'app
       * (ex. `text-surface-900` des titres) ; `400`, nettement plus clair, élargit
       * l'écart avec le placeholder plutôt que de le resserrer.
       */
      light: {
        formField: {
          color: '{surface.900}',
          placeholderColor: '{surface.400}',
        },
      },

      /**
       * Aura dérive sa primaire sombre de `{primary.400}` avec une couleur de
       * contraste sombre : on obtenait un indigo pâle sur texte quasi noir, alors
       * que les maquettes montrent un indigo saturé sur texte blanc.
       *
       * C'est le seul point où corriger, sinon chaque bouton le referait à sa façon.
       */
      dark: {
        primary: {
          color: '{indigo.500}',
          contrastColor: '#ffffff',
          hoverColor: '{indigo.400}',
          activeColor: '{indigo.600}',
        },

        /**
         * Même écart à créer qu'en clair, mais dans l'autre sens : la vraie valeur
         * (`{surface.0}`, blanc) est déjà correcte par défaut. C'est le placeholder
         * qui doit s'assombrir d'un cran (`400` → `500` de la rampe sur mesure
         * ci-dessous) pour ne pas se lire presque aussi clair que du texte réel.
         */
        formField: {
          placeholderColor: '{surface.500}',
        },

        /**
         * Aura livre `zinc`, strictement neutre ; les maquettes sont légèrement
         * marine. Aucune palette Tailwind ne tombe juste — `gray` et `slate`
         * virent trop bleu — d'où cette rampe sur mesure, à teinte constante
         * (~224°) et saturation qui décroît en montant vers les clairs.
         *
         * Ancrée sur deux valeurs relevées dans les maquettes :
         *   950 `#0b0c13` — fond de page
         *   900 `#12161f` — sidebar et cartes
         *
         * Le mode clair garde `zinc` : les maquettes ne le couvrent pas.
         */
        surface: {
          0: '#ffffff',
          50: '#f6f7f9',
          100: '#eaecf0',
          200: '#d9dbe3',
          300: '#b9becb',
          400: '#8b92a7',
          500: '#626b84',
          600: '#474f66',
          700: '#30374b',
          800: '#1d2230',
          900: '#12161f',
          950: '#0b0c13',
        },
      },
    },
  },
});
