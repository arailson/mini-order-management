interface EstadoMensagemProps {
  tipo: 'loading' | 'erro' | 'vazio' | 'sucesso';
  mensagem: string;
  acaoLabel?: string;
  onAcao?: () => void;
}

export function EstadoMensagem({ tipo, mensagem, acaoLabel, onAcao }: EstadoMensagemProps) {
  return (
    <div className={`estado estado--${tipo}`} role={tipo === 'erro' ? 'alert' : 'status'}>
      <p>{mensagem}</p>
      {acaoLabel && onAcao ? (
        <button type="button" className="btn btn--secundario" onClick={onAcao}>
          {acaoLabel}
        </button>
      ) : null}
    </div>
  );
}
