# Projeto Cloud

API em .NET 10 (Minimal API + EF Core) hospedada no **Render**, com banco PostgreSQL no **Neon**.

## Estrutura

```
Dockerfile               -> build da imagem usada pelo Render
render.yaml              -> blueprint do serviço no Render
src/ProjetoCloud.Api/
  Program.cs             -> endpoints, configuração de porta e banco
  Data/                  -> DbContext e conversão da URL do Neon
  Models/                -> entidades
  Migrations/            -> migrations do EF Core (aplicadas ao iniciar)
```

## 1. Banco no Neon

1. Crie um projeto em https://console.neon.tech
2. Em **Connection Details**, copie a connection string, algo como:
   `postgresql://usuario:senha@ep-xxxx.sa-east-1.aws.neon.tech/neondb?sslmode=require`

A API aceita essa URL diretamente na variável `DATABASE_URL`.

## 2. Rodar localmente

```powershell
dotnet user-secrets init --project src/ProjetoCloud.Api
dotnet user-secrets set "ConnectionStrings:Default" "postgresql://..." --project src/ProjetoCloud.Api
dotnet run --project src/ProjetoCloud.Api
```

As migrations (tabela `tarefas`) só são aplicadas ao subir se a variável `RUN_MIGRATIONS=true` estiver definida. Teste em `http://localhost:<porta>/tarefas`.

## 3. Deploy no Render

1. Suba o projeto para um repositório no GitHub.
2. No Render: **New > Blueprint** e selecione o repositório (ele lê o `render.yaml`).
   - Ou **New > Web Service**, runtime **Docker**.
3. Defina a variável de ambiente `DATABASE_URL` com a connection string do Neon.
4. Deploy. Health check em `/health`.

## Endpoints

| Método | Rota            | Descrição          |
|--------|-----------------|--------------------|
| GET    | `/`             | Página com o botão "Testar conexão" |
| GET    | `/api/db/status`| Testa a conexão com o Neon |
| GET    | `/health`       | Health check (não depende do banco) |
| GET    | `/tarefas`      | Lista tarefas      |
| GET    | `/tarefas/{id}` | Busca por id       |
| POST   | `/tarefas`      | Cria tarefa        |
| PUT    | `/tarefas/{id}` | Atualiza tarefa    |
| DELETE | `/tarefas/{id}` | Remove tarefa      |

## Nova migration

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add NomeDaMigration --project src/ProjetoCloud.Api
```
