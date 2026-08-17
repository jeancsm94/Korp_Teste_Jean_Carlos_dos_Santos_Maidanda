import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';
import { ProblemDetails } from '../models/problem-details.model';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifier = inject(NotificationService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const problem = err.error as ProblemDetails | undefined;
      const message = problem?.detail ?? problem?.title ?? defaultMessageFor(err.status);
      notifier.error(message);
      return throwError(() => err);
    })
  );
};

function defaultMessageFor(status: number): string {
  switch (status) {
    case 503:
      return 'O serviço de estoque está indisponível no momento. Tente novamente em instantes.';
    case 409:
      return 'Não foi possível concluir a operação devido a um conflito de estado.';
    case 404:
      return 'Registro não encontrado.';
    default:
      return 'Ocorreu um erro inesperado. Tente novamente.';
  }
}
