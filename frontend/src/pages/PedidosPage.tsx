import { useState } from 'react';
import { EstadoMensagem } from '../components/EstadoMensagem';
import { FiltrosPedidos } from '../components/FiltrosPedidos';
import { ModalCriarPedido } from '../components/ModalCriarPedido';
import { Paginacao } from '../components/Paginacao';
import { TabelaPedidos } from '../components/TabelaPedidos';
import { useCatalogo } from '../hooks/useCatalogo';
import { usePedidos } from '../hooks/usePedidos';

export function PedidosPage() {
  const catalogo = useCatalogo();
  const pedidos = usePedidos();
  const [modalAberto, setModalAberto] = useState(false);
  const [modalKey, setModalKey] = useState(0);
  const [sucesso, setSucesso] = useState<string | null>(null);

  function abrirModal() {
    setSucesso(null);
    catalogo.recarregar();
    setModalKey((k) => k + 1);
    setModalAberto(true);
  }

  function aoCriarPedido(pedidoId: number) {
    setModalAberto(false);
    setSucesso(`Pedido #${pedidoId} criado com sucesso.`);
    pedidos.recarregar();
    catalogo.recarregar();
  }

  const itens = pedidos.resultado?.items ?? [];
  const semResultados =
    !pedidos.carregando && !pedidos.erro && pedidos.resultado !== null && itens.length === 0;

  return (
    <div className="pagina">
      <header className="pagina__cabecalho">
        <div>
          <p className="pagina__marca">Mini Gestão de Pedidos</p>
          <h1>Pedidos</h1>
          <p className="pagina__subtitulo">Consulte, filtre e crie pedidos.</p>
        </div>
        <button
          type="button"
          className="btn"
          onClick={abrirModal}
          disabled={catalogo.carregando && catalogo.clientes.length === 0}
        >
          Novo pedido
        </button>
      </header>

      {sucesso ? (
        <EstadoMensagem tipo="sucesso" mensagem={sucesso} />
      ) : null}

      {catalogo.erro ? (
        <EstadoMensagem
          tipo="erro"
          mensagem={`Não foi possível carregar clientes/produtos: ${catalogo.erro}`}
          acaoLabel="Tentar novamente"
          onAcao={catalogo.recarregar}
        />
      ) : null}

      <FiltrosPedidos
        clientes={catalogo.clientes}
        valorAtual={pedidos.filtros}
        onAplicar={pedidos.aplicarFiltros}
        desabilitado={pedidos.carregando}
      />

      {pedidos.carregando ? (
        <EstadoMensagem tipo="loading" mensagem="Carregando pedidos…" />
      ) : null}

      {pedidos.erro ? (
        <EstadoMensagem
          tipo="erro"
          mensagem={pedidos.erro}
          acaoLabel="Tentar novamente"
          onAcao={pedidos.recarregar}
        />
      ) : null}

      {semResultados ? (
        <EstadoMensagem tipo="vazio" mensagem="Nenhum pedido encontrado para os filtros informados." />
      ) : null}

      {!pedidos.carregando && !pedidos.erro && itens.length > 0 ? (
        <>
          <TabelaPedidos pedidos={itens} />
          <Paginacao
            page={pedidos.resultado?.page ?? pedidos.page}
            totalPages={pedidos.resultado?.totalPages ?? 0}
            totalItems={pedidos.resultado?.totalItems ?? 0}
            onMudar={pedidos.irParaPagina}
            desabilitado={pedidos.carregando}
          />
        </>
      ) : null}

      <ModalCriarPedido
        key={modalKey}
        aberto={modalAberto}
        clientes={catalogo.clientes}
        produtos={catalogo.produtos}
        onFechar={() => setModalAberto(false)}
        onCriado={aoCriarPedido}
      />
    </div>
  );
}
