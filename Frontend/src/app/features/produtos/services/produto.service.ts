import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from './../../../../environments/environment';
import { CreateProdutoInput, Produto, UpdateProdutoInput } from '../models/produto.model';

@Injectable({ providedIn: 'root' })
export class ProdutoService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.estoqueApiUrl}/products`;

  list(search?: string): Observable<Produto[]> {
    let params = new HttpParams();
    if (search && search.trim().length > 0) {
      params = params.set('search', search);
    }
    return this.http.get<Produto[]>(this.baseUrl, { params });
  }

  get(id: number): Observable<Produto> {
    return this.http.get<Produto>(`${this.baseUrl}/${id}`);
  }

  create(payload: CreateProdutoInput): Observable<Produto> {
    return this.http.post<Produto>(this.baseUrl, payload);
  }

  update(id: number, payload: UpdateProdutoInput): Observable<Produto> {
    return this.http.put<Produto>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
