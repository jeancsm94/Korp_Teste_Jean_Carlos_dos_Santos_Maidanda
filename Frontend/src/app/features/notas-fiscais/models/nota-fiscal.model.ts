export type NotaFiscalStatus = 'Aberta' | 'Fechada';

export interface ItemNota {
  id?: number;
  productId: number;
  productCode?: string;
  productDescription?: string;
  quantity: number;
}

export interface NotaFiscal {
  id: number;
  number: number;
  status: NotaFiscalStatus;
  createdAt: string;
  closedAt?: string | null;
  items: ItemNota[];
}

export interface CreateNotaFiscalItemInput {
  productId: number;
  quantity: number;
}

export interface CreateNotaFiscalInput {
  items: CreateNotaFiscalItemInput[];
}
