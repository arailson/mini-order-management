# Resumo da investigação de performance (Fase 6)

Ambiente: SQL Server 2022 em projeto Compose isolado (`docker compose -p perf`, porta `1434`), banco descartável marcado com `PerfExperiment=1`. Medições com `SET STATISTICS IO, TIME ON`, `DBCC DROPCLEANBUFFERS`, `DBCC FREEPROCCACHE` e `OPTION (MAXDOP 1)`.

Scripts: [`database/optional/perf/`](../../database/optional/perf/).

## Volume

| Momento | Clientes | Produtos | Pedidos | ItensPedido |
| ------- | -------- | -------- | ------- | ----------- |
| Seed (corretude) | 12 | 10 | 40 | 72 |
| Após `perf-01` | 5.012 | 10 | 500.040 | 1.500.072 |

Pedidos em 2026 (range): **335.746** (~67% do total). Por isso o otimizador tende a preferir *Clustered Index Scan* em `Pedidos` mesmo com predicado sargável — o índice `IX_Pedidos_DataPedido` sozinho não reduz o trabalho o suficiente para compensar *lookups*.

## Corretude (somente seed)

### Query original (enunciado)

Ordem crescente, sem `ClienteId`, homônimos fundidos:

| Nome | Total |
| ---- | ----- |
| Rafael Costa | 1.229,30 |
| Fernanda Lima | 1.339,50 |
| Ana Oliveira | 2.468,40 |
| Pedro Santos | 7.257,80 |
| Carlos Pereira | 8.667,10 |
| Maria Souza | 10.815,80 (Ids 2+6) |
| João Silva | 15.406,10 (Ids 1+5) |

### Query otimizada (spec)

| ClienteId | Nome | Total |
| --------- | ---- | ----- |
| 1 | João Silva | 14.286,80 |
| 2 | Maria Souza | 10.486,30 |
| 3 | Carlos Pereira | 8.667,10 |
| 7 | Pedro Santos | 7.257,80 |
| 4 | Ana Oliveira | 2.468,40 |

Alinhado ao ranking esperado do seed (`03-seed-data.sql`).

## Medição com volume (`perf-02b`)

Leituras lógicas e tempo de CPU da agregação (cache frio; valores de `CPU time` / `elapsed time` da consulta):

| Variante | Pedidos (logical reads) | ItensPedido | Clientes | CPU (ms) | Elapsed (ms) |
| -------- | ----------------------- | ----------- | -------- | -------- | ------------ |
| Original (`YEAR` + `LEFT JOIN` + `GROUP BY Nome`) | 2.111 | 6.328 | 77 | 270 | 268 |
| Intermediária (range no lugar de `YEAR`) | 2.111 | 6.328 | 77 | 229 | 232 |
| Otimizada (spec: `INNER JOIN`, range, `GROUP BY Id`, `TOP 5`) | 2.111 | 6.328 | 496 | 201 | 201 |

Totais de leituras lógicas via DMV (`perf-04`): original **8.516**, otimizada **8.457**. Worker time: **271 ms** vs **197 ms**.

Observações:

- Trocar `YEAR()` por range já reduz CPU (~15%) mesmo sem mudar as leituras — o predicado deixa de calcular `datepart` em cada linha e o plano aplica o filtro diretamente no scan de `Pedidos`.
- A otimizada não reduz leituras de `Pedidos`/`ItensPedido` neste volume porque ainda faz *Clustered Index Scan* (seletividade ~67%). O ganho vem de `INNER JOIN`, agregação por `ClienteId`, `TOP 5` + sort descendente e seek em `Clientes`.
- O `LEFT JOIN` da original vira filtro efetivo de inner join por causa do `WHERE` em `p.DataPedido`; o plano estimado ainda mostra *Hash Match (Left Outer Join)* com filtro `datepart(year)=2026`.

## Planos estimados (trechos)

**Original:** `Clustered Index Scan` em Clientes/Pedidos/ItensPedido → *Hash Match (Left Outer Join)* → `Compute Scalar(datepart(year))` + *Filter* → *Hash Match Aggregate* por `Nome` → *Sort ASC*.

**Otimizada:** *Clustered Index Scan* em Pedidos **com** predicado de range → *Hash Match (Inner Join)* com ItensPedido → *Hash Match Aggregate* por `ClienteId` → *Sort DESC* → **Top (5)** → *Clustered Index Seek* em Clientes.

## Índices de cobertura (experimento, não aplicados ao script oficial)

Temporários em `perf-03` (removidos ao final):

- `IX_Pedidos_DataPedido_InclCliente` em `(DataPedido) INCLUDE (ClienteId)`
- `IX_ItensPedido_PedidoId_InclQtdPreco` em `(PedidoId) INCLUDE (Quantidade, PrecoUnitario)`

Medição da otimizada com esses índices (`MAXDOP 1`):

| Tabela | Logical reads (sem cobertura) | Com cobertura |
| ------ | ----------------------------- | ------------- |
| Pedidos | 2.111 | 934 |
| ItensPedido | 6.328 | 5.042 |
| Clientes | 496 | 519 |
| CPU / elapsed (ms) | 201 / 201 | 176 / 185 |

Ganho moderado em `Pedidos` (~56% menos leituras). Não alteramos [`database/02-create-indexes.sql`](../../database/02-create-indexes.sql); a recomendação fica opcional para cargas reais com menor seletividade no período.

## Índices oficiais presentes

- `PK_Clientes`, `PK_Pedidos`, `PK_ItensPedido`
- `IX_Pedidos_ClienteId`, `IX_Pedidos_DataPedido`
- `IX_ItensPedido_PedidoId`, `IX_ItensPedido_ProdutoId`

## Artefatos brutos

- `seed-correctness.out` — resultados no seed
- `perf-01-gerar-volume.out` — log da carga
- `perf-02b-medicao-metrics.txt` — mensagens de `STATISTICS IO/TIME` extraídas
- `perf-03-cobertura.out` — medição com índices temporários
- `perf-04-planos.txt` — planos estimados (texto) e resumo DMV
