using FluentValidation;
using FluentValidation.AspNetCore;
using MaxiMassas.Api.Middleware;
using MaxiMassas.Api.Seguranca;
using MaxiMassas.Modelos.Validadores;
using MaxiMassas.Negocio;
using MaxiMassas.Negocio.Integracoes;
using MaxiMassas.Repositorio;
using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// A connection string vem do appsettings.json (ou de User Secrets) — nunca do código-fonte.
var connectionString = builder.Configuration.GetConnectionString("Padrao")
    ?? throw new InvalidOperationException("Connection string 'Padrao' não encontrada no appsettings.json.");

// ---------- Injeção de dependência ----------
// LISTA DE MONTAGEM do sistema: cada linha ensina o ASP.NET Core a criar uma peça.
// Cadeia típica: Controller -> *Negocio -> I*Repositorio -> AppDbContext (connection string).
// Explicação passo a passo: INJECAO-DE-DEPENDENCIA.md, na pasta da solution.
//
// ATENÇÃO: este bloco é IGUAL no Program.cs da Web. Mudou aqui, mude lá também.

// 1) O banco
builder.Services.AddDbContext<AppDbContext>(opcoes => opcoes.UseSqlServer(connectionString));

// 2) Repositórios (camada Repositorio)
builder.Services.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();
builder.Services.AddScoped<IVendaRepositorio, VendaRepositorio>();
builder.Services.AddScoped<IEstoqueRepositorio, EstoqueRepositorio>();
builder.Services.AddScoped<IConsumoProprioRepositorio, ConsumoProprioRepositorio>();
builder.Services.AddScoped<IRelatorioRepositorio, RelatorioRepositorio>();
builder.Services.AddScoped<IRedefinicaoSenhaRepositorio, RedefinicaoSenhaRepositorio>();
builder.Services.AddScoped<IBackupRepositorio, BackupRepositorio>();
builder.Services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

// 3) Classes de negócio (camada Negocio)
builder.Services.AddScoped<IUsuarioNegocio, UsuarioNegocio>();
builder.Services.AddScoped<IClienteNegocio, ClienteNegocio>();
builder.Services.AddScoped<IProdutoNegocio, ProdutoNegocio>();
builder.Services.AddScoped<IEstoqueNegocio, EstoqueNegocio>();
builder.Services.AddScoped<IVendaNegocio, VendaNegocio>();
builder.Services.AddScoped<IVariacaoProdutoNegocio, VariacaoProdutoNegocio>();
builder.Services.AddScoped<IConsumoProprioNegocio, ConsumoProprioNegocio>();
builder.Services.AddScoped<IRelatorioNegocio, RelatorioNegocio>();
builder.Services.AddScoped<IExportacaoNegocio, ExportacaoNegocio>();
builder.Services.AddScoped<IAuthNegocio, AuthNegocio>();
builder.Services.AddScoped<IAutenticacaoNegocio, AutenticacaoNegocio>();
builder.Services.AddScoped<IAppClienteNegocio, AppClienteNegocio>();
builder.Services.AddScoped<IConfiguracaoEntregaNegocio, ConfiguracaoEntregaNegocio>();

// 4) Serviços de apoio (e-mail, foto, ViaCEP)
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IFotoPerfilService, FotoPerfilService>();
builder.Services.AddScoped<IViaCepService, ViaCepService>();
var viacepTimeout = builder.Configuration.GetValue<int>("ViaCep:TimeoutSeconds", 10);
builder.Services.AddHttpClient("ViaCep", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ViaCep:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(viacepTimeout);
});

// 5) AutoMapper e validações (FluentValidation)
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ClienteRequestValidator>();
// --------------------------------------------

// Autenticação: Basic Auth para usuários internos e esquema próprio para clientes do app (/api/app).
builder.Services.AddAuthentication(BasicAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>(BasicAuthHandler.SchemeName, null)
    .AddScheme<AuthenticationSchemeOptions, ClienteAuthHandler>(ClienteAuthHandler.SchemeName, null);
builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

// OpenAPI: documento JSON padrão em /openapi/v1.json — é o que o grupo mostra ao app Flutter.
builder.Services.AddOpenApi();

// CORS liberado para desenvolvimento; restringir as origens em produção.
builder.Services.AddCors(o => o.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Aplica as migrations e cria o administrador padrão se o banco estiver vazio.
using (var escopo = app.Services.CreateScope())
{
    var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = escopo.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DataSeeder.SeedAsync(db, logger);
}

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Scalar: tela gráfica para ver e testar cada endpoint, em /scalar.
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
