import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./home-page/home-page.component').then(m => m.HomePageComponent)
  },
  {
    path: 'produtos',
    loadComponent: () =>
      import('./features/produtos/produto-list/produto-list.component').then(m => m.ProdutoListComponent)
  },
  {
    path: 'produtos/novo',
    loadComponent: () =>
      import('./features/produtos/produto-form/produto-form.component').then(m => m.ProdutoFormComponent)
  },
  {
    path: 'produtos/:id/editar',
    loadComponent: () =>
      import('./features/produtos/produto-form/produto-form.component').then(m => m.ProdutoFormComponent)
  },
  {
    path: 'notas',
    loadComponent: () =>
      import('./features/notas-fiscais/nota-list/nota-list.component').then(m => m.NotaListComponent)
  },
  {
    path: 'notas/nova',
    loadComponent: () =>
      import('./features/notas-fiscais/nota-form/nota-form.component').then(m => m.NotaFormComponent)
  },
  {
    path: 'notas/:id',
    loadComponent: () =>
      import('./features/notas-fiscais/nota-detail/nota-detail.component').then(m => m.NotaDetailComponent)
  },
  { path: '**', redirectTo: '' }
];
