# Mini Gestão de Pedidos

Aplicação web para gerenciamento de pedidos de clientes: cadastro de pedidos, consulta com filtros e paginação e relatório dos cinco clientes com maior volume de compras.

Projeto desenvolvido com abordagem **AI First** usando o Cursor. Veja a especificação em [docs/spec.md](docs/spec.md) e o plano de implementação em [docs/plan.md](docs/plan.md).

> Status: **Fase 0 concluída** (estrutura base). As demais fases estão descritas no plano.

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

### Backend

```bash
cd backend
dotnet build MiniGestaoPedidos.sln
dotnet test MiniGestaoPedidos.sln
dotnet run --project src/API
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

### Docker Compose

O `docker-compose.yml` será adicionado na Fase 5. O objetivo é subir tudo com:

```bash
docker compose up
```

## Documentação adicional

Serão adicionados nas próximas fases: execução e testes detalhados, decisões técnicas, investigação da query otimizada, configuração do Cursor (Rules, Hook, Skill e MCP) e exemplos de uso de IA.
