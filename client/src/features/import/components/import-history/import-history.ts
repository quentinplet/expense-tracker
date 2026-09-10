import { Component, computed, inject, input, OnChanges, OnInit, signal, SimpleChanges } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Button } from 'primeng/button';
import { ConfirmationService, MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ImportService } from '@/core/services/import-service';
import { LanguageService } from '@/core/services/language-service';
import { ImportBatch } from '@/types/import';

/**
 * Liste des imports passés avec bouton d'annulation (§ Key Gotchas frontend, doc
 * feature Import CSV) — c'est ce qui rend DELETE /api/import/batches/{id}
 * atteignable depuis l'UI, pas seulement le résultat de l'import qui vient d'être fait.
 */
@Component({
  selector: 'app-import-history',
  imports: [Button, TranslatePipe, DatePipe],
  templateUrl: './import-history.html',
})
export class ImportHistory implements OnInit, OnChanges {
  /** Incrémenté par le parent après chaque import confirmé — recharge la liste. */
  refreshTrigger = input(0);

  private importService = inject(ImportService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);
  private languageService = inject(LanguageService);

  protected locale = computed(() => this.languageService.current());
  protected batches = signal<ImportBatch[]>([]);
  protected loading = signal(false);
  protected cancellingId = signal<string | null>(null);

  ngOnInit() {
    this.load();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) this.load();
  }

  private load() {
    this.loading.set(true);
    this.importService.getBatches().subscribe({
      next: (batches) => {
        this.batches.set(batches);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  cancelImport(batch: ImportBatch) {
    this.confirmationService.confirm({
      message: this.translate.instant('import.history.cancelConfirm', { fileName: batch.fileName }),
      header: this.translate.instant('common.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.cancellingId.set(batch.id);
        this.importService.deleteBatch(batch.id).subscribe({
          next: () => {
            this.cancellingId.set(null);
            this.batches.update((batches) => batches.filter((b) => b.id !== batch.id));
            this.messageService.add({
              severity: 'success',
              summary: this.translate.instant('common.success'),
              detail: this.translate.instant('import.history.cancelled'),
              life: 3000,
            });
          },
          error: () => {
            this.cancellingId.set(null);
            this.messageService.add({
              severity: 'error',
              summary: this.translate.instant('common.error'),
              detail: this.translate.instant('import.history.cancelFailed'),
            });
          },
        });
      },
    });
  }
}
