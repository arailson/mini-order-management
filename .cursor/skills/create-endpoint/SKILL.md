---
name: create-endpoint
description: Guia a criação de novos endpoints REST no backend .NET do Mini Gestão de Pedidos seguindo a arquitetura em camadas (DTOs, validação, Service, repositório, Controller com Swagger, erros ProblemDetails e testes). Use quando o usuário pedir para criar, adicionar ou alterar um endpoint, rota, controller ou caso de uso da API.
---

# Criar endpoint (backend .NET 8)

Siga a ordem abaixo. Caminhos relativos a `backend/`. Padrões completos em `.cursor/rules/backend-standards.mdc`.

## Checklist

```
- [ ] 0. Escopo aprovado
- [ ] 1. DTOs
- [ ] 2. Domain (regras e exceções)
- [ ] 3. Contrato e implementação do repositório
- [ ] 4. Service (validação + regra de negócio)
- [ ] 5. Controller + Swagger
- [ ] 6. Mapeamento de erros
- [ ] 7. Testes unitários e de integração
- [ ] 8. Verificação e documentação
```

### 0. Escopo

O endpoint deve estar em `docs/spec.md`. Se não estiver, **pare e peça aprovação explícita** antes de implementar.

### 1. DTOs (`src/Application/DTOs/`)

- `record` para request e response; nomes em português (`CriarXRequest`, `XResponse`).
- Nunca retornar entidades de Domain. Campos de resposta em camelCase (padrão do serializador).

### 2. Domain (`src/Domain/`)

- Invariantes nas entidades (`Entities/`), com exceções específicas em `Exceptions/`:
  - regra violada: `DomainException` (400)
  - recurso inexistente: `NotFoundException` (404)
  - conflito de estado (ex.: estoque): exceção dedicada (409)

### 3. Persistência

- Contrato em `src/Application/Abstractions/` (interface do repositório). Não expor `IQueryable`/EF para a Application.
- Implementação em `src/Infrastructure/Persistence/Repositories/`. Leituras com `AsNoTracking()`.
- Se houver nova tabela/coluna/índice: atualizar `database/*.sql` **e** o mapeamento em `Infrastructure/Persistence/Configurations/` com os mesmos nomes (sem migrations).
- Filtros de data: `>= inicio` e `< fim.AddDays(1)`, sem funções na coluna.

### 4. Service (`src/Application/Services/`)

- Interface `IXService` + implementação; registrar em `src/Application/DependencyInjection.cs`.
- Validação de entrada primeiro, acumulando erros:

```csharp
var erros = new Dictionary<string, string[]>();
if (request.ClienteId <= 0) erros["clienteId"] = new[] { "O clienteId deve ser maior que zero." };
if (erros.Count > 0) throw new ValidationException(erros);   // 400
```

- Regras de negócio no Service/Domain, nunca no Controller. Escritas com várias etapas dentro de `IUnitOfWork.ExecuteInTransactionAsync` (rollback automático).
- Datas com `TimeProvider`. Valores monetários sempre calculados no backend.

### 5. Controller (`src/API/Controllers/`)

- `[ApiController]`, `[Route("recurso-em-portugues")]`, controller fino (só chama o Service).
- Documentar para o Swagger: comentário `///` e `[ProducesResponseType]` para cada status (200/201, 400, 404, 409, 500) com `ProblemDetails` nos erros.
- `POST` de criação retorna `201` com `Location`; listas paginadas retornam `{ items, page, pageSize, totalItems, totalPages }`.

### 6. Erros

`ExceptionHandlingMiddleware` já converte exceções em `ProblemDetails`. Só altere se criar um **novo tipo** de exceção:

| Exceção | HTTP |
| ------- | ---- |
| `ValidationException`, `DomainException`, JSON/parâmetro inválido | 400 |
| `NotFoundException` | 404 |
| `EstoqueInsuficienteException` (conflito) | 409 |
| qualquer outra | 500 (mensagem genérica, erro logado) |

### 7. Testes

- **Unitários** (`tests/UnitTests`): Service e Domain com repositórios falsos; cobrir sucesso, validação (400), inexistente (404) e conflito (409) quando aplicável. Nome `Metodo_Cenario_Resultado`.
- **Integração** (`tests/IntegrationTests`): `WebApplicationFactory<Program>` + SQL Server real em Docker (fixture via CLI do Docker), scripts de `database/` aplicados; chamar o endpoint HTTP e validar status e corpo.

### 8. Verificação

```powershell
dotnet build backend/MiniGestaoPedidos.sln
dotnet test  backend/MiniGestaoPedidos.sln
```

- Testar no Swagger (`http://localhost:5000/swagger`) com a API apontando para o SQL Server do Docker.
- Atualizar a tabela de endpoints no `README.md`.
- O hook `stop` valida o build ao final; corrija qualquer falha que ele reportar.
