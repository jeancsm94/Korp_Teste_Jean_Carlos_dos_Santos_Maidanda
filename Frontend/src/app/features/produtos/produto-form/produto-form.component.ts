import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, take, switchMap } from 'rxjs';
import { ProdutoService } from '../services/produto.service';
import { ProdutoStoreService } from '../services/produto-store.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CreateProdutoInput, UpdateProdutoInput } from '../models/produto.model';

@Component({
  selector: 'app-produto-form',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatIconModule
  ],
  templateUrl: './produto-form.component.html',
  styleUrls: ['./produto-form.component.scss']
})
export class ProdutoFormComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private produtoService = inject(ProdutoService);
  private store = inject(ProdutoStoreService);
  private notifier = inject(NotificationService);
  private destroyRef = inject(DestroyRef);

  form!: FormGroup;
  editando = false;
  produtoId: number | null = null;
  carregando = false;
  salvando = false;

  ngOnInit(): void {
    this.form = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(50)]],
      description: ['', [Validators.required, Validators.maxLength(500)]],
      balance: [0, [Validators.required, Validators.min(0)]]
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.editando = true;
      this.produtoId = parseInt(idParam, 10);
      this.carregarProduto(this.produtoId);
    }
  }

  private carregarProduto(id: number): void {
    this.carregando = true;
    this.produtoService
      .get(id)
      .pipe(
        finalize(() => this.carregando = false),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: produto => {
          this.form.patchValue({
            code: produto.code,
            description: produto.description,
            balance: produto.balance
          });
        }
      });
  }

  submit(): void {
    if (this.form.invalid || this.salvando) return;

    this.salvando = true;
    const values = this.form.getRawValue();

    const request$ = this.editando
      ? this.produtoService.update(this.produtoId!, {
          code: values.code,
          description: values.description,
          balance: values.balance
        } as UpdateProdutoInput)
      : this.produtoService.create({
          code: values.code,
          description: values.description,
          initialBalance: values.balance
        } as CreateProdutoInput);

    request$
      .pipe(
        take(1),
        switchMap(() => this.store.refresh()),
        finalize(() => this.salvando = false)
      )
      .subscribe({
        next: () => {
          this.notifier.success(
            this.editando ? 'Produto atualizado com sucesso.' : 'Produto criado com sucesso.'
          );
          this.router.navigate(['/produtos']);
        }
      });
  }
}
