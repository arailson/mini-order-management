# Scripts de experimento de performance

Uso **somente** em banco descartável. Não execute no SQL Server de desenvolvimento permanente.

## Ordem

1. Suba um SQL Server isolado (ex.: `docker compose -p perf` com `MSSQL_PORT=1434`) e aplique `01`–`03`.
2. `perf-00-marcar-experimento.sql` — marca `PerfExperiment=1`.
3. (Opcional) valide corretude no seed antes do volume.
4. `perf-01-gerar-volume.sql` — ~5k clientes, ~500k pedidos, ~1,5M itens. Aborta sem a marca ou se já houver > 1.000 pedidos.
5. `perf-02-comparar-queries.sql` / `perf-02b-medicao-io.sql` — original, intermediária e otimizada.
6. `perf-03-avaliar-indices-cobertura.sql` — cria índices temporários, mede e remove.
7. `perf-04-extrair-planos.sql` — planos estimados e resumo DMV.
8. `docker compose -p perf down -v`.

Resultados documentados em [`docs/perf/resumo.md`](../../../docs/perf/resumo.md).
