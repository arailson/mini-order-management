export interface ItemPedidoRequest {
  produtoId: number;
  quantidade: number;
}

export interface CriarPedidoRequest {
  clienteId: number;
  itens: ItemPedidoRequest[];
}

export interface ItemPedido {
  produtoId: number;
  quantidade: number;
  precoUnitario: number;
  subtotal: number;
}

export interface Pedido {
  id: number;
  clienteId: number;
  dataPedido: string;
  valorTotal: number;
  itens: ItemPedido[];
}

export interface PedidoListItem {
  id: number;
  clienteId: number;
  clienteNome: string;
  dataPedido: string;
  valorTotal: number;
}
