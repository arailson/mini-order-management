import { useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { criarPedido } from '../api/pedidosApi';
import { mensagemDeErro } from '../api/erros';
import type { Cliente } from '../types/cliente';
import type { Produto } from '../types/produto';
import { formatarMoeda } from '../utils/formatacao';

interface LinhaItem {
  key: string;
  produtoId: string;
  quantidade: string;
}

interface ModalCriarPedidoProps {
  aberto: boolean;
  clientes: Cliente[];
  produtos: Produto[];
  onFechar: () => void;
  onCriado: (pedidoId: number) => void;
}

function novaLinha(): LinhaItem {
  return { key: crypto.randomUUID(), produtoId: '', quantidade: '1' };
}

export function ModalCriarPedido({
  aberto,
  clientes,
  produtos,
  onFechar,
  onCriado,
}: ModalCriarPedidoProps) {
  const tituloId = useId();
  const primeiroFoco = useRef<HTMLSelectElement>(null);
  const [clienteId, setClienteId] = useState('');
  const [itens, setItens] = useState<LinhaItem[]>(() => [novaLinha()]);
  const [erroFormulario, setErroFormulario] = useState<string | null>(null);
  const [erroApi, setErroApi] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    if (!aberto) return;
    const timer = window.setTimeout(() => primeiroFoco.current?.focus(), 0);
    return () => window.clearTimeout(timer);
  }, [aberto]);

  useEffect(() => {
    if (!aberto) return;

    function onKey(event: KeyboardEvent) {
      if (event.key === 'Escape' && !enviando) onFechar();
    }
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [aberto, onFechar, enviando]);

  if (!aberto) return null;

  const mapaProdutos = new Map(produtos.map((p) => [p.id, p]));

  function produtoJaEscolhido(produtoId: string, ignorarKey: string): boolean {
    return itens.some((i) => i.key !== ignorarKey && i.produtoId === produtoId && produtoId !== '');
  }

  function atualizarLinha(key: string, patch: Partial<LinhaItem>) {
    setItens((atual) => atual.map((linha) => (linha.key === key ? { ...linha, ...patch } : linha)));
  }

  function removerLinha(key: string) {
    setItens((atual) => (atual.length <= 1 ? atual : atual.filter((l) => l.key !== key)));
  }

  const subtotais = itens.map((linha) => {
    const id = Number(linha.produtoId);
    const produto = Number.isFinite(id) ? mapaProdutos.get(id) : undefined;
    const qtd = Number(linha.quantidade);
    if (!produto || !Number.isInteger(qtd) || qtd <= 0) return 0;
    return produto.preco * qtd;
  });
  const totalEstimado = subtotais.reduce((acc, v) => acc + v, 0);

  async function confirmar(event: FormEvent) {
    event.preventDefault();
    setErroFormulario(null);
    setErroApi(null);

    if (!clienteId) {
      setErroFormulario('Selecione um cliente.');
      return;
    }

    const itensValidos: { produtoId: number; quantidade: number }[] = [];
    const idsVistos = new Set<number>();

    for (const linha of itens) {
      if (!linha.produtoId) {
        setErroFormulario('Selecione um produto em cada linha.');
        return;
      }
      const produtoId = Number(linha.produtoId);
      const quantidade = Number(linha.quantidade);
      if (!Number.isInteger(quantidade) || quantidade <= 0) {
        setErroFormulario('A quantidade deve ser um inteiro maior que zero.');
        return;
      }
      if (idsVistos.has(produtoId)) {
        setErroFormulario('Não é permitido repetir o mesmo produto nas linhas.');
        return;
      }
      idsVistos.add(produtoId);
      itensValidos.push({ produtoId, quantidade });
    }

    if (itensValidos.length === 0) {
      setErroFormulario('Inclua ao menos um item no pedido.');
      return;
    }

    setEnviando(true);
    try {
      const pedido = await criarPedido({
        clienteId: Number(clienteId),
        itens: itensValidos,
      });
      onCriado(pedido.id);
    } catch (err: unknown) {
      setErroApi(mensagemDeErro(err));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="modal-backdrop" onClick={() => !enviando && onFechar()}>
      <div
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby={tituloId}
        onClick={(e) => e.stopPropagation()}
      >
        <header className="modal__cabecalho">
          <h2 id={tituloId}>Novo pedido</h2>
          <button
            type="button"
            className="btn btn--fantasma"
            aria-label="Fechar"
            disabled={enviando}
            onClick={onFechar}
          >
            ×
          </button>
        </header>

        <form className="modal__corpo" onSubmit={confirmar}>
          <div className="campo">
            <label htmlFor="pedido-cliente">Cliente</label>
            <select
              id="pedido-cliente"
              ref={primeiroFoco}
              value={clienteId}
              disabled={enviando}
              onChange={(e) => setClienteId(e.target.value)}
              required
            >
              <option value="">Selecione…</option>
              {clientes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nome}
                </option>
              ))}
            </select>
          </div>

          <fieldset className="itens" disabled={enviando}>
            <legend>Itens</legend>
            {itens.map((linha, indice) => {
              const produtoId = Number(linha.produtoId);
              const produto = Number.isFinite(produtoId) ? mapaProdutos.get(produtoId) : undefined;
              return (
                <div className="itens__linha" key={linha.key}>
                  <div className="campo">
                    <label htmlFor={`produto-${linha.key}`}>Produto</label>
                    <select
                      id={`produto-${linha.key}`}
                      value={linha.produtoId}
                      onChange={(e) => {
                        const valor = e.target.value;
                        if (produtoJaEscolhido(valor, linha.key)) {
                          setErroFormulario('Este produto já foi adicionado.');
                          return;
                        }
                        setErroFormulario(null);
                        atualizarLinha(linha.key, { produtoId: valor });
                      }}
                    >
                      <option value="">Selecione…</option>
                      {produtos.map((p) => (
                        <option
                          key={p.id}
                          value={p.id}
                          disabled={produtoJaEscolhido(String(p.id), linha.key)}
                        >
                          {p.nome} — {formatarMoeda(p.preco)} (estoque: {p.estoque})
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="campo campo--curto">
                    <label htmlFor={`qtd-${linha.key}`}>Qtd.</label>
                    <input
                      id={`qtd-${linha.key}`}
                      type="number"
                      min={1}
                      step={1}
                      value={linha.quantidade}
                      onChange={(e) => atualizarLinha(linha.key, { quantidade: e.target.value })}
                    />
                  </div>

                  <div className="itens__subtotal" aria-live="polite">
                    <span className="itens__rotulo">Subtotal (estimativa)</span>
                    <strong>{formatarMoeda(subtotais[indice] ?? 0)}</strong>
                    {produto ? (
                      <span className="itens__hint">Unit.: {formatarMoeda(produto.preco)}</span>
                    ) : null}
                  </div>

                  <button
                    type="button"
                    className="btn btn--secundario"
                    onClick={() => removerLinha(linha.key)}
                    disabled={itens.length <= 1}
                    aria-label="Remover item"
                  >
                    Remover
                  </button>
                </div>
              );
            })}

            <button
              type="button"
              className="btn btn--secundario"
              onClick={() => setItens((atual) => [...atual, novaLinha()])}
            >
              Adicionar item
            </button>
          </fieldset>

          <p className="total-estimado">
            Total estimado (apenas informativo): <strong>{formatarMoeda(totalEstimado)}</strong>
          </p>
          <p className="aviso-backend">
            O preço definitivo e a validação de estoque são definidos pelo backend.
          </p>

          {erroFormulario ? (
            <p className="banner banner--aviso" role="alert">
              {erroFormulario}
            </p>
          ) : null}
          {erroApi ? (
            <p className="banner banner--erro" role="alert">
              {erroApi}
            </p>
          ) : null}

          <footer className="modal__rodape">
            <button type="button" className="btn btn--secundario" disabled={enviando} onClick={onFechar}>
              Cancelar
            </button>
            <button type="submit" className="btn" disabled={enviando}>
              {enviando ? 'Salvando…' : 'Confirmar pedido'}
            </button>
          </footer>
        </form>
      </div>
    </div>
  );
}
