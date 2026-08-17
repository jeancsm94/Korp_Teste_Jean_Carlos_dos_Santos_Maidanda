import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { NotaFiscal } from '../models/nota-fiscal.model';
import { NotaFiscalService } from './nota-fiscal.service';

@Injectable({ providedIn: 'root' })
export class InvoiceStoreService {
  private notaFiscalService = inject(NotaFiscalService);
  private notasSubject = new BehaviorSubject<NotaFiscal[]>([]);
  readonly notas$ = this.notasSubject.asObservable();
  get notasAtuais(): NotaFiscal[] {
    return this.notasSubject.getValue();
  }
  private loaded = false;

  load(force = false): Observable<NotaFiscal[]> {
    if (this.loaded && !force) {
      return this.notas$;
    }
    return this.notaFiscalService.list().pipe(
      tap(n => {
        this.notasSubject.next(n);
        this.loaded = true;
      })
    );
  }

  refresh(): Observable<NotaFiscal[]> {
    return this.load(true);
  }

  set(notas: NotaFiscal[]): void {
    this.notasSubject.next(notas);
  }
}
