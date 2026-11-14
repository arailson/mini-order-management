export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface ConsultaPedidosFiltro {
  clienteId?: number;
  dataInicio?: string;
  dataFim?: string;
  page?: number;
  pageSize?: number;
}
