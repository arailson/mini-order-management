import { useState, type FormEvent } from 'react';
import type { Cliente } from '../types/cliente';
import type { FiltrosPedidos as Filtros } from '../hooks/usePedidos';

interface FiltrosPedidosProps {
  clientes: Cliente[];
  valorAtual: Filtros;
  onAplicar: (filtros: Filtros) => void;
  desabilitado?: boolean;
}

export function FiltrosPedidos({
  clientes,
  valorAtual,
  onAplicar,
  desabilitado = false,
}: FiltrosPedidosProps) {
  const [clienteId, setClienteId] = useState(valorAtual.clienteId?.toString() ?? '');
  const [dataInicio, setDataInicio] = useState(valorAtual.dataInicio ?? '');
  const [dataFim, setDataFim] = useState(valorAtual.dataFim ?? '');
  const [erroLocal, setErroLocal] = useState<string | null>(null);

  function aplicar(event: FormEvent) {
    event.preventDefault();
    if (dataInicio && dataFim && dataInicio > dataFim) {
      setErroLocal('A data início não pode ser posterior à data fim.');
      return;
    }
    setErroLocal(null);
    onAplicar({
      clienteId: clienteId ? Number(clienteId) : undefined,
      dataInicio: dataInicio || undefined,
      dataFim: dataFim || undefined,
    });
  }

  function limpar() {
    setClienteId('');
    setDataInicio('');
    setDataFim('');
    setErroLocal(null);
    onAplicar({});
  }

  return (
    <form className="filtros" onSubmit={aplicar}>
      <div className="filtros__campo">
        <label htmlFor="filtro-cliente">Cliente</label>
        <select
          id="filtro-cliente"
          value={clienteId}
          disabled={desabilitado}
          onChange={(e) => setClienteId(e.target.value)}
        >
          <option value="">Todos</option>
          {clientes.map((c) => (
            <option key={c.id} value={c.id}>
              {c.nome}
            </option>
          ))}
        </select>
      </div>

      <div className="filtros__campo">
        <label htmlFor="filtro-inicio">Data início</label>
        <input
          id="filtro-inicio"
          type="date"
          value={dataInicio}
          disabled={desabilitado}
          onChange={(e) => setDataInicio(e.target.value)}
        />
      </div>

      <div className="filtros__campo">
        <label htmlFor="filtro-fim">Data fim</label>
        <input
          id="filtro-fim"
          type="date"
          value={dataFim}
          disabled={desabilitado}
          onChange={(e) => setDataFim(e.target.value)}
        />
      </div>

      <div className="filtros__acoes">
        <button type="submit" className="btn" disabled={desabilitado}>
          Filtrar
        </button>
        <button type="button" className="btn btn--secundario" disabled={desabilitado} onClick={limpar}>
          Limpar
        </button>
      </div>

      {erroLocal ? (
        <p className="filtros__erro" role="alert">
          {erroLocal}
        </p>
      ) : null}
    </form>
  );
}
