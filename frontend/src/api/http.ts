import axios from 'axios';

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

/** Instância única do Axios para toda a aplicação. */
export const http = axios.create({
  baseURL,
  headers: { Accept: 'application/json' },
});
