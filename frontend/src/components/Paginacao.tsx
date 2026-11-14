interface PaginacaoProps {
  page: number;
  totalPages: number;
  totalItems: number;
  onMudar: (page: number) => void;
  desabilitado?: boolean;
}

export function Paginacao({
  page,
  totalPages,
  totalItems,
  onMudar,
  desabilitado = false,
}: PaginacaoProps) {
  if (totalPages <= 0) return null;

  return (
    <nav className="paginacao" aria-label="Paginação de pedidos">
      <button
        type="button"
        className="btn btn--secundario"
        disabled={desabilitado || page <= 1}
        onClick={() => onMudar(page - 1)}
      >
        Anterior
      </button>
      <span className="paginacao__info">
        Página {page} de {totalPages} ({totalItems} {totalItems === 1 ? 'pedido' : 'pedidos'})
      </span>
      <button
        type="button"
        className="btn btn--secundario"
        disabled={desabilitado || page >= totalPages}
        onClick={() => onMudar(page + 1)}
      >
        Próxima
      </button>
    </nav>
  );
}
