# Mini Gestão de Pedidos

Aplicação web para gerenciamento de pedidos de clientes: cadastro de pedidos, consulta com filtros e paginação e relatório dos cinco clientes com maior volume de compras.

Projeto desenvolvido com abordagem **AI First** usando o Cursor. Veja a especificação em [docs/spec.md](docs/spec.md) e o plano de implementação em [docs/plan.md](docs/plan.md).

> Status: **Fases 0 (estrutura), 1 (banco de dados), 2 (backend), 3 (frontend), 4 (testes) e 7 (configuração AI First no Cursor) concluídas**. As demais fases (5 Docker e 6 documentação final) estão descritas no plano.

## Stack

- **Backend:** .NET 8 Web API, C#, Entity Framework Core, SQL Server, xUnit
- **Frontend:** React, TypeScript, Vite, Axios
- **Infraestrutura:** Docker Compose, SQL Server em container, Testcontainers

## Estrutura do repositório

```text
.
├── docs/                 # spec.md e plan.md
├── .cursor/              # configuração AI First (Fase 7)
│   ├── rules/                # project-, backend- e frontend-standards.mdc
│   ├── hooks.json            # hook "stop"
│   ├── hooks/validate-build.sh
│   ├── skills/create-endpoint/SKILL.md
│   └── mcp.json              # MCP SQL Server (somente leitura)
├── database/             # scripts SQL (Fase 1)
│   └── optional/             # scripts opcionais (usuário readonly do MCP)
├── backend/
│   ├── MiniGestaoPedidos.sln
│   ├── src/
│   │   ├── API/              # controllers, middlewares, composição
│   │   ├── Application/      # casos de uso, DTOs, validações
│   │   ├── Domain/           # entidades e regras de negócio
│   │   └── Infrastructure/   # EF Core, repositórios
│   └── tests/
│       ├── UnitTests/            # xUnit (Domain/Application)
│       └── IntegrationTests/     # xUnit + SQL Server em Docker
├── frontend/             # React + TypeScript + Vite (Fase 3)
│   ├── src/api|types|hooks|components|pages
│   └── .env.example          # VITE_API_URL
├── e2e/                  # Playwright (Fase 4)
├── .env.example
├── .env.mcp.example
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

A UI cobre listagem de pedidos (filtros por cliente e período, paginação) e criação via modal. Subtotais e total no formulário são apenas informativos; preço e estoque ficam a cargo da API.

```bash
# Em frontend/, copie o exemplo e ajuste se a API não estiver em :5000
copy .env.example .env   # Windows
# cp .env.example .env   # Linux/macOS

cd frontend
npm install
npm run dev
```

- App: <http://localhost:5173> (porta padrão do Vite)
- Variável: `VITE_API_URL` (padrão `http://localhost:5000`) — o Vite só lê `.env` dentro de `frontend/`
- Qualidade: `npm run lint --prefix frontend` e `npm run build --prefix frontend`
- A API precisa estar rodando (veja a seção Backend) e o CORS já libera `http://localhost:5173`

### Testes

#### Unitários e integração (.NET)

```powershell
# Só unitários (sem Docker)
dotnet test backend/tests/UnitTests/UnitTests.csproj

# Integração (exige Docker Desktop ligado; sobe SQL Server 2022 temporário)
dotnet test backend/tests/IntegrationTests/IntegrationTests.csproj

# Solution inteira
dotnet test backend/MiniGestaoPedidos.sln

# Pular integração
dotnet test backend/MiniGestaoPedidos.sln --filter "Category!=Integration"
```

- Unitários: Domain + Application com NSubstitute/`FakeUnitOfWork` (sem banco).
- Integração: `WebApplicationFactory` + SQL Server real em Docker; scripts `database/01-03` aplicados no setup; reset do seed antes de cada teste.
- Observação Windows + ASP.NET 11: o projeto `IntegrationTests` usa TFM `net11.0` (TestHost alinhado ao runtime instalado). A API permanece `net8.0`. A fixture usa a CLI do Docker (o `Testcontainers.MsSql` está referenciado, mas o Docker.DotNet quebra no named pipe com .NET 11 neste ambiente).

#### E2E (Playwright)

Pré-requisito: SQL Server do Compose no ar com os scripts 01–03 aplicados.

```powershell
docker compose up -d
# se o banco ainda não existir, rode os scripts 01, 02 e 03 (veja "Banco de dados")

npm install --prefix e2e
npm run install:browsers --prefix e2e
npm test --prefix e2e
# ou: npm run test:headed --prefix e2e
```

O Playwright sobe API (`dotnet run` com connection string do `.env`) e frontend (`npm run dev`) automaticamente, ou reaproveita se já estiverem rodando. O fluxo coberto: abrir a tela, criar pedido e validar na listagem filtrada.

### Docker Compose

Hoje o `docker-compose.yml` sobe apenas o SQL Server. Os serviços `api` e `frontend` serão adicionados na Fase 5, com o objetivo de subir tudo com:

```bash
docker compose up
```

## Configuração AI First no Cursor

Toda a configuração fica versionada em [.cursor/](.cursor/) (exceto segredos). Visão geral:

| Recurso | Arquivo | Para quê |
| ------- | ------- | -------- |
| Rules | `.cursor/rules/*.mdc` | Contexto persistente para o agente |
| Hook | `.cursor/hooks.json` + `.cursor/hooks/validate-build.sh` | Valida o build ao final de cada tarefa |
| Skill | `.cursor/skills/create-endpoint/SKILL.md` | Roteiro para criar endpoints no padrão do projeto |
| MCP | `.cursor/mcp.json` | Consultas somente leitura ao SQL Server |

### Rules

- `project-standards.mdc` (`alwaysApply: true`): arquitetura, pastas, nomenclatura, commits em português (Conventional Commits), comandos de build/teste/Docker e a regra de **não implementar nada fora de [docs/spec.md](docs/spec.md) sem aprovação**.
- `backend-standards.mdc` (`globs: backend/**`): camadas Domain/Application/Infrastructure/API, DTOs, exceções e `ProblemDetails`, EF Core mapeado aos scripts SQL, convenções de testes unitários e de integração.
- `frontend-standards.mdc` (`globs: frontend/**`): React + TypeScript + Vite, organização de pastas, tipagem, Axios, estados de loading/erro e validação de formulários.

### Hook de validação de build

O hook roda no evento `stop` (quando o agente termina uma tarefa). O script `validate-build.sh`:

1. Só valida quando o agente terminou normalmente (`status: completed`).
2. Usa `git status` para ver o que mudou: se `backend/` mudou, roda `dotnet build backend/MiniGestaoPedidos.sln`; se `frontend/` mudou (e existem `package.json` e `node_modules`), roda `npm run build --prefix frontend`.
3. Se o build falhar, devolve um `followup_message` com os erros e o Cursor o reenvia ao agente para corrigir (até `loop_limit: 3` tentativas). Se passar, devolve `{}`.

Limitações: o evento `stop` não bloqueia por exit code, então o "bloqueio" é um pedido automático de correção. Se `dotnet`, `npm` ou `node_modules` não existirem, o hook avisa no stderr e pula a etapa (falha aberta). O log da última execução fica em `.cursor/hooks/.last-build.log` (ignorado pelo Git) e a saída do hook aparece na aba **Hooks** do Cursor.

**Como executa em cada sistema**

- **Windows:** o Cursor executa hooks via PowerShell, que não roda `.sh` sozinho, e o `bash` do PATH costuma ser o stub do WSL. Por isso o `hooks.json` chama o **Git Bash pelo caminho curto** (sem espaços):
  `C:\\PROGRA~1\\Git\\bin\\bash.exe .cursor/hooks/validate-build.sh`.
  Requer o Git for Windows instalado no caminho padrão; caso contrário ajuste o caminho (ou use `bash` se ele resolver para um Bash válido, ex.: WSL com distro Linux).
- **Linux/macOS:** troque o `command` do `hooks.json` por `bash .cursor/hooks/validate-build.sh` (opcionalmente `chmod +x` no script).
- Teste manual (qualquer sistema com bash): `echo '{"status":"completed"}' | bash .cursor/hooks/validate-build.sh`.

### Skill `create-endpoint`

Descreve o passo a passo e o checklist para criar endpoints seguindo a arquitetura real: confirmar escopo na spec, DTOs, validação, regra no Domain/Service, repositório e mapeamento, Controller com Swagger, tratamento de erros e testes unitários e de integração. Invoque citando `create-endpoint` (ex.: "use a skill create-endpoint para criar GET /algo") ou deixe o agente aplicá-la ao detectar pedidos de novo endpoint.

### MCP SQL Server (somente leitura)

Usa o pacote `mssql-read-only-mcp` (versão fixada em `1.0.2`), que expõe só `list_table`, `describe_table` e `read_data` (apenas `SELECT`). O `.cursor/mcp.json` **não contém credenciais**: lê o arquivo `.env.mcp` (ignorado pelo Git) via `envFile`.

Passo a passo:

1. Suba o banco e execute os scripts `01` a `03` (veja "Banco de dados").
2. Crie o usuário somente leitura (informe a senha por variável do sqlcmd; evite aspas simples):

   ```powershell
   docker cp database/optional/create-readonly-user.sql mini-gestao-sqlserver:/tmp/ro.sql
   docker exec mini-gestao-sqlserver bash -c '/opt/mssql-tools18/bin/sqlcmd -C -b -f 65001 -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -v MCP_READONLY_PASSWORD="<SenhaForte#123>" -i /tmp/ro.sql'
   ```

3. Copie `.env.mcp.example` para `.env.mcp` e coloque a mesma senha em `SQL_PASSWORD`.
4. Em **Cursor Settings > MCP**, ative o servidor `sqlserver` (reinicie o Cursor se não aparecer).
5. Exemplo de uso no chat: "liste as tabelas e descreva `Pedidos`", "mostre o top 5 clientes de 2026 com a query otimizada e compare com o plano".

Limitações e cuidados:

- Somente leitura por dois motivos independentes: o servidor aceita só `SELECT` **e** o login `mcp_readonly` pertence apenas a `db_datareader` (nunca use `sa`).
- Serve para explorar esquema, dados de exemplo e investigar queries; não aplica scripts nem altera dados. Mudanças de esquema continuam sendo feitas em `database/*.sql`.
- Pacote de terceiros executado via `npx`: mantenha a versão fixada e revise antes de atualizar.
- Aponte apenas para o banco local de desenvolvimento (dados fictícios). Não use contra produção nem cole resultados com dados sensíveis em prompts.
- Nunca versione `.env.mcp`. Apenas `.env.mcp.example` (sem segredos) faz parte do repositório.

## Documentação adicional

Serão adicionados nas próximas fases: execução e testes detalhados, decisões técnicas, investigação da query otimizada e exemplos de uso de IA.
