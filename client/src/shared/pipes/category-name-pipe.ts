import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type NamedCategory = {
  name: string;
  translationKey?: string | null;
};

/**
 * Le nom affichable d'une catégorie, résolu **en un seul endroit** :
 * `translationKey ? translate(translationKey) : name`.
 *
 * Une catégorie système porte une clé et se traduit ; une catégorie créée par
 * l'utilisateur n'en a pas et s'affiche telle quelle — on ne traduit jamais un
 * libellé saisi par l'utilisateur (§10).
 *
 * Impur à dessein : le nom doit suivre un changement de langue à chaud.
 */
@Pipe({
  name: 'categoryName',
  pure: false,
})
export class CategoryNamePipe implements PipeTransform {
  private translate = inject(TranslateService);

  transform(category: NamedCategory | null | undefined): string {
    if (!category) return '';
    if (!category.translationKey) return category.name;

    const translated = this.translate.instant(category.translationKey);

    // ngx-translate renvoie la clé quand elle est absente : on retombe sur le nom
    // plutôt que d'afficher « category.system.housing » à l'utilisateur.
    return translated === category.translationKey ? category.name : translated;
  }
}