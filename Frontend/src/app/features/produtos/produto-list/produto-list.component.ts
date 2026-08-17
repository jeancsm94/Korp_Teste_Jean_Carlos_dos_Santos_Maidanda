import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import {
  BehaviorSubject,
  Observable,
  debounceTime,
  distinctUntilChanged,
  finalize,
  map,
  startWith,
  switchMap,
  take
} from 'rxjs';
import { Produto } from '../models/produto.model';
import { ProdutoStoreService } from '../services/produto-store.service';
import { ProdutoService } from '../services/produto.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialogComponent } from '../../../shared/ui/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-produto-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatCardModule
  ],
  templateUrl: './produto-list.component.html',
  styleUrls: ['./produto-list.component.scss']
})
export class ProdutoListComponent implements OnInit {
  protected store = inject(ProdutoStoreService);
  private produtoService = inject(ProdutoService);
  private notifier = inject(NotificationService);
  private dialog = inject(MatDialog);

  filterControl = new FormControl('', { nonNullable: true });
  loading$ = new BehaviorSubject<boolean>(true);
  displayedColumns = ['id', 'code', 'description', 'balance', 'actions'];

  filteredProdutos$!: Observable<Produto[]>;

  ngOnInit(): void {
    this.filteredProdutos$ = this.filterControl.valueChanges.pipe(
      startWith(''),
      debounceTime(250),
      distinctUntilChanged(),
      switchMap(term =>
        this.store.produtos$.pipe(
          map(produtos =>
            produtos.filter(p => {
              const t = term.toLowerCase();
              return (
                p.description.toLowerCase().includes(t) ||
                p.code.toLowerCase().includes(t)
              );
            })
          )
        )
      )
    );

    this.store
      .load()
      .pipe(finalize(() => this.loading$.next(false)))
      .subscribe();
  }

  editar(id: number): void {
    // navegação feita via routerLink no template
  }

  excluir(produto: Produto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Excluir produto',
        message: `Deseja realmente excluir o produto "${produto.description}" (código ${produto.code})?`,
        confirmText: 'Excluir',
        cancelText: 'Cancelar'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result !== true) return;

      this.produtoService
        .delete(produto.id)
        .pipe(
          take(1),
          switchMap(() => this.store.refresh())
        )
        .subscribe({
          next: () => this.notifier.success('Produto excluído com sucesso.')
        });
    });
  }
}
