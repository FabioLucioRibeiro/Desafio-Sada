# SADA — Gestão de Tarefas

API REST para criar, consultar, buscar, atualizar e excluir tarefas, desenvolvida em .NET 10 com Entity Framework Core InMemory, Swagger e testes xUnit. O escopo é exclusivamente backend, conforme o desafio; não é necessário frontend nem cadastro de usuários.

## Pré-requisitos

- SDK .NET 10: confira a instalação com `dotnet --version`.
- Git para clonar o repositório.
- Acesso ao NuGet para restaurar as dependências na primeira execução.

Não é necessário instalar banco de dados, executar migrations ou configurar credenciais.

## Como executar

```bash
git clone https://github.com/FabioLucioRibeiro/Desafio-Sada.git
cd Desafio-Sada
dotnet restore TaskManagement.sln
dotnet run --project Api --launch-profile http
```

- API: http://localhost:5046/api/tasks
- Swagger UI: http://localhost:5046/swagger
- Documento OpenAPI: http://localhost:5046/swagger/v1/swagger.json

O perfil `http` configura o ambiente `Development`, necessário para exibir o Swagger. Para recarregar alterações durante o desenvolvimento:

```bash
dotnet watch --project Api run --launch-profile http
```

## Arquitetura e justificativa

A solução usa três camadas e um projeto de testes:

| Projeto | Responsabilidade |
| --- | --- |
| `Api` | Controllers REST, DTO de entrada, conversão para commands, Swagger, logging HTTP e tratamento global de exceções. |
| `Domain` | Entidades, enum de status, commands e validações, handlers dos casos de uso, filtros, DTO de saída e interfaces dos serviços/repositórios. |
| `Infra` | DbContext, mapeamentos do EF Core e implementação do repositório InMemory. |
| `Tests` | Testes unitários dos handlers e testes de integração da persistência e dos endpoints. |

As dependências são `Api → Domain/Infra` e `Infra → Domain`. O Domain não depende do EF Core nem do ASP.NET Core. Os handlers dependem de `ITaskItemRepository`, cuja implementação é injetada na inicialização. Controllers delegam os casos de uso a `ITaskHandler`.

A escolha privilegia separação de responsabilidades, inversão de dependência e testes, mantendo a solução proporcional ao CRUD solicitado. Os handlers ficam no Domain por decisão de simplificação; uma camada Application separada não é necessária neste escopo. Commands representam operações de escrita, enquanto DTOs definem os dados trocados entre API e handlers. O repositório retorna entidades para que os handlers executem as alterações.

## Modelo e regras

| Campo | Regra |
| --- | --- |
| `id` | GUID gerado pelo servidor, chave primária usada nas rotas. |
| `code` | Número positivo gerado automaticamente pelo EF InMemory, configurado como chave alternativa e preservado na edição. Não faz parte do DTO de entrada. |
| `title` | Obrigatório, até 200 caracteres. Não aceita vazio ou somente espaços; espaços nas extremidades são removidos. |
| `description` | Opcional, até 4000 caracteres. Espaços nas extremidades são removidos; vazio ou somente espaços vira `null`. |
| `dueDate` | Opcional, no formato `yyyy-MM-dd`. Datas passadas são aceitas: o enunciado não proíbe tarefas vencidas. |
| `status` | `Pending` (Pendente), `InProgress` (Em progresso) ou `Completed` (Concluída). O padrão é `Pending`. No JSON, envie o nome, não um número. |
| `createdAt` | Data de criação em UTC, gerada pelo servidor, preservada na edição. |

Os dados e a sequência existem somente durante a vida do banco InMemory. Ao encerrar e reiniciar a aplicação, o banco é recriado vazio. O código não é um identificador global entre reinicializações, e não existe promessa de sequência sem lacunas. InMemory é utilizado por exigência do desafio; não simula todas as restrições ou transações de um banco relacional.

## Endpoints

| Método | Rota | Resultado |
| --- | --- | --- |
| POST | `/api/tasks` | Cria a tarefa; retorna `201`, a tarefa e o cabeçalho `Location`. |
| GET | `/api/tasks` | Retorna `200` e a lista ordenada por `code`. Sem resultados, retorna `[]`. |
| GET | `/api/tasks/{id}` | Retorna `200` ou `404`. |
| PUT | `/api/tasks/{id}` | Atualiza os campos editáveis; retorna `200` ou `404`. |
| DELETE | `/api/tasks/{id}` | Exclui; retorna `204` sem corpo ou `404`. |

As rotas individuais recebem `id` (GUID), não o `code`. No PUT, envie todos os campos que deseja manter: descrição e vencimento omitidos ficam nulos; status omitido assume `Pending`. O ID, o código e a data de criação são preservados.

### Filtros e busca

`GET /api/tasks` aceita filtros opcionais, combinados com **E**:

- `search`: busca parcial no título ou na descrição, sem distinguir maiúsculas/minúsculas. Não há normalização de acentos. Texto vazio não restringe a busca.
- `status`: filtra pelo status, por exemplo `Pending`.
- `dueDate`: inclui vencimentos **até a data informada, inclusive**. Tarefas sem vencimento são excluídas quando este filtro está presente.

Exemplo: `/api/tasks?search=reunião&status=Pending&dueDate=2026-10-10`.

### Exemplos com curl

Criar (copie o `id` da resposta para as chamadas seguintes):

```bash
curl -i -X POST http://localhost:5046/api/tasks \
  -H 'Content-Type: application/json' \
  -d '{"title":"Preparar apresentação","description":"Revisar slides","dueDate":"2026-10-10","status":"Pending"}'
```

Buscar e filtrar:

```bash
curl -G http://localhost:5046/api/tasks \
  --data-urlencode 'search=apresentação' \
  --data-urlencode 'status=Pending' \
  --data-urlencode 'dueDate=2026-10-10'
```

Consultar, atualizar e excluir (substitua o valor da variável):

```bash
TASK_ID='cole-o-guid-retornado-aqui'
curl "http://localhost:5046/api/tasks/$TASK_ID"

curl -i -X PUT "http://localhost:5046/api/tasks/$TASK_ID" \
  -H 'Content-Type: application/json' \
  -d '{"title":"Apresentação revisada","description":null,"dueDate":"2026-10-10","status":"Completed"}'

curl -i -X DELETE "http://localhost:5046/api/tasks/$TASK_ID"
```

Também é possível usar **Try it out** no Swagger ou as requisições de `Api/Api.http` com um cliente compatível.

## Validação, erros e logs

A API valida seus DTOs e os handlers validam commands, inclusive quando chamados diretamente. Retornos principais:

- `400`: título inválido, limite excedido, JSON/status/data inválidos ou GUID malformado.
- `404`: tarefa inexistente, inclusive nas operações de atualização e exclusão.
- `500`: falha inesperada; a resposta não expõe detalhes internos.

Erros usam `ProblemDetails` ou `ValidationProblemDetails`. Exceções tratadas globalmente incluem `traceId`. Criação, atualização e exclusão são registradas com o ID da tarefa; falhas inesperadas são registradas com sua exceção.

## Como testar

```bash
dotnet test TaskManagement.sln --configuration Release
```

Para executar grupos separadamente:

```bash
dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~Unit'
dotnet test Tests/Tests.csproj --filter 'FullyQualifiedName~Integration'
```

- **Unitários:** criação e atualização, limites de campos, título obrigatório, status inválido, normalização, preservação da identidade, ausência de tarefa e exclusão. Usam uma implementação em memória do contrato de repositório, independente do EF.
- **Integração com EF InMemory:** geração de códigos distintos entre contextos e após exclusão, momento da persistência, preservação do código e filtros isolados/combinados.
- **Integração HTTP:** CRUD, `Location`, códigos HTTP, entradas inválidas, código gerado pelo servidor mesmo se enviado como campo extra, filtros e documento Swagger.

Cada teste de integração recebe um banco isolado. Os testes HTTP utilizam `WebApplicationFactory` e não exigem iniciar a API manualmente.

## Verificação antes da entrega

```bash
dotnet build TaskManagement.sln --configuration Release
dotnet test TaskManagement.sln --configuration Release --no-build
```

Arquivos compilados (`bin/`, `obj/`) e resultados de testes são ignorados pelo Git. Não há autenticação, frontend ou banco persistente porque não são exigidos pelo desafio.
# Desafio-Sada
