# Maxi Massas Itápolis — Sistema de Gestão (PIM IV)

Ecossistema da franquia Maxi Massas Itápolis: **Sistema Web de Gestão** (proprietários), **API** (consumida pelo aplicativo Flutter dos clientes), **aplicativo Flutter** (clientes fazem pedidos) e **banco SQL Server**. O backend foi originalmente desenvolvido em Razor Pages (PIM III) e foi reorganizado no modelo em cinco projetos exigido pelo Manual do PIM IV (Etapa 4).

## Estrutura

```
MaxiMassas.sln
├── MaxiMassas.Modelos       entidades, DTOs (Request/Response), validadores, enums
├── MaxiMassas.Repositorio   AppDbContext (EF Core), repositórios, Dapper, Migrations
├── MaxiMassas.Negocio       regras de negócio, autenticação, e-mail, relatórios, backup
├── MaxiMassas.Web           ASP.NET Core MVC: painel administrativo
└── MaxiMassas.Api           ASP.NET Core Web API: BasicAuth (painel) e /api/app/* (app)

MaxiMassas.App/              aplicativo Flutter dos clientes (consome /api/app/*; fora da solution .NET)
deploy/                      scripts de publicação e instalação no servidor
docs/                        contrato da API para o aplicativo Flutter
```

Referências: `Web/Api → Negocio → Repositorio → Modelos`. Web e Api não acessam o banco nem o Repositório diretamente (a única exceção é o registro de serviços no `Program.cs`).

## Requisitos do manual atendidos

| Requisito                                        | Onde                                                                             |
|--------------------------------------------------|----------------------------------------------------------------------------------|
| Trigger (`HasTrigger`)                           | `TR_Clientes_DataAtualizacao`, na migration `Inicial` e no `AppDbContext`        |
| Stored procedure executada pelo sistema (Dapper) | `sp_Clientes_BuscarPorNome`, chamada em `ClienteRepositorio.SearchByNomeAsync`   |
| Injeção de dependência                           | `Program.cs` da Web e da Api (blocos idênticos); ver `INJECAO-DE-DEPENDENCIA.md` |
| Connection string fora do código                 | chave `Padrao` em `appsettings`/User Secrets                                     |
| OpenAPI                                          | Api: `/openapi/v1.json` e Scalar em `/scalar` (Development)                      |

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
3. Execute `MaxiMassas.Web` e/ou `MaxiMassas.Api`. Na primeira execução (banco vazio) o `DataSeeder` cria o usuário administrador **admin / admin123**; troque a senha após o primeiro acesso.

`Banco:AplicarMigrationsAoIniciar` (appsettings) controla se as migrations e o seeder rodam ao iniciar; use `false` para subir o site sem banco (apenas testes de rota).

## Hospedagem (AWS)

Ambiente de demonstração em produção:

| Componente        | Serviço                                          | Observação                                                                                         |
|-------------------|--------------------------------------------------|----------------------------------------------------------------------------------------------------|
| Banco             | Amazon RDS, SQL Server Express (`db.t3.micro`)   | Criado e atualizado por `dotnet ef database update`                                                |
| Web e Api         | Amazon EC2 (Ubuntu 24.04, `t3.micro`)            | Publicação auto-contida para `linux-x64`; serviços `systemd`; proxy Nginx                          |
| Endereço          | IP elástico + subdomínio DuckDNS + Let's Encrypt | `https://maximassasitapolis.duckdns.org` (Web) e `/api/app/*` (Api do app)                         |
| Fotos de produtos | Amazon S3                                        | Enviadas pelo Sistema Web e servidas ao app por `/api/app/produtos/{id}/foto`                      |
| Aplicativo        | Versão web e APK Android                         | Web em `/app/`; APK em `/downloads/maximassas.apk` (iPhone usa a versão web)                      |

Integrações externas: e-mail de recuperação de senha por SMTP de uma conta Gmail dedicada; consulta de CEP no ViaCEP; comanda de entrega e retirada enviada por bot do Telegram (identificadores configurados em *Configurações de entrega*).

Scripts em `deploy/`:

- `publicar.sh IP chave.pem`: publica Web e Api e envia ao servidor.
- `servidor-instalar.sh` (no servidor): instala Nginx e cria os serviços.
- `servidor-https.sh DOMINIO EMAIL` (no servidor): emite o certificado e configura HTTPS.

A connection string do servidor fica em `/etc/maximassas/*.env`, fora do código. O backup automático do sistema (`BackupService`) grava em disco local e não se aplica ao RDS, que mantém seu próprio backup automático diário (janela às 00h de Brasília, retenção de 1 dia).

## Escopo e decisões do cliente

- Atendimento somente em Itápolis, com perfil de acesso único (administrador).
- Pagamentos do app são presenciais (na entrega ou na retirada); não há dados de cartão.
- Notificações push, pagamento no app, avaliação de pedidos e autenticação por token (JWT) estão fora do escopo, por decisão dos proprietários.

## Notas de segurança

- A senha do administrador em produção já foi alterada. Em um banco novo, o `DataSeeder` continua criando `admin / admin123`; troque no primeiro acesso.
- CORS da Api está como `AllowAll` (`Program.cs`); limitação conhecida a restringir às origens do app caso o sistema siga em uso após o PIM.
- A connection string e a senha de app do e-mail ficam fora do repositório (User Secrets no desenvolvimento e `/etc/maximassas/*.env` no servidor).

## Documentação complementar

- `INJECAO-DE-DEPENDENCIA.md`: como registrar uma nova entidade nas camadas.
- `docs/contrato-api-app-flutter.md`: contrato dos endpoints `/api/app/*` consumidos pelo aplicativo Flutter.
- Documentos do PIM (manual, PIM III, guia de banco) estão no Project do grupo.
