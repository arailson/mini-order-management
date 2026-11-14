import { useEffect, useState } from 'react';
import { listarPedidos } from '../api/pedidosApi';
import { mensagemDeErro } from '../api/erros';
import type { ConsultaPedidosFiltro, PagedResult } from '../types/paginacao';
import type { PedidoListItem } from '../types/pedido';

const PAGE_SIZE_PADRAO = 10;

export interface FiltrosPedidos {
  clienteId?: number;
  dataInicio?: string;
  dataFim?: string;
}

interface UsePedidosResultado {
  resultado: PagedResult<PedidoListItem> | null;
  carregando: boolean;
  erro: string | null;
  page: number;
  filtros: FiltrosPedidos;
  aplicarFiltros: (filtros: FiltrosPedidos) => void;
  irParaPagina: (page: number) => void;
  recarregar: () => void;
}

export function usePedidos(): UsePedidosResultado {
  const [filtros, setFiltros] = useState<FiltrosPedidos>({});
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const [resultado, setResultado] = useState<PagedResult<PedidoListItem> | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    void (async () => {
      // Adia o setState para depois do efeito (evita aviso do oxlint).
      await Promise.resolve();
      if (controller.signal.aborted) return;

      setCarregando(true);
      setErro(null);

      const consulta: ConsultaPedidosFiltro = {
        clienteId: filtros.clienteId,
        dataInicio: filtros.dataInicio || undefined,
        dataFim: filtros.dataFim || undefined,
        page,
        pageSize: PAGE_SIZE_PADRAO,
      };

      try {
        const data = await listarPedidos(consulta, controller.signal);
        if (controller.signal.aborted) return;
        setResultado(data);
      } catch (err: unknown) {
        if (controller.signal.aborted) return;
        setErro(mensagemDeErro(err));
        setResultado(null);
      } finally {
        if (!controller.signal.aborted) setCarregando(false);
      }
    })();

    return () => controller.abort();
  }, [filtros, page, refreshKey]);

  return {
    resultado,
    carregando,
    erro,
    page,
    filtros,
    aplicarFiltros: (novos) => {
      setFiltros(novos);
      setPage(1);
    },
    irParaPagina: (nova) => setPage(nova),
    recarregar: () => {
      setPage(1);
      setRefreshKey((k) => k + 1);
    },
  };
}
