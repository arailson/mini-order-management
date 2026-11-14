import type { Produto } from '../types/produto';
import { http } from './http';

export async function listarProdutos(signal?: AbortSignal): Promise<Produto[]> {
  const { data } = await http.get<Produto[]>('/produtos', { signal });
  return data;
}
