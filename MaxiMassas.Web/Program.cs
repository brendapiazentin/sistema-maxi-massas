using FluentValidation;
using MaxiMassas.Modelos.Validadores;
using MaxiMassas.Negocio;
using MaxiMassas.Negocio.Integracoes;
using MaxiMassas.Repositorio;
using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using MaxiMassas.Web.Seguranca;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// A connection string vem do appsettings.json (ou de User Secrets) — nunca do código-fonte.
var connectionString = builder.Configuration.GetConnectionString("Padrao")
    ?? throw new InvalidOperationException("Connection string 'Padrao' não encontrada no appsettings.json.");

// ---------- Injeção de dependência ----------
// LISTA DE MONTAGEM do sistema: cada linha ensina o ASP.NET Core a criar uma peça.
// Cadeia típica: Controller -> *Negocio -> I*Repositorio -> AppDbContext (connection string).
// Explicação passo a passo: INJECAO-DE-DEPENDENCIA.md, na pasta da solution.
//
// ATENÇÃO: este bloco é IGUAL no Program.cs da Api. Mudou aqui, mude lá também.

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
builder.Services.AddSingleton<IFotoProdutoService, FotoProdutoService>();
builder.Services.AddScoped<IViaCepService, ViaCepService>();
builder.Services.AddScoped<ITelegramComandaService, TelegramComandaService>();
builder.Services.AddHttpClient("Telegram", client =>
{
    client.BaseAddress = new Uri("https://api.telegram.org/");
    client.Timeout = TimeSpan.FromSeconds(8);
});
// A URL da API do Telegram contem o token do bot: o HttpClient nao pode registra-la nos logs.
builder.Logging.AddFilter("System.Net.Http.HttpClient.Telegram", LogLevel.None);
var viacepTimeout = builder.Configuration.GetValue<int>("ViaCep:TimeoutSeconds", 10);
builder.Services.AddHttpClient("ViaCep", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ViaCep:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(viacepTimeout);
});

// 5) AutoMapper e validações (FluentValidation)
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddValidatorsFromAssemblyContaining<ClienteRequestValidator>();
// --------------------------------------------

// Backup automático do banco (RNF03): roda em segundo plano, só na Web, para não duplicar com a Api.
builder.Services.AddHostedService<BackupService>();

// Autenticação do painel: cookie de sessão criado em /Login.
builder.Services.AddPainelAuth();

// Telas (MVC). O token antifalsificação é exigido em todo POST.
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Acentos saem como texto ("Evolução"), não como entidades ("Evolu&#xE7;&#xE3;o").
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));

var app = builder.Build();

// Aplica as migrations e cria o administrador padrão se o banco estiver vazio.
// (Banco:AplicarMigrationsAoIniciar = false desliga isso, por exemplo para subir sem SQL Server.)
if (builder.Configuration.GetValue("Banco:AplicarMigrationsAoIniciar", true))
{
    using var escopo = app.Services.CreateScope();
    var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = escopo.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DataSeeder.SeedAsync(db, logger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Erro");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // wwwroot: CSS, JS, imagens e ícones do painel
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
