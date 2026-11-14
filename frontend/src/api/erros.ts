import axios from 'axios';
import type { ProblemDetails } from '../types/problema';

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null;
}

function formatarErrosDeCampo(errors: Record<string, string[]>): string {
  return Object.values(errors)
    .flat()
    .filter((msg) => msg.trim().length > 0)
    .join(' ');
}

/**
 * Converte um erro desconhecido (Axios ou outro) em mensagem amigável
 * a partir do ProblemDetails da API (400, 404, 409) ou mensagem genérica.
 */
export function mensagemDeErro(erro: unknown): string {
  if (axios.isAxiosError(erro)) {
    if (!erro.response) {
      return 'Não foi possível conectar à API. Verifique se ela está em execução.';
    }

    const status = erro.response.status;
    const data: unknown = erro.response.data;
    const problem = isProblemDetails(data) ? data : undefined;
    const detail = typeof problem?.detail === 'string' ? problem.detail.trim() : '';
    const errosCampo =
      problem?.errors && typeof problem.errors === 'object'
        ? formatarErrosDeCampo(problem.errors)
        : '';

    if (status === 400) {
      const partes = [detail || 'Dados inválidos.', errosCampo].filter(Boolean);
      return partes.join(' ');
    }

    if (status === 404) {
      return detail || 'Recurso não encontrado.';
    }

    if (status === 409) {
      return detail || 'Conflito: estoque insuficiente.';
    }

    if (status >= 500) {
      return 'Erro inesperado no servidor. Tente novamente mais tarde.';
    }

    return detail || `Erro na requisição (${status}).`;
  }

  if (erro instanceof Error && erro.message) {
    return erro.message;
  }

  return 'Ocorreu um erro inesperado.';
}
