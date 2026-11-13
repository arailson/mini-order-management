# Especificação Técnica — Mini Gestão de Pedidos

- **Projeto:** Teste Técnico Full Stack — Joya Solutions
- **Autor:** Arailson Santos
- **Versão:** 1.1
- **Status:** Especificação inicial

---

## 1. Objetivo

Desenvolver uma aplicação web para gerenciamento de pedidos de clientes, permitindo cadastrar pedidos, consultar pedidos existentes com filtros e visualizar um relatório de clientes com maior volume de compras.

O projeto será desenvolvido utilizando uma abordagem **AI First**, com apoio do Cursor durante o planejamento, implementação, testes, documentação e revisão do código.

A solução deverá ser executável integralmente por meio do **Docker Compose**.

---

## 2. Stack tecnológica

### Backend

- .NET 8 Web API
- C#
- Entity Framework Core
- SQL Server
- xUnit para testes unitários e de integração

### Frontend

- React
- TypeScript
- Vite
- Axios para comunicação HTTP

### Infraestrutura

- SQL Server em container Docker
- Docker Compose
- Git e GitHub
- Testcontainers para testes de integração

### Ferramenta de Inteligência Artificial

- Cursor Pro
- Cursor Agent
- Claude Opus ou outro modelo disponível no Cursor, selecionado conforme a complexidade da tarefa

---

## 3. Regras de negócio

### 3.1 Clientes

- Um pedido deve estar associado a um cliente existente.
- Clientes devem possuir identificador único, nome e informações de contato.
- Clientes podem possuir vários pedidos.

### 3.2 Produtos

- Cada produto deve possuir identificador único, nome, preço e quantidade disponível em estoque.
- O preço do produto deve ser maior que zero.
- O estoque não pode ser negativo.

### 3.3 Pedidos

- Um pedido deve estar associado a um cliente existente.
- Um pedido deve possuir pelo menos um item.
- A quantidade de cada item deve ser maior que zero.
- O produto informado deve existir.
- O estoque disponível deve ser suficiente para atender à quantidade solicitada.
- O preço unitário aplicado ao pedido deve corresponder ao preço vigente do produto no momento da criação.
- O valor total do pedido será calculado pelo backend, nunca confiando em valores enviados pelo frontend.
- O estoque deve ser atualizado após a criação bem-sucedida do pedido.
- A criação do pedido, seus itens e a atualização do estoque devem ocorrer em uma **única transação**.
- Em caso de falha, nenhuma alteração parcial deverá ser persistida.

### 3.4 Consulta de pedidos

- A listagem deve permitir filtrar pedidos por cliente.
- A listagem deve permitir filtrar pedidos por período de data.
- Os resultados devem possuir paginação.
- A ordenação padrão será por data do pedido, da mais recente para a mais antiga.

### 3.5 Relatório de clientes

- O relatório deve apresentar os cinco clientes com maior valor total de pedidos no período informado.
- O valor total será calculado pela soma dos valores dos itens dos pedidos.
- Pedidos cancelados, caso essa funcionalidade seja introduzida futuramente, não deverão ser contabilizados.

---

## 4. Modelo de dados

### 4.1 Clientes

| Campo        | Tipo          | Descrição        |
| ------------ | ------------- | ---------------- |
| Id           | INT           | Chave primária   |
| Nome         | NVARCHAR(150) | Nome do cliente  |
| Email        | NVARCHAR(150) | E-mail do cliente |
| DataCadastro | DATETIME2     | Data de cadastro |

### 4.2 Produtos

| Campo   | Tipo          | Descrição            |
| ------- | ------------- | -------------------- |
| Id      | INT           | Chave primária       |
| Nome    | NVARCHAR(150) | Nome do produto      |
| Preco   | DECIMAL(18,2) | Preço unitário       |
| Estoque | INT           | Quantidade disponível |

### 4.3 Pedidos

| Campo      | Tipo          | Descrição             |
| ---------- | ------------- | --------------------- |
| Id         | INT           | Chave primária        |
| ClienteId  | INT           | Chave estrangeira     |
| DataPedido | DATETIME2     | Data de criação       |
| ValorTotal | DECIMAL(18,2) | Valor total calculado |

### 4.4 ItensPedido

| Campo         | Tipo          | Descrição                  |
| ------------- | ------------- | -------------------------- |
| Id            | INT           | Chave primária             |
| PedidoId      | INT           | Chave estrangeira          |
| ProdutoId     | INT           | Chave estrangeira          |
| Quantidade    | INT           | Quantidade solicitada      |
| PrecoUnitario | DECIMAL(18,2) | Preço registrado no pedido |

### 4.5 Relacionamentos

- Clientes 1:N Pedidos
- Pedidos 1:N ItensPedido
- Produtos 1:N ItensPedido

### 4.6 Índices

Serão criados índices para:

- `Pedidos.ClienteId`
- `Pedidos.DataPedido`
- `ItensPedido.PedidoId`
- `ItensPedido.ProdutoId`

Índices adicionais poderão ser definidos conforme análise do plano de execução das consultas.

---

## 5. API REST

### 5.1 POST /pedidos

Cria um novo pedido.

**Request:**

```json
{
  "clienteId": 1,
  "itens": [
    {
      "produtoId": 1,
      "quantidade": 2
    }
  ]
}
```

**Response — 201 Created:**

```json
{
  "id": 1,
  "clienteId": 1,
  "dataPedido": "2026-11-12T18:00:00",
  "valorTotal": 200.00,
  "itens": [
    {
      "produtoId": 1,
      "quantidade": 2,
      "precoUnitario": 100.00,
      "subtotal": 200.00
    }
  ]
}
```

**Possíveis erros:**

- **400 Bad Request:** dados inválidos ou pedido sem itens.
- **404 Not Found:** cliente ou produto inexistente.
- **409 Conflict:** estoque insuficiente.
- **500 Internal Server Error:** erro inesperado.

### 5.2 GET /pedidos

Lista pedidos com filtros e paginação.

**Query parameters:**

- `clienteId`: identificador opcional do cliente.
- `dataInicio`: data inicial opcional.
- `dataFim`: data final opcional.
- `page`: página atual.
- `pageSize`: quantidade de registros por página.

**Exemplo:**

```http
GET /pedidos?clienteId=1&dataInicio=2026-01-01&dataFim=2026-12-31&page=1&pageSize=10
```

**Response:**

```json
{
  "items": [],
  "page": 1,
  "pageSize": 10,
  "totalItems": 0,
  "totalPages": 0
}
```

### 5.3 GET /relatorios/top-clientes

Retorna os cinco clientes com maior valor total de pedidos em determinado período.

**Query parameters:**

- `dataInicio`: data inicial obrigatória.
- `dataFim`: data final obrigatória.

**Exemplo:**

```http
GET /relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-12-31
```

**Response:**

```json
[
  {
    "clienteId": 1,
    "nome": "Cliente Exemplo",
    "totalComprado": 15000.00
  }
]
```

---

## 6. Frontend

### 6.1 Tela de listagem de pedidos

A tela deverá conter:

- Tabela de pedidos com:
  - Identificador do pedido.
  - Nome do cliente.
  - Data do pedido.
  - Valor total.
- Filtro por cliente.
- Filtro por período.
- Paginação.
- Botão para criação de pedido.

### 6.2 Tela ou modal de criação de pedido

A funcionalidade deverá permitir:

- Selecionar um cliente.
- Selecionar produtos.
- Informar quantidades.
- Visualizar subtotais e valor total.
- Confirmar a criação.
- Exibir mensagens de sucesso ou erro retornadas pela API.

O frontend **não** será responsável por determinar o preço definitivo ou validar o estoque final.

---

## 7. Debug e otimização da query

A consulta original possui problemas relacionados à filtragem, agregação e ordenação.

A versão proposta é:

```sql
SELECT TOP 5
    c.Id AS ClienteId,
    c.Nome,
    SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
INNER JOIN Pedidos p
    ON p.ClienteId = c.Id
INNER JOIN ItensPedido i
    ON i.PedidoId = p.Id
WHERE
    p.DataPedido >= '20260101'
    AND p.DataPedido < '20270101'
GROUP BY
    c.Id,
    c.Nome
ORDER BY
    Total DESC;
```

### Problemas identificados

- O uso de `YEAR(p.DataPedido)` no filtro pode impedir o uso eficiente de um índice sobre `DataPedido`.
- O `LEFT JOIN` combinado com o filtro na tabela `Pedidos` acaba eliminando clientes sem pedidos no período.
- O `ORDER BY Total` sem `DESC` apresenta o ranking em ordem crescente.
- O agrupamento apenas por `Nome` pode unir clientes diferentes que possuem nomes iguais.
- A ausência de `TOP 5` retorna todos os clientes, embora o relatório solicitado seja limitado aos cinco primeiros.

### Investigação

A análise deverá considerar:

- Plano de execução estimado e real.
- Índices existentes.
- Quantidade de registros envolvidos.
- Leituras lógicas e custo das operações.
- Comparação entre a consulta original e a otimizada.

A consulta otimizada deverá ser validada no SQL Server e sua investigação documentada no README.

---

## 8. Testes

### 8.1 Testes unitários

Cobrirão:

- Criação de pedido com dados válidos.
- Cliente inexistente.
- Produto inexistente.
- Estoque insuficiente.
- Pedido sem itens.
- Quantidade inválida.
- Cálculo correto do valor total.
- Atualização correta do estoque.
- Falha transacional.

### 8.2 Testes de integração

Serão realizados contra um SQL Server real, preferencialmente por meio de Testcontainers.

Cobrirão:

- `POST /pedidos` com sucesso.
- `POST /pedidos` com cliente inexistente.
- `POST /pedidos` com estoque insuficiente.
- `GET /pedidos` com filtros e paginação.
- `GET /relatorios/top-clientes` com dados persistidos.

### 8.3 Testes end to end

Como diferencial, serão implementados testes com Playwright para:

- Acessar a aplicação.
- Criar um pedido.
- Verificar o pedido na listagem.

---

## 9. Docker

O projeto deverá possuir um `docker-compose.yml` na raiz do repositório.

### Serviços

- `sqlserver`: banco de dados SQL Server.
- `api`: backend .NET.
- `frontend`: aplicação React.

### Requisitos

- Configuração por variáveis de ambiente.
- Healthcheck para o SQL Server.
- Inicialização adequada da API após a disponibilidade do banco.
- Scripts SQL executados automaticamente na inicialização de um banco novo.
- Frontend acessível por meio de uma porta HTTP.
- Aplicação executável com `docker compose up`.

---

## 10. Estrutura de diretórios proposta

```text
mini-gestao-pedidos/
├── docs/
│   ├── spec.md
│   └── plan.md
├── .cursor/
│   ├── rules/
│   │   ├── project-standards.mdc
│   │   ├── backend-standards.mdc
│   │   └── frontend-standards.mdc
│   ├── hooks.json
│   ├── hooks/
│   │   └── validate-build.sh
│   ├── skills/
│   │   └── create-endpoint/
│   │       └── SKILL.md
│   └── mcp.json
├── database/
│   ├── 01-create-tables.sql
│   ├── 02-create-indexes.sql
│   └── 03-seed-data.sql
├── backend/
│   ├── src/
│   │   ├── API/
│   │   ├── Application/
│   │   ├── Domain/
│   │   └── Infrastructure/
│   └── tests/
│       ├── UnitTests/
│       └── IntegrationTests/
├── frontend/
│   └── src/
├── docker-compose.yml
├── README.md
└── .gitignore
```

---

## 11. Configuração AI First com Cursor

### 11.1 Rules

Serão utilizadas regras de projeto armazenadas em `.cursor/rules/`.

As regras deverão definir:

- Arquitetura e organização de diretórios.
- Convenções de nomenclatura.
- Padrões de implementação do backend.
- Padrões de implementação do frontend.
- Convenções de testes.
- Comandos para build, testes e execução.
- Orientações para que a IA não implemente funcionalidades fora do escopo sem aprovação.

### 11.2 Hook

Será implementado pelo menos um hook útil no Cursor.

Exemplo:

- Executar validação de build ou formatação após alterações relevantes.
- Impedir que alterações sejam consideradas concluídas caso a validação falhe.

O hook deverá ser versionado e documentado no repositório.

### 11.3 Skill customizada

Será criada uma skill customizada para orientar a criação de novos endpoints seguindo o padrão arquitetural do projeto.

A skill deverá orientar a IA sobre:

- Criação dos DTOs.
- Validação de entrada.
- Implementação da lógica de negócio.
- Tratamento de erros.
- Persistência.
- Testes unitários e de integração.

### 11.4 MCP

Será configurado um MCP para conectar o Cursor ao SQL Server.

O MCP será utilizado durante o desenvolvimento para:

- Consultar a estrutura do banco.
- Inspecionar tabelas e relacionamentos.
- Consultar dados de exemplo.
- Auxiliar na investigação e otimização das queries.

O README deverá explicar como o MCP foi configurado e utilizado, incluindo as limitações e os cuidados adotados.

A configuração não deverá expor credenciais reais no repositório.

### 11.5 Planejamento com IA

O planejamento da implementação será elaborado utilizando o modo de planejamento do Cursor Agent.

O arquivo `docs/plan.md` deverá conter:

- Plano inicial gerado pela IA.
- Etapas de implementação.
- Dependências entre etapas.
- Ajustes realizados pelo desenvolvedor.
- Decisões que foram alteradas ou rejeitadas.
- Justificativas técnicas para as alterações.

---

## 12. Critérios de aceite

- [ ] O banco SQL Server possui as quatro tabelas requeridas.
- [ ] As chaves primárias, estrangeiras e índices estão configurados.
- [ ] Os scripts de criação e carga de dados estão versionados.
- [ ] A API permite criar pedidos com validação de cliente e estoque.
- [ ] A API permite listar pedidos com filtros e paginação.
- [ ] A API permite consultar o ranking dos cinco maiores clientes.
- [ ] O frontend permite criar e consultar pedidos.
- [ ] Os testes unitários cobrem as regras de negócio.
- [ ] Os testes de integração executam contra SQL Server real.
- [ ] A query otimizada está documentada no README.
- [ ] O projeto sobe com Docker Compose.
- [ ] O README documenta a execução, os testes e as decisões técnicas.
- [ ] O Cursor está configurado com Rules, Hooks, Skills e MCP.
- [ ] O plano de implementação foi elaborado com o Cursor e documentado.
- [ ] O histórico de commits demonstra evolução incremental.
- [ ] O vídeo de demonstração possui até 10 minutos.

---

## 13. Considerações sobre o uso de IA

A inteligência artificial será utilizada como ferramenta de apoio durante todo o ciclo de desenvolvimento.

O Cursor será utilizado para:

- Elaborar e revisar a especificação técnica.
- Gerar o plano de implementação.
- Auxiliar nas decisões arquiteturais.
- Implementar funcionalidades.
- Criar testes.
- Investigar erros.
- Auxiliar na análise de desempenho das consultas SQL.
- Revisar e documentar o código.

O desenvolvedor será responsável por:

- Definir os requisitos.
- Revisar o planejamento gerado pela IA.
- Avaliar as decisões arquiteturais.
- Validar o código gerado.
- Executar e interpretar os testes.
- Investigar erros e corrigir implementações inadequadas.
- Documentar decisões e limitações.

O código gerado pela IA não será considerado correto sem validação.

A utilização do Cursor será documentada no README, incluindo exemplos de tarefas realizadas, resultados obtidos, correções necessárias e reflexões sobre o processo de desenvolvimento.
