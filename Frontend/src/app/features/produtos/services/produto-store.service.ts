import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap, take } from 'rxjs';
import { Produto } from '../models/produto.model';
import { ProdutoService } from './produto.service';

@Injectable({ providedIn: 'root' })
export class ProdutoStoreService {
  private produtoService = inject(ProdutoService);
  private produtosSubject = new BehaviorSubject<Produto[]>([]);
  readonly produtos$ = this.produtosSubject.asObservable();
  get produtosAtuais(): Produto[] {
    return this.produtosSubject.getValue();
  }
  private loaded = false;

  load(force = false): Observable<Produto[]> {
    if (this.loaded && !force) {
      return this.produtos$.pipe(take(1));
    }
    return this.produtoService.list().pipe(
      tap(p => {
        this.produtosSubject.next(p);
        this.loaded = true;
      })
    );
  }

  refresh(): Observable<Produto[]> {
    return this.load(true);
  }

  set(produtos: Produto[]): void {
    this.produtosSubject.next(produtos);
  }
}
