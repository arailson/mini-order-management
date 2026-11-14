import type { Cliente } from '../types/cliente';
import { http } from './http';

export async function listarClientes(signal?: AbortSignal): Promise<Cliente[]> {
  const { data } = await http.get<Cliente[]>('/clientes', { signal });
  return data;
}
