import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from './../../../../environments/environment';
import {
  CreateNotaFiscalInput,
  NotaFiscal
} from '../models/nota-fiscal.model';

@Injectable({ providedIn: 'root' })
export class NotaFiscalService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.faturamentoApiUrl}/invoices`;

  list(status?: string): Observable<NotaFiscal[]> {
    let params = new HttpParams();
    if (status && status.trim().length > 0) {
      params = params.set('status', status);
    }
    return this.http.get<NotaFiscal[]>(this.baseUrl, { params });
  }

  get(id: number): Observable<NotaFiscal> {
    return this.http.get<NotaFiscal>(`${this.baseUrl}/${id}`);
  }

  create(payload: CreateNotaFiscalInput): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(this.baseUrl, payload);
  }

  print(id: number): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(`${this.baseUrl}/${id}/print`, {});
  }
}
