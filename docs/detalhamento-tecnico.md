# Detalhamento Técnico — Sistema de Emissão de Notas Fiscais

## Visão geral da arquitetura

O sistema é composto por dois microsserviços independentes em **ASP.NET Core (C#/.NET 10)**, cada um com seu próprio banco de dados físico (SQLite), e um frontend em **Angular 18**:

- **Estoque.Api** (`Services/Estoque.Api`, porta 5100) — cadastro de produtos e controle de saldo.
- **Faturamento.Api** (`Services/Faturamento.Api`, porta 5200) — cadastro de notas fiscais e ação de impressão, que se comunica com o Estoque via HTTP.
- **Frontend** (`Frontend/`, porta 4200) — aplicação Angular que consome os dois serviços diretamente.

Uma biblioteca compartilhada, **Korp.Shared** (`Shared/Korp.Shared`), contém a infraestrutura de tratamento de erros usada pelos dois serviços (referenciada via `ProjectReference`, compilada estaticamente em cada serviço — não é uma dependência em tempo de execução, então não quebra a independência dos microsserviços).

---

## 1. Ciclos de vida do Angular utilizados

- **`ngOnInit`** é usado em todo componente que carrega dados ao ser exibido: `ProdutoListComponent`, `ProdutoFormComponent`, `NotaListComponent`, `NotaFormComponent`, `NotaDetailComponent`. É nele que as chamadas HTTP iniciais são disparadas (via os stores ou diretamente via serviço).
- **`takeUntilDestroyed()`** (API moderna do Angular 16+, `@angular/core/rxjs-interop`) é usada em `NotaDetailComponent` (carregamento da nota e ação de impressão) e `ProdutoFormComponent` (carregamento do produto ao editar) para garantir que a subscription HTTP seja cancelada automaticamente quando o componente é destruído — substituindo o padrão clássico de `ngOnDestroy` manual com `Subject`/`unsubscribe()`. Essa foi uma escolha deliberada: menos código boilerplate, mesma garantia de limpeza.
- Nos demais casos (listas exibidas via `async` pipe), o próprio Angular gerencia o ciclo de vida da subscription automaticamente, então nenhum `ngOnDestroy` explícito é necessário.

## 2. Uso do RxJS

Sim, RxJS é usado de forma deliberada em vários pontos, além do uso implícito do `HttpClient` (que já retorna Observables):

- **`BehaviorSubject`** — `ProdutoStoreService` (`Frontend/src/app/features/produtos/services/produto-store.service.ts`) e `InvoiceStoreService` (`.../notas-fiscais/services/invoice-store.service.ts`) mantêm um cache compartilhado de produtos/notas entre telas, evitando refetch desnecessário quando o usuário navega entre a lista e o formulário. O método `refresh()` força uma nova busca (usado após criar/editar/imprimir), enquanto `load()` reaproveita o cache quando já carregado.
- **`debounceTime` / `distinctUntilChanged` / `switchMap`** — filtro de busca em `ProdutoListComponent`: o campo de filtro é ligado a um `FormControl`, cujo `valueChanges` passa por debounce de 250ms, ignora valores repetidos e então filtra o stream de produtos do store.
- **`finalize()`** — usado em praticamente toda chamada HTTP que precisa resetar um estado de carregamento (`printing`, `salvando`, `carregando`, `loading$`) independentemente de sucesso ou erro — por exemplo, no botão "Imprimir" de `NotaDetailComponent`, garantindo que o spinner sempre pare e o botão volte a ficar clicável, mesmo em caso de falha (essencial para permitir nova tentativa após um erro 503 do serviço de estoque).
- **`switchMap` / `take(1)` / `map`** — usados nos fluxos de submit (`ProdutoFormComponent`, `NotaFormComponent`) para encadear a chamada de criação/atualização com a atualização do store correspondente, e no fluxo de exclusão de produto (`ProdutoListComponent.excluir`), que encadeia o resultado do diálogo de confirmação (`MatDialog.afterClosed()`) com a chamada DELETE via `switchMap`.

## 3. Outras bibliotecas utilizadas

Além do próprio Angular (`@angular/core`, `@angular/common`, `@angular/forms`, `@angular/router`) e do RxJS (`rxjs`), a única biblioteca de terceiros adicionada foi o **Angular Material** (`@angular/material` + `@angular/cdk`) — não foram usadas bibliotecas de utilitário adicionais (como lodash) nem bibliotecas de state management externas (o `BehaviorSubject` dos stores já resolveu a necessidade de estado compartilhado sem precisar de NgRx/Akita).

## 4. Biblioteca de componentes visuais

**Angular Material**, usada de forma consistente em toda a aplicação:
- `MatTable` — listagens de produtos e notas fiscais, e itens de uma nota.
- `MatFormField` / `MatInput` / `MatSelect` — formulários de produto e de nota fiscal (Reactive Forms).
- `MatButton` / `MatIconButton` — ações em geral (criar, editar, excluir, imprimir, voltar).
- `MatDialog` — confirmação de exclusão de produto (`ConfirmDialogComponent`).
- `MatSnackBar` — feedback de sucesso/erro (`NotificationService`), tanto local (por componente) quanto global (via `errorInterceptor`).
- `MatProgressSpinner` — indicador de carregamento nas listas, formulários e especificamente no botão "Imprimir" durante o processamento.
- `MatCard`, `MatToolbar`, `MatIcon`, `MatDivider` — estrutura visual geral e navegação (incluindo o botão "Home" na toolbar).

Escolhida por ser a biblioteca oficial do Angular, com componentes nativamente standalone (sem necessidade de `NgModule`), cobrindo toda a necessidade da aplicação sem exigir uma segunda biblioteca de UI.

## 5. Gerenciamento de dependências

**Não se aplica Golang** — o backend foi implementado inteiramente em **C#/.NET**, não em Go. O gerenciamento de dependências do backend é feito via **NuGet**, com os pacotes declarados em cada `.csproj` (`PackageReference`), e o compartilhamento de código entre os dois microsserviços feito via `ProjectReference` para a biblioteca `Korp.Shared` — não há acoplamento em tempo de execução entre os serviços, apenas em tempo de compilação. No frontend, o gerenciamento de dependências é feito via **npm** (`package.json`), com apenas o Angular Material como dependência de terceiros além do próprio framework.

## 6. Frameworks utilizados no C#

- **ASP.NET Core Web API** (`.NET 10`, `net10.0`), estilo controller-based (`[ApiController]` + `[Route]`), não minimal API.
- **Entity Framework Core** (`Microsoft.EntityFrameworkCore.Sqlite` + `.Design`) — ORM e migrations para persistência em SQLite, um banco físico por serviço (`estoque.db`, `faturamento.db`).
- **Microsoft.Extensions.Http.Resilience** (baseado em Polly v8) — resiliência na comunicação HTTP entre o Faturamento e o Estoque: retry com backoff exponencial, circuit breaker e timeout, configurados via `AddStandardResilienceHandler` em `Faturamento.Api/Program.cs`.

## 7. Tratamento de erros e exceções no backend

Implementado de forma centralizada e consistente nos dois serviços, via a biblioteca compartilhada `Korp.Shared`:

- **Hierarquia de exceções de domínio** (`Shared/Korp.Shared/Exceptions/`): `DomainException` (abstrata, carrega `StatusCode` e `Title`) → `NotFoundException` (404), `ConflictException` (409, base) → `InvalidInvoiceStateException` e `InsufficientStockException` (esta última carrega a lista de itens com saldo insuficiente como uma extensão do `ProblemDetails`).
- **`ApiExceptionHandler : IExceptionHandler`** (`Shared/Korp.Shared/Web/ApiExceptionHandler.cs`), registrado nos dois serviços via `AddExceptionHandler<T>()` + `AddProblemDetails()` + `app.UseExceptionHandler()`, converte qualquer `DomainException` lançada pela camada de serviço em uma resposta padronizada **RFC 7807 (`ProblemDetails`)**, com `status`, `title` e `detail` consistentes.
- O **Faturamento.Api** registra adicionalmente o `EstoqueUnavailableExceptionHandler` (`Services/Faturamento.Api/Web/`), que intercepta especificamente as exceções que o Polly propaga quando a chamada ao Estoque falha (`BrokenCircuitException`, `TimeoutRejectedException`, `HttpRequestException`) e as converte em uma resposta **503** com mensagem amigável ("Serviço de estoque está indisponível no momento..."), ao invés de deixá-las estourar como erro 500 genérico. Esse handler é registrado **antes** do `ApiExceptionHandler` para ter prioridade.
- No frontend, o `errorInterceptor` (`Frontend/src/app/core/interceptors/error.interceptor.ts`) lê o corpo `ProblemDetails` de qualquer resposta de erro HTTP e exibe uma notificação (`MatSnackBar`) com a mensagem apropriada, mapeando os principais códigos de status (409, 503, 404) para mensagens específicas.

## 8. Uso de LINQ

Sim, LINQ é usado extensivamente na camada de serviço de ambos os microsserviços, tanto para consultas quanto para atualizações em lote. Exemplos concretos:

- `ProductService.ListAsync` (`Services/Estoque.Api/Services/ProductService.cs`) — `Where` + `OrderBy` sobre `IQueryable<Product>`, com filtro opcional por código/descrição.
- `ProductService.GetByIdsAsync` — `Where(p => ids.Contains(p.Id))` para busca em lote de produtos por uma lista de ids (usado pelo Faturamento ao criar uma nota).
- `ProductService.DebitBatchAsync` — o núcleo da lógica de concorrência: `_db.Products.Where(p => p.Id == item.ProductId && p.Balance >= item.Quantity).ExecuteUpdateAsync(...)`, um **UPDATE atômico guardado** expresso via LINQ, que só afeta a linha se o saldo for suficiente — o mecanismo que garante corretude sob concorrência (ver seção de tratamento de concorrência abaixo).
- `InvoiceService.CreateAsync` / `ListAsync` — `Select`, `Distinct()`, `ToDictionary`, `Include`/`ThenInclude` (para carregar os itens da nota junto com a nota), `OrderBy` por status/número.
- `InvoiceService.NextInvoiceNumberAsync` — outro `ExecuteUpdateAsync` guardado, sobre a linha única de `InvoiceCounters`, para gerar a numeração sequencial sem risco de corrida (evita o padrão inseguro `MAX(Number) + 1`).

---

## Requisitos obrigatórios — como foram atendidos

### Arquitetura de microsserviços
Dois serviços independentes (`Estoque.Api`, `Faturamento.Api`), cada um com seu próprio banco SQLite físico, comunicando-se exclusivamente via HTTP (sem acesso direto a banco entre eles). O Faturamento nunca acessa o banco do Estoque diretamente — ele guarda um snapshot (`ProductCode`/`ProductDescription`) capturado no momento da criação da nota, e consulta o Estoque via API para validar produtos e debitar saldo.

### Tratamento de falhas
Cenário demonstrável: ao derrubar o `Estoque.Api` e tentar imprimir uma nota, o `Faturamento.Api`:
1. Tenta a chamada HTTP algumas vezes (retry com backoff exponencial via Polly);
2. Após esgotar as tentativas, abre o circuito (circuit breaker) para falhar rápido em chamadas subsequentes;
3. Retorna **503** com mensagem clara ao cliente, sem nunca marcar a nota como "Fechada" (a nota só é fechada *depois* da confirmação do débito no Estoque — se a chamada falha, a exceção interrompe o método antes dessa linha, sem necessidade de rollback manual);
4. Ao religar o Estoque, uma nova tentativa de impressão funciona normalmente e debita o estoque **exatamente uma vez**, graças à idempotência (ver abaixo) — mesmo que uma tentativa anterior tenha alcançado parcialmente o Estoque antes de falhar.

### Conexão real com banco de dados
Cada serviço persiste em um arquivo SQLite físico próprio (`estoque.db`, `faturamento.db`), gerenciado via EF Core Migrations (não apenas em memória).

---

## Requisitos opcionais implementados

### Tratamento de concorrência
Cenário: produto com saldo 1 sendo debitado por duas notas impressas quase simultaneamente. Resolvido no `ProductService.DebitBatchAsync` via um **UPDATE atômico guardado** (`WHERE Balance >= quantidade`, checando o número de linhas afetadas) em vez de um "ler-depois-escrever" ingênuo — apenas uma das duas requisições consegue debitar (linhas afetadas = 1), a outra recebe `0` linhas afetadas e é tratada como saldo insuficiente (`409 Conflict`), sem nunca deixar o saldo ficar negativo. Esse padrão é correto mesmo em um banco com múltiplos escritores reais, não é uma particularidade do SQLite.

### Idempotência
O endpoint `POST /products/debit-batch` do Estoque exige um header `Idempotency-Key`. O Faturamento sempre envia uma chave determinística por nota (`invoice-{id}`), não por tentativa. O Estoque mantém uma tabela `ProcessedRequests` com essa chave como índice único: se a mesma chave chegar de novo (por causa de um retry após falha de rede, por exemplo), o resultado já processado é devolvido sem debitar o estoque novamente. Isso cobre o cenário de "o débito foi aplicado no Estoque, mas o Faturamento caiu antes de salvar o status Fechada" — o retry do `print` é seguro.

### Uso de Inteligência Artificial
**Não implementado nesta versão** — avaliado e deliberadamente adiado por decisão de escopo, priorizando os requisitos obrigatórios e os dois outros opcionais (concorrência e idempotência).

---

## Como executar o projeto

```
# Backend (2 terminais)
dotnet run --project Services/Estoque.Api       # http://localhost:5100
dotnet run --project Services/Faturamento.Api   # http://localhost:5200

# Frontend (1 terminal, dentro de Frontend/)
npm install
ng serve                                        # http://localhost:4200
```
