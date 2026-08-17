export interface Produto {
  id: number;
  code: string;
  description: string;
  balance: number;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateProdutoInput {
  code: string;
  description: string;
  initialBalance: number;
}

export interface UpdateProdutoInput {
  code: string;
  description: string;
  balance: number;
}
