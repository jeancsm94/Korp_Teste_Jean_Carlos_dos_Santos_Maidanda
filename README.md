# Korp Teste — Sistema de Emissão de Notas Fiscais

Sistema de cadastro de produtos, notas fiscais e impressão (com baixa de estoque), implementado como **dois microsserviços em .NET** e um **frontend em Angular**, sem nenhuma dependência de código entre backend e frontend — cada parte tem seu próprio build, dependências e ciclo de execução.

## Estrutura do repositório

```
├── Services/
│   ├── Estoque.Api/          # Backend — microsserviço de estoque (porta 5100)
│   └── Faturamento.Api/      # Backend — microsserviço de faturamento (porta 5200)
├── Shared/
│   └── Korp.Shared/          # Backend — biblioteca .NET compartilhada (tratamento de erros)
├── Korp_Teste_...slnx        # Solução .NET (referencia só os 3 projetos acima)
│
├── Frontend/                 # Frontend — aplicação Angular (porta 4200), projeto npm independente
│
└── docs/
    ├── detalhamento-tecnico.md   # Detalhamento técnico exigido pelo teste
    └── detalhamento-tecnico.pdf  # Mesmo conteúdo, em PDF
```

**Backend** (`Services/`, `Shared/`) e **Frontend** (`Frontend/`) são projetos independentes dentro do mesmo repositório:
- A solução .NET (`.slnx`) não referencia nada do Angular.
- O `Frontend/package.json` não depende de nada do backend — ele apenas consome as APIs via HTTP, em runtime, através das URLs configuradas em `Frontend/src/environments/environment.ts`.
- Cada parte pode ser buildada, testada e rodada de forma totalmente independente (veja abaixo).

## Como rodar

### Backend (2 terminais, a partir da raiz do repositório)
```bash
dotnet run --project Services/Estoque.Api       # http://localhost:5100
dotnet run --project Services/Faturamento.Api   # http://localhost:5200
```

### Frontend (1 terminal, dentro de `Frontend/`)
```bash
cd Frontend
npm install
ng serve                                        # http://localhost:4200
```

O frontend espera o backend rodando em `localhost:5100`/`5200` (configurável em `environment.ts`); o backend não depende do frontend para funcionar — pode ser testado isoladamente via `curl` ou coleção HTTP de sua preferência.

## Documentação

- [`docs/detalhamento-tecnico.md`](docs/detalhamento-tecnico.md) ([PDF](docs/detalhamento-tecnico.pdf)) — arquitetura, ciclos de vida Angular, uso de RxJS, bibliotecas, frameworks C#, tratamento de erros e uso de LINQ.
- [`AGENTS.md`](AGENTS.md) — notas de arquitetura para desenvolvimento assistido por IA (Claude Code).
