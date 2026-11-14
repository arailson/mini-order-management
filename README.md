# Mini Gestão de Pedidos

Aplicação web para gerenciamento de pedidos de clientes: cadastro de pedidos, consulta com filtros e paginação e relatório dos cinco clientes com maior volume de compras.

Projeto desenvolvido com abordagem **AI First** usando o Cursor. Veja a especificação em [docs/spec.md](docs/spec.md) e o plano de implementação em [docs/plan.md](docs/plan.md).

> Status: **Fases 0 (estrutura), 1 (banco de dados) e 2 (backend) concluídas**. As demais fases estão descritas no plano.

## Stack

- **Backend:** .NET 8 Web API, C#, Entity Framework Core, SQL Server, xUnit
- **Frontend:** React, TypeScript, Vite, Axios
- **Infraestrutura:** Docker Compose, SQL Server em container, Testcontainers

## Estrutura do repositório

```text
.
├── docs/                 # spec.md e plan.md
├── .cursor/              # rules, hooks, skills e MCP (Fase 7)
├── database/             # scripts SQL (Fase 1)
├── backend/
│   ├── MiniGestaoPedidos.sln
│   ├── src/
│   │   ├── API/              # controllers, middlewares, composição
│   │   ├── Application/      # casos de uso, DTOs, validações
│   │   ├── Domain/           # entidades e regras de negócio
│   │   └── Infrastructure/   # EF Core, repositórios
│   └── tests/
│       ├── UnitTests/
│       └── IntegrationTests/
├── frontend/             # React + TypeScript + Vite
├── .env.example
└── README.md
```

### Referências entre projetos do backend

- `Application` -> `Domain`
- `Infrastructure` -> `Application`, `Domain`
- `API` -> `Application`, `Infrastructure`
- `UnitTests` -> `Application`, `Domain`
- `IntegrationTests` -> `API`, `Application`, `Domain`, `Infrastructure`

## Pré-requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) (os projetos têm como alvo `net8.0`)
- [Node.js](https://nodejs.org/) e npm
- [Docker](https://www.docker.com/) e Docker Compose
- Git

## Configuração

```bash
cp .env.example .env   # ajuste os valores; o arquivo .env não é versionado
```

## Como executar (desenvolvimento)

### Banco de dados (SQL Server no Docker)

```powershell
docker compose up -d          # sobe o SQL Server 2022 (usa as variáveis do .env)
```

Execute os scripts de [database/](database/) em ordem (são idempotentes; usar `-f 65001` por causa dos acentos):

```powershell
foreach ($f in '01-create-tables','02-create-indexes','03-seed-data') {
  docker cp database/$f.sql mini-gestao-sqlserver:/tmp/$f.sql
  docker exec mini-gestao-sqlserver bash -c "/opt/mssql-tools18/bin/sqlcmd -C -b -f 65001 -S localhost -U sa -P `"`$MSSQL_SA_PASSWORD`" -i /tmp/$f.sql"
}
```

O script `01` cria o banco `MiniGestaoPedidos`. Para recomeçar do zero: `docker compose down -v`.

### Backend

```bash
cd backend
dotnet build MiniGestaoPedidos.sln
dotnet test MiniGestaoPedidos.sln
```

Para subir a API localmente (PowerShell, a partir da raiz do repositório):

```powershell
# Connection string apontando para o SQL Server do Docker exposto em localhost
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=MiniGestaoPedidos;User Id=sa;Password=<MSSQL_SA_PASSWORD do .env>;TrustServerCertificate=True"
dotnet run --project backend/src/API --launch-profile http
```

- Swagger: <http://localhost:5000/swagger>
- No `.env`, o host da connection string é `sqlserver` (rede do Docker Compose); fora do Docker use `localhost,1433`.
- O perfil `http` define `DOTNET_ROLL_FORWARD=Major`, útil quando só há o runtime ASP.NET Core mais novo instalado (os projetos têm alvo `net8.0`). Se você tem o runtime 8, pode remover essa variável de `backend/src/API/Properties/launchSettings.json`.
- CORS: origens liberadas em `Cors:AllowedOrigins` (`appsettings.json`; padrão `http://localhost:5173` e `http://localhost:3000`), sobrescrevíveis por `Cors__AllowedOrigins__0`.

#### Endpoints

| Método | Rota | Descrição | Respostas |
| ------ | ---- | --------- | --------- |
| POST | `/pedidos` | Cria pedido (transação única: itens, total e baixa de estoque) | 201, 400, 404, 409, 500 |
| GET | `/pedidos` | Lista com `clienteId`, `dataInicio`, `dataFim`, `page`, `pageSize`; mais recentes primeiro | 200, 400 |
| GET | `/relatorios/top-clientes` | Top 5 clientes por valor (`dataInicio` e `dataFim` obrigatórios) | 200, 400 |
| GET | `/clientes` | Apoio ao frontend: lista de clientes | 200 |
| GET | `/produtos` | Apoio ao frontend: lista de produtos com preço e estoque | 200 |

Detalhes de comportamento:

- Erros retornam `application/problem+json` (RFC 7807). Estoque insuficiente (409) inclui `produtoId`, `solicitado` e `disponivel`.
- Itens repetidos do mesmo produto em um pedido são consolidados (soma das quantidades).
- `dataFim` é inclusiva (o dia inteiro é considerado); `pageSize` padrão 10, máximo 100.
- O preço unitário é sempre o preço vigente do produto, e o total é calculado no backend.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

### Docker Compose

Hoje o `docker-compose.yml` sobe apenas o SQL Server. Os serviços `api` e `frontend` serão adicionados na Fase 5, com o objetivo de subir tudo com:

```bash
docker compose up
```

## Documentação adicional

Serão adicionados nas próximas fases: execução e testes detalhados, decisões técnicas, investigação da query otimizada, configuração do Cursor (Rules, Hook, Skill e MCP) e exemplos de uso de IA.
