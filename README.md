# Maxi Massas Itápolis — Sistema de Gestão (PIM IV)

Ecossistema da franquia Maxi Massas Itápolis: **Sistema Web de Gestão** (proprietários),
**API** (consumida também pelo aplicativo Flutter dos clientes) e **banco SQL Server**.
O backend foi originalmente desenvolvido em Razor Pages (PIM III) e foi reorganizado
no modelo em cinco projetos exigido pelo Manual do PIM IV (Etapa 4).

## Estrutura

```
MaxiMassas.sln
├── MaxiMassas.Modelos       entidades, DTOs (Request/Response), validadores, enums
├── MaxiMassas.Repositorio   AppDbContext (EF Core), repositórios, Dapper, Migrations
├── MaxiMassas.Negocio       regras de negócio, autenticação, e-mail, relatórios, backup
├── MaxiMassas.Web           ASP.NET Core MVC: painel administrativo
└── MaxiMassas.Api           ASP.NET Core Web API: BasicAuth (painel) e /api/app/* (app)
```

Referências: `Web/Api → Negocio → Repositorio → Modelos`. Web e Api não acessam o banco
nem o Repositório diretamente (a única exceção é o registro de serviços no `Program.cs`).

## Requisitos do manual atendidos

| Requisito | Onde |
|---|---|
| Trigger (`HasTrigger`) | `TR_Clientes_DataAtualizacao`, na migration `Inicial` e no `AppDbContext` |
| Stored procedure executada pelo sistema (Dapper) | `sp_Clientes_BuscarPorNome`, chamada em `ClienteRepositorio.SearchByNomeAsync` |
| Injeção de dependência | `Program.cs` da Web e da Api (blocos idênticos); ver `INJECAO-DE-DEPENDENCIA.md` |
| Connection string fora do código | chave `Padrao` em `appsettings`/User Secrets |
| OpenAPI | Api: `/openapi/v1.json` e Scalar em `/scalar` (Development) |

## Como executar (Windows, SQL Server Express)

1. Guarde a connection string nos User Secrets (nos dois projetos):
   ```
   dotnet user-secrets set "ConnectionStrings:Padrao" "Server=localhost\SQLEXPRESS;Database=MaxiMassas;Trusted_Connection=True;TrustServerCertificate=True" --project MaxiMassas.Api
   dotnet user-secrets set "ConnectionStrings:Padrao" "Server=localhost\SQLEXPRESS;Database=MaxiMassas;Trusted_Connection=True;TrustServerCertificate=True" --project MaxiMassas.Web
   ```
2. Se já existir um banco antigo (esquema do modelo-base), recrie-o:
   ```
   dotnet ef database drop --project MaxiMassas.Repositorio --startup-project MaxiMassas.Api
   dotnet ef database update --project MaxiMassas.Repositorio --startup-project MaxiMassas.Api
   ```
3. Execute `MaxiMassas.Web` e/ou `MaxiMassas.Api`. Na primeira execução o `DataSeeder`
   cria o usuário administrador **admin / admin123** (troque a senha após o primeiro acesso).

`Banco:AplicarMigrationsAoIniciar` (appsettings) controla se as migrations e o seeder
rodam ao iniciar; use `false` para subir o site sem banco (apenas testes de rota).

## Notas de segurança (a tratar antes de produção)

- Senha inicial do administrador é conhecida; alterar no primeiro acesso.
- CORS da Api está como `AllowAll`; restringir ao(s) domínio(s) do app em produção.
- Pagamentos do app são presenciais (decisão do PIM III); não há dados de cartão.

## Documentação complementar

- `INJECAO-DE-DEPENDENCIA.md`: como registrar uma nova entidade nas camadas.
- Documentos do PIM (manual, PIM III, guia de banco) estão no Project do grupo.
