import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { BehaviorSubject, finalize } from 'rxjs';
import { NotaFiscal } from '../models/nota-fiscal.model';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge/status-badge.component';
import { InvoiceStoreService } from '../services/invoice-store.service';

@Component({
  selector: 'app-nota-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatCardModule,
    StatusBadgeComponent
  ],
  templateUrl: './nota-list.component.html',
  styleUrls: ['./nota-list.component.scss']
})
export class NotaListComponent implements OnInit {
  protected store = inject(InvoiceStoreService);

  loading$ = new BehaviorSubject<boolean>(true);
  displayedColumns = ['id', 'number', 'status', 'createdAt', 'itemsCount', 'actions'];

  ngOnInit(): void {
    // refresh() (não load()) força uma nova busca sempre que a tela é aberta,
    // já que o store é singleton e "load()" reaproveitaria o cache entre navegações.
    this.store
      .refresh()
      .pipe(finalize(() => this.loading$.next(false)))
      .subscribe();
  }

  formatarData(iso: string): string {
    const d = new Date(iso);
    return d.toLocaleString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  totalItens(nota: NotaFiscal): number {
    return nota.items.reduce((acc, i) => acc + i.quantity, 0);
  }
}
