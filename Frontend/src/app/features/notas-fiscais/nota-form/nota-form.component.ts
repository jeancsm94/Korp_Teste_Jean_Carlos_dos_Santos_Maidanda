import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize, map, switchMap, take } from 'rxjs';
import { NotaFiscalService } from '../services/nota-fiscal.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ProdutoStoreService } from '../../produtos/services/produto-store.service';
import { InvoiceStoreService } from '../services/invoice-store.service';

@Component({
  selector: 'app-nota-form',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatTableModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './nota-form.component.html',
  styleUrls: ['./nota-form.component.scss']
})
export class NotaFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private notaService = inject(NotaFiscalService);
  private produtoStore = inject(ProdutoStoreService);
  private invoiceStore = inject(InvoiceStoreService);
  private notifier = inject(NotificationService);

  form!: FormGroup;
  produtos$ = this.produtoStore.produtos$;
  salvando = false;
  carregandoProdutos = false;
  displayedColumns = ['produto', 'quantidade', 'actions'];

  get items(): FormArray<FormGroup> {
    return this.form.get('items') as FormArray<FormGroup>;
  }

  ngOnInit(): void {
    this.form = this.fb.group({
      items: this.fb.array<FormGroup>([], Validators.required)
    });
    this.addItem();

    this.carregandoProdutos = true;
    this.produtoStore
      .load()
      .pipe(finalize(() => (this.carregandoProdutos = false)))
      .subscribe();
  }

  private buildItemGroup(): FormGroup {
    return this.fb.group({
      productId: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]]
    });
  }

  addItem(): void {
    this.items.push(this.buildItemGroup());
  }

  removeItem(index: number): void {
    if (this.items.length > 1) {
      this.items.removeAt(index);
    }
  }

  submit(): void {
    if (this.form.invalid || this.items.length === 0 || this.salvando) return;

    this.salvando = true;
    const rawItems = this.items.getRawValue() as Array<{ productId: number | string; quantity: number }>;
    const payload = {
      items: rawItems.map(item => ({
        productId: typeof item.productId === 'string' ? parseInt(item.productId, 10) : item.productId,
        quantity: item.quantity
      }))
    };

    this.notaService
      .create(payload)
      .pipe(
        take(1),
        switchMap(notaCriada =>
          this.produtoStore.refresh().pipe(map(() => notaCriada))
        ),
        finalize(() => (this.salvando = false))
      )
      .subscribe({
        next: nota => {
          this.notifier.success('Nota fiscal criada com sucesso.');
          this.invoiceStore.refresh().subscribe();
          this.router.navigate(['/notas', nota.id]);
        }
      });
  }
}
