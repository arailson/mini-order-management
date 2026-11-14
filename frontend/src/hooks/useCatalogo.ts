import { useEffect, useState } from 'react';
import { listarClientes } from '../api/clientesApi';
import { listarProdutos } from '../api/produtosApi';
import { mensagemDeErro } from '../api/erros';
import type { Cliente } from '../types/cliente';
import type { Produto } from '../types/produto';

interface UseCatalogoResultado {
  clientes: Cliente[];
  produtos: Produto[];
  carregando: boolean;
  erro: string | null;
  recarregar: () => void;
}

export function useCatalogo(): UseCatalogoResultado {
  const [clientes, setClientes] = useState<Cliente[]>([]);
  const [produtos, setProdutos] = useState<Produto[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  const [tick, setTick] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    void (async () => {
      await Promise.resolve();
      if (controller.signal.aborted) return;

      setCarregando(true);
      setErro(null);

      try {
        const [listaClientes, listaProdutos] = await Promise.all([
          listarClientes(controller.signal),
          listarProdutos(controller.signal),
        ]);
        if (controller.signal.aborted) return;
        setClientes(listaClientes);
        setProdutos(listaProdutos);
      } catch (err: unknown) {
        if (controller.signal.aborted) return;
        setErro(mensagemDeErro(err));
      } finally {
        if (!controller.signal.aborted) setCarregando(false);
      }
    })();

    return () => controller.abort();
  }, [tick]);

  return {
    clientes,
    produtos,
    carregando,
    erro,
    recarregar: () => setTick((t) => t + 1),
  };
}
