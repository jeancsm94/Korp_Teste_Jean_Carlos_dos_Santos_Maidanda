import { Component, OnInit, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { NotaFiscal } from '../models/nota-fiscal.model';
import { NotaFiscalService } from '../services/nota-fiscal.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge/status-badge.component';
import { ProdutoStoreService } from '../../produtos/services/produto-store.service';
import { InvoiceStoreService } from '../services/invoice-store.service';

@Component({
  selector: 'app-nota-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    StatusBadgeComponent
  ],
  templateUrl: './nota-detail.component.html',
  styleUrls: ['./nota-detail.component.scss']
})
export class NotaDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private notaService = inject(NotaFiscalService);
  private notifier = inject(NotificationService);
  private produtoStore = inject(ProdutoStoreService);
  private invoiceStore = inject(InvoiceStoreService);
  private destroyRef = inject(DestroyRef);

  nota = signal<NotaFiscal | null>(null);
  loadingNota = signal(true);
  printing = signal(false);
  printError = signal<string | null>(null);
  displayedColumns = ['productCode', 'productDescription', 'quantity'];

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) {
      this.notifier.error('ID da nota fiscal não informado.');
      this.router.navigate(['/notas']);
      return;
    }

    const id = parseInt(idParam, 10);
    this.notaService
      .get(id)
      .pipe(
        finalize(() => this.loadingNota.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: n => this.nota.set(n)
      });
  }

  get podeImprimir(): boolean {
    const n = this.nota();
    return !!n && n.status === 'Aberta' && !this.printing();
  }

  formatarData(iso?: string | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  totalItens(): number {
    const n = this.nota();
    if (!n) return 0;
    return n.items.reduce((acc, i) => acc + i.quantity, 0);
  }

  imprimir(): void {
    const n = this.nota();
    if (!n || this.printing()) return;

    this.printing.set(true);
    this.printError.set(null);

    this.notaService
      .print(n.id)
      .pipe(
        finalize(() => this.printing.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: updated => {
          this.nota.set({ ...n, status: updated.status, closedAt: updated.closedAt });
          this.notifier.success('Nota fiscal impressa com sucesso.');
          this.produtoStore.refresh().subscribe();
          this.invoiceStore.refresh().subscribe();
        },
        error: (err: HttpErrorResponse) => {
          this.printError.set(this.mapPrintError(err));
        }
      });
  }

  private mapPrintError(err: HttpErrorResponse): string {
    const problem = err.error as ProblemDetails | undefined;
    if (err.status === 503) {
      return 'O serviço de estoque está indisponível no momento. Tente novamente em instantes.';
    }
    if (err.status === 409) {
      return (
        problem?.detail ??
        'Não foi possível imprimir: a nota já não está mais aberta ou não há saldo suficiente de estoque.'
      );
    }
    return problem?.detail ?? 'Ocorreu um erro ao imprimir a nota fiscal.';
  }
}
