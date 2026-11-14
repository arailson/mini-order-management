import type { CriarPedidoRequest, Pedido, PedidoListItem } from '../types/pedido';
import type { ConsultaPedidosFiltro, PagedResult } from '../types/paginacao';
import { http } from './http';

export async function listarPedidos(
  filtro: ConsultaPedidosFiltro,
  signal?: AbortSignal,
): Promise<PagedResult<PedidoListItem>> {
  const { data } = await http.get<PagedResult<PedidoListItem>>('/pedidos', {
    params: {
      clienteId: filtro.clienteId,
      dataInicio: filtro.dataInicio,
      dataFim: filtro.dataFim,
      page: filtro.page,
      pageSize: filtro.pageSize,
    },
    signal,
  });
  return data;
}

export async function criarPedido(request: CriarPedidoRequest): Promise<Pedido> {
  const { data } = await http.post<Pedido>('/pedidos', request);
  return data;
}
