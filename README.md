# Mini Gestão de Pedidos

Aplicação web para gerenciamento de pedidos: cadastro, consulta com filtros e paginação, e relatório dos cinco clientes com maior volume de compras.

Projeto desenvolvido com abordagem **AI First** usando o Cursor. Especificação: [docs/spec.md](docs/spec.md). Plano: [docs/plan.md](docs/plan.md).

> Status: **Fases 0–7 concluídas** (fundação, banco, backend, frontend, testes, Docker, configuração AI First no Cursor, otimização da query e documentação). Pendência do desenvolvedor: vídeo de demonstração (até 10 minutos).

## Índice

1. [Stack](#stack)
2. [Estrutura do repositório](#estrutura-do-repositório)
3. [Pré-requisitos e configuração](#pré-requisitos-e-configuração)
4. [Como executar](#como-executar)
5. [Testes](#testes)
6. [Endpoints da API](#endpoints-da-api)
7. [Decisões arquiteturais e técnicas](#decisões-arquiteturais-e-técnicas)
8. [Otimização da query](#otimização-da-query)
9. [Configuração AI First no Cursor](#configuração-ai-first-no-cursor)
10. [Uso do Cursor neste projeto](#uso-do-cursor-neste-projeto)
11. [Limitações conhecidas](#limitações-conhecidas)

## Stack

- **Backend:** .NET 8 Web API, C#, Entity Framework Core, SQL Server, xUnit
- **Frontend:** React 19, TypeScript (strict), Vite, Axios
- **Infraestrutura:** Docker Compose, SQL Server 2022, Playwright (E2E)

## Estrutura do repositório

```text
.
├── docs/                 # spec.md, plan.md, perf/ (investigação da query)
├── .cursor/              # rules, hooks, skill, MCP
├── database/             # scripts SQL 01–03 + optional/
├── backend/              # solution .NET (Domain, Application, Infrastructure, API, tests)
├── frontend/             # React + TypeScript + Vite
├── e2e/                  # Playwright
├── docker-compose.yml
├── .env.example
└── README.md
```

Referências do backend: `Application` → `Domain`; `Infrastructure` → `Application`/`Domain`; `API` → `Application`/`Infrastructure`; testes → camadas correspondentes.

## Pré-requisitos e configuração

- [.NET SDK](https://dotnet.microsoft.com/download) (projetos alvo `net8.0`; em Windows com runtime mais novo, o perfil `http` define `DOTNET_ROLL_FORWARD=Major`)
- [Node.js](https://nodejs.org/) ≥ 20.19 (ou ≥ 22.12) e npm
- [Docker](https://www.docker.com/) e Docker Compose
- Git

Para subir com Docker Compose **não é necessário criar o `.env`**: o `docker-compose.yml` traz valores padrão de desenvolvimento (senha do SQL Server, portas). Copie o exemplo apenas para personalizar valores ou para rodar API/E2E fora do Docker:

```bash
cp .env.example .env   # opcional; ajuste MSSQL_SA_PASSWORD e demais valores; .env não é versionado
```

## Como executar

### Opção A — Docker Compose (recomendado)

```powershell
docker compose up -d --build   # (ou apenas: docker compose up) - sem .env e sem passos manuais
```

Depois de subir, abra o frontend e a API nas URLs abaixo. Os scripts SQL (criação, índices e seed) são aplicados automaticamente.

| Serviço | URL padrão | Observação |
| ------- | ---------- | ---------- |
| Frontend | <http://localhost:3000> | Nginx com o build Vite |
| API / Swagger | <http://localhost:5000/swagger> | Porta interna 8080 |
| SQL Server | `127.0.0.1:1433` | Só em loopback |

Ordem: `sqlserver` (healthy) → `db-init` (scripts 01–03) → `api` (healthy) → `frontend`.

```powershell
docker compose down        # para containers (mantém volume)
docker compose down -v     # também apaga dados do SQL Server
```

Avisos: pare a API local na porta 5000 antes do Compose (ou mude `API_PORT`); `VITE_API_URL` é embutida no build — mude e faça rebuild do frontend; no macOS a porta 5000 pode estar com AirPlay; Apple Silicon pode precisar de `platform: linux/amd64` no serviço `sqlserver`.

### Opção B — Desenvolvimento local

**1. Banco** (só o SQL Server do Compose):

```powershell
docker compose up -d sqlserver
```

Os scripts 01–03 são aplicados pelo `db-init` no stack completo. Manualmente (idempotentes; `-f 65001` por causa dos acentos):

```powershell
foreach ($f in '01-create-tables','02-create-indexes','03-seed-data') {
  docker compose cp "database/$f.sql" sqlserver:/tmp/$f.sql
  docker compose exec -e SQLCMDPASSWORD=$env:MSSQL_SA_PASSWORD sqlserver `
    /opt/mssql-tools18/bin/sqlcmd -C -b -f 65001 -S localhost -U sa -i "/tmp/$f.sql"
}
```

**2. API** (a partir da raiz; o `dotnet run` **não** lê o `.env` automaticamente):

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=MiniGestaoPedidos;User Id=sa;Password=<MSSQL_SA_PASSWORD do .env>;TrustServerCertificate=True"
dotnet run --project backend/src/API --launch-profile http
```

Swagger: <http://localhost:5000/swagger>.

**3. Frontend:**

```powershell
copy frontend\.env.example frontend\.env   # VITE_API_URL=http://localhost:5000
npm install --prefix frontend
npm run dev --prefix frontend
```

App: <http://localhost:5173>.

## Testes

### Unitários e integração (.NET)

```powershell
dotnet test backend/tests/UnitTests/UnitTests.csproj
dotnet test backend/tests/IntegrationTests/IntegrationTests.csproj   # exige Docker
dotnet test backend/MiniGestaoPedidos.sln
dotnet test backend/MiniGestaoPedidos.sln --filter "Category!=Integration"
```

- Unitários: Domain + Application (NSubstitute / `FakeUnitOfWork`), sem banco.
- Integração: `WebApplicationFactory` + SQL Server real em Docker; scripts 01–03 no setup; reset do seed por teste. O SQL Server de teste sobe em container próprio (porta aleatória), criado pela fixture via CLI do Docker.
- Os projetos de teste usam `net8.0` por padrão. **Windows com apenas o runtime ASP.NET Core 11 instalado:** o TestHost 8 com roll-forward quebra a serialização JSON; nesse caso, rode a integração com o TFM `net11.0` (a API continua `net8.0`):

  ```powershell
  $env:IntegrationTestsTfm = 'net11.0'
  dotnet test backend/tests/IntegrationTests/IntegrationTests.csproj
  # ou: dotnet test backend/tests/IntegrationTests/IntegrationTests.csproj -p:IntegrationTestsTfm=net11.0
  ```

### E2E (Playwright)

```powershell
docker compose up -d
npm install --prefix e2e
npm run install:browsers --prefix e2e
npm test --prefix e2e
```

O Playwright sobe API e frontend se necessário. Contra o Compose: `$env:E2E_BASE_URL="http://localhost:3000"; npm test --prefix e2e`.

Qualidade do frontend: `npm run lint --prefix frontend` e `npm run build --prefix frontend`.

## Endpoints da API

| Método | Rota | Descrição | Respostas |
| ------ | ---- | --------- | --------- |
| POST | `/pedidos` | Cria pedido (transação: itens, total, baixa de estoque) | 201, 400, 404, 409, 500 |
| GET | `/pedidos` | Lista com `clienteId`, `dataInicio`, `dataFim`, `page`, `pageSize` | 200, 400 |
| GET | `/relatorios/top-clientes` | Top 5 por valor (`dataInicio` e `dataFim` obrigatórios) | 200, 400 |
| GET | `/clientes` | Apoio ao frontend (aprovado fora da spec) | 200 |
| GET | `/produtos` | Apoio ao frontend (aprovado fora da spec) | 200 |

- Erros em `application/problem+json` (RFC 7807). Estoque insuficiente (409) inclui `produtoId`, `solicitado` e `disponivel`.
- Itens do mesmo produto no mesmo pedido são consolidados. Preço e total são calculados no backend.
- `dataFim` é inclusiva (dia inteiro); `pageSize` padrão 10, máximo 100.

## Decisões arquiteturais e técnicas

| Decisão | Motivo |
| ------- | ------ |
| Arquitetura em camadas (Domain / Application / Infrastructure / API) | Separar regras de negócio, casos de uso e persistência |
| Scripts SQL como fonte de verdade (sem migrations EF) | Esquema versionado e reproduzível via `database/` |
| Backend autoridade de preço, total e estoque | Frontend só exibe; evita inconsistência e fraude de preço |
| `IUnitOfWork.ExecuteInTransactionAsync` + `UPDLOCK`/`ROWLOCK` no estoque | Criação atômica do pedido e mitigação de concorrência |
| `ProblemDetails` + middleware de exceções | Erros HTTP uniformes (404/409/400/500) |
| Filtro de período com limite superior exclusivo (`< dataFim + 1 dia`) | Inclui o dia final sem `YEAR()`/`CAST` na coluna |
| Ranking agrupado por `Cliente.Id` (não só por nome) | Homônimos do seed (João Silva, Maria Souza) não se fundem |
| CORS policy "Frontend" | Origens configuráveis (`Cors:AllowedOrigins`) |
| Integração com SQL Server real (não InMemory) | Valida índices, constraints e scripts oficiais |
| Compose com `db-init` one-shot + healthchecks | API só sobe após banco populado |
| Sem `container_name` fixo | Permite projetos Compose paralelos (`-p`) |

## Otimização da query

Investigação completa: [docs/perf/resumo.md](docs/perf/resumo.md). Scripts: [database/optional/perf/](database/optional/perf/).

### Query original (enunciado / e-mail do teste)

```sql
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
```

### Query otimizada (spec, seção 7)

```sql
SELECT TOP 5
    c.Id AS ClienteId,
    c.Nome,
    SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
INNER JOIN Pedidos p ON p.ClienteId = c.Id
INNER JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE p.DataPedido >= '20260101'
  AND p.DataPedido < '20270101'
GROUP BY c.Id, c.Nome
ORDER BY Total DESC;
```

### Problemas da original e correções

| Problema | Efeito | Correção |
| -------- | ------ | -------- |
| `YEAR(p.DataPedido)` | Impede seek eficiente; força `datepart` por linha | Range sargável `>=` / `<` |
| `LEFT JOIN` + `WHERE` em `Pedidos` | Elimina clientes sem pedido; outer join vira filtro | `INNER JOIN` explícito |
| `ORDER BY Total` (ASC) | Ranking invertido | `ORDER BY Total DESC` |
| `GROUP BY c.Nome` | Funde homônimos | `GROUP BY c.Id, c.Nome` |
| Sem `TOP 5` | Retorna todos os grupos | `SELECT TOP 5` |
| Sem `ClienteId` no SELECT | Resultado não identifica o cliente | Projetar `c.Id` |

### Metodologia

- Projeto Compose isolado (`docker compose -p perf`, porta 1434), banco descartável com flag `PerfExperiment`.
- Corretude validada no **seed** (40 pedidos): otimizada retorna clientes **1, 2, 3, 7, 4**; original funde João Silva/Maria Souza e ordena de forma crescente.
- Volume sintético: **5.012** clientes, **500.040** pedidos, **1.500.072** itens (~67% dos pedidos em 2026).
- `SET STATISTICS IO, TIME ON`, cache frio, `OPTION (MAXDOP 1)`.
- Planos estimados (`SHOWPLAN_TEXT`) e totais via DMV.

### Resultados (volume)

| Variante | Logical reads (Pedidos / Itens / Clientes) | CPU (ms) | Elapsed (ms) |
| -------- | ------------------------------------------ | -------- | ------------ |
| Original | 2.111 / 6.328 / 77 | 270 | 268 |
| Intermediária (só range no lugar de `YEAR`) | 2.111 / 6.328 / 77 | 229 | 232 |
| Otimizada | 2.111 / 6.328 / 496 | 201 | 201 |

DMV (leituras totais / worker µs): original **8.516 / 271.069**; otimizada **8.457 / 197.256** (~27% menos CPU).

**Plano estimado — original:** scans clusterizados + *Hash Left Outer Join* + `datepart(year)` + *Filter* + agregação por `Nome` + *Sort ASC*.

**Plano estimado — otimizada:** scan de `Pedidos` **com** predicado de range + *Inner Join* + agregação por `ClienteId` + *Sort DESC* + **Top 5** + *Seek* em `Clientes`.

Com ~67% das linhas no período, o otimizador ainda prefere *Clustered Index Scan* em `Pedidos` (o `IX_Pedidos_DataPedido` sozinho não evita o scan). Índices de cobertura experimentais reduziram leituras de `Pedidos` de 2.111 para 934; **não** foram aplicados em `02-create-indexes.sql` (recomendação opcional).

## Configuração AI First no Cursor

Toda a configuração fica em [.cursor/](.cursor/) (exceto segredos).

| Recurso | Arquivo | Para quê |
| ------- | ------- | -------- |
| Rules | `.cursor/rules/*.mdc` | Contexto persistente (arquitetura, escopo, nomenclatura) |
| Hook | `.cursor/hooks.json` + `validate-build.sh` | Valida build ao final da tarefa (`stop`) |
| Skill | `.cursor/skills/create-endpoint/SKILL.md` | Roteiro para novos endpoints |
| MCP | `.cursor/mcp.json` | SQL Server somente leitura (`mssql-read-only-mcp@1.0.2`) |

### Hook

No `stop`, se `backend/` ou `frontend/` mudaram, roda `dotnet build` / `npm run build`. Falha → `followup_message` (até `loop_limit: 3`). No Windows o `hooks.json` chama o Git Bash pelo caminho curto `C:\PROGRA~1\Git\bin\bash.exe` (o stub WSL do PATH não serve). Em Linux/macOS use `bash .cursor/hooks/validate-build.sh`.

### MCP SQL Server

1. Banco no ar com scripts 01–03.
2. Crie o usuário readonly (`database/optional/create-readonly-user.sql`).
3. Copie `.env.mcp.example` → `.env.mcp` e ative o servidor `sqlserver` em Cursor Settings > MCP.

Somente `SELECT`; login em `db_datareader`. Não use em produção.

## Uso do Cursor neste projeto

### Tarefas realizadas com apoio da IA

| Fase | O que foi feito |
| ---- | --------------- |
| 0 | Estrutura do monorepo, solution .NET, Vite, Docker Compose mínimo |
| 1 | Scripts SQL idempotentes, índices, seed com bordas e homônimos |
| 2 | API em camadas, pedidos, ranking, ProblemDetails, Swagger |
| 7 | Rules, hook de build, skill `create-endpoint`, MCP readonly |
| 3 | Frontend: listagem, filtros, modal de criação, Axios tipado |
| 4 | Unitários, integração com SQL Server, E2E Playwright |
| 5 | Dockerfiles multi-stage, Compose completo, `db-init` |
| 6 | Investigação da query, README e fechamento do plano |

### Resultados

- Builds e testes verdes (41 unitários + 23 integração + 1 E2E no fechamento das fases).
- Stack sobe com `docker compose up -d --build`.
- Query otimizada validada em corretude e medida com volume sintético.

### Correções necessárias (exemplos reais)

- **TypeScript “8”:** a especificação citava TS 8; a versão estável usada foi **7.0.2** (não existe TypeScript 8).
- **API local 500 / login failed / erro 4060:** `dotnet run` não carrega `.env`; foi preciso exportar `ConnectionStrings__DefaultConnection` e aplicar os scripts SQL.
- **Testcontainers + .NET 11:** Docker.DotNet falhou no named pipe no Windows; a fixture passou a usar a **CLI do Docker** (o pacote foi removido). Para o TestHost, `IntegrationTests` ganhou o override `IntegrationTestsTfm=net11.0` (padrão `net8.0`).
- **FluentAssertions 8.x:** licença comercial → permaneceu em **6.12.2**.
- **Hook CRLF:** script `.sh` quebrava no Bash; normalizado para LF + `.gitattributes`.
- **oxlint `set-state-in-effect`:** hooks/modal refatorados (async IIFE / remount por `key`).
- **Escopo:** pedido de “nome do pedido” na listagem esclarecido para produtos; `GET /clientes` e `/produtos` só após aprovação explícita.

### Reflexões

O Cursor acelerou a entrega faseada e manteve o alinhamento com a spec via rules e o plano em `docs/plan.md`. O ganho maior foi na geração de estrutura, testes e documentação. Os pontos que mais exigiram revisão humana foram ambiente Windows (.NET roll-forward, Docker named pipe, Git Bash no hook), decisões de licença/pacotes e o limite de escopo — a regra de “não implementar fora da spec sem aprovação” evitou desvios. Nada gerado pela IA foi aceito sem build e testes.

## Limitações conhecidas

- Sem autenticação/autorização (fora do escopo da spec).
- `GET /clientes` e `GET /produtos` são exceções aprovadas, não estão na spec original.
- Testes de integração usam fixture via CLI do Docker (não Testcontainers, que falhou no named pipe do Windows com .NET 11); em máquinas só com o runtime ASP.NET Core 11 é preciso o override `IntegrationTestsTfm=net11.0` (veja Testes).
- API local exige variável de ambiente de connection string (`dotnet run` não lê `.env`).
- O Compose usa uma senha padrão de desenvolvimento para o SQL Server (a mesma de `.env.example`) para subir sem `.env`; ela só deve ser usada neste contexto local/de avaliação, nunca em produção.
- Não há pipeline de GitHub Actions (diferencial não implementado).
- MCP depende de pacote de terceiros via `npx` (versão fixada). A seção de MCP descreve configuração e exemplo de uso, sem registro de uso real durante o desenvolvimento.
- O E2E cria um pedido para Pedro Santos no banco em uso e baixa estoque, então os totais do ranking mudam em relação ao seed (exemplo: após o E2E, Pedro Santos passa de 7.257,80 para 7.347,50). Antes de demonstrar valores do seed, use `docker compose down -v`.
- E2E cobre o fluxo principal de criar e listar pedido.
- Volume da investigação de performance é sintético e descartável; não faz parte do seed oficial.
- As medições de CPU e tempo da investigação são de execução única e indicativas (as leituras lógicas são praticamente iguais). O plano de execução real não foi arquivado: ficam apenas os planos estimados e o resumo do DMV em `docs/perf/`.
- Vídeo de demonstração (critério de aceite) é responsabilidade do desenvolvedor.
