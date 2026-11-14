import type { PedidoListItem } from '../types/pedido';
import { formatarDataHora, formatarMoeda } from '../utils/formatacao';

interface TabelaPedidosProps {
  pedidos: PedidoListItem[];
}

export function TabelaPedidos({ pedidos }: TabelaPedidosProps) {
  return (
    <div className="tabela-wrap">
      <table className="tabela">
        <thead>
          <tr>
            <th scope="col">ID</th>
            <th scope="col">Cliente</th>
            <th scope="col">Data</th>
            <th scope="col">Valor total</th>
          </tr>
        </thead>
        <tbody>
          {pedidos.map((p) => (
            <tr key={p.id}>
              <td>{p.id}</td>
              <td>{p.clienteNome}</td>
              <td>{formatarDataHora(p.dataPedido)}</td>
              <td className="tabela__numero">{formatarMoeda(p.valorTotal)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
