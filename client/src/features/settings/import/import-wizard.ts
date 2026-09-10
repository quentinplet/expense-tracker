import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';
import { FileSelectEvent, FileUpload } from 'primeng/fileupload';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ImportService } from '@/core/services/import-service';
import { CategorieService } from '@/core/services/categorie-service';
import { Categorie } from '@/types/categorie';
import { ColumnMappingOverride, ImportConfirmResult, ImportConfirmRow, ImportPreview } from '@/types/import';
import { ColumnMapper } from './components/column-mapper/column-mapper';
import { PreviewTable } from './components/preview-table/preview-table';
import { ImportHistory } from './components/import-history/import-history';

type WizardStep = 'upload' | 'mapping' | 'preview' | 'result';

/**
 * Container 3 étapes (upload → mapping/prévisualisation → résultat), sans état
 * conservé côté serveur entre les deux appels (§ Flux en deux temps, doc feature
 * Import CSV) : le fichier choisi reste en mémoire ici tant que l'utilisateur n'a
 * pas confirmé ou recommencé.
 */
@Component({
  selector: 'app-import-wizard',
  imports: [FileUpload, Button, TranslatePipe, ColumnMapper, PreviewTable, ImportHistory],
  templateUrl: './import-wizard.html',
  styleUrl: './import-wizard.scss',
})
export class ImportWizard implements OnInit {
  private importService = inject(ImportService);
  private categorieService = inject(CategorieService);
  private messageService = inject(MessageService);
  private translate = inject(TranslateService);
  private router = inject(Router);

  protected step = signal<WizardStep>('upload');
  protected loading = signal(false);
  protected serverError = signal<string | null>(null);

  protected categories = signal<Categorie[]>([]);
  protected preview = signal<ImportPreview | null>(null);
  protected result = signal<ImportConfirmResult | null>(null);

  /** Recharge ImportHistory après chaque import confirmé. */
  protected historyVersion = signal(0);

  private file: File | null = null;

  ngOnInit() {
    this.categorieService.getCategories().subscribe((categories) => this.categories.set(categories));
  }

  onFileSelect(event: FileSelectEvent) {
    const file = (event.files as File[])[0];
    if (!file) return;
    this.file = file;
    this.runPreview();
  }

  onMappingSubmit(mapping: ColumnMappingOverride) {
    this.runPreview(mapping);
  }

  private runPreview(mapping?: ColumnMappingOverride) {
    if (!this.file) return;
    this.loading.set(true);
    this.serverError.set(null);

    this.importService.preview(this.file, mapping).subscribe({
      next: (preview) => {
        this.loading.set(false);
        this.preview.set(preview);
        this.step.set(this.needsManualMapping(preview) ? 'mapping' : 'preview');
      },
      error: (error) => {
        this.loading.set(false);
        this.serverError.set(this.extractServerMessage(error));
      },
    });
  }

  private needsManualMapping(preview: ImportPreview): boolean {
    const m = preview.suggestedMapping;
    return !m.dateColumn || !m.labelColumn || (!m.amountColumn && !m.debitColumn && !m.creditColumn);
  }

  onConfirm(rows: ImportConfirmRow[]) {
    if (!this.file || rows.length === 0) return;
    this.loading.set(true);
    this.serverError.set(null);

    this.importService.confirm(this.file.name, rows).subscribe({
      next: (result) => {
        this.loading.set(false);
        this.result.set(result);
        this.step.set('result');
        this.historyVersion.update((v) => v + 1);
      },
      error: (error) => {
        this.loading.set(false);
        this.serverError.set(this.extractServerMessage(error));
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('import.wizard.confirmFailed'),
        });
      },
    });
  }

  startOver() {
    this.file = null;
    this.preview.set(null);
    this.result.set(null);
    this.serverError.set(null);
    this.step.set('upload');
  }

  goToTransactions() {
    this.router.navigateByUrl('/transactions');
  }

  private extractServerMessage(error: unknown): string | null {
    const body = (error as { error?: unknown } | undefined)?.error;
    if (Array.isArray(body)) return (body[0] as string) ?? null;
    if (typeof body === 'string') return body;
    return null;
  }
}
