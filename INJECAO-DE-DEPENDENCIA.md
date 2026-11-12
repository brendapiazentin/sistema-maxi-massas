# Injeção de dependência — o que é e como usar no PIM

O manual do PIM IV exige: *"as dependências entre as camadas devem ser configuradas por injeção de dependência no arquivo Program.cs dos projetos Web e Api"*. Este guia explica o que isso quer dizer, partindo do que você já sabe de POO, e mostra exatamente o que escrever quando criar uma entidade nova.

> **Nota sobre o projeto Maxi Massas:** os exemplos usam `Cliente` e `Ingresso` para ilustrar. No sistema real, o bloco completo (repositórios, negócios, serviços, AutoMapper e FluentValidation) está em `MaxiMassas.Web/Program.cs` e `MaxiMassas.Api/Program.cs`, e é idêntico nos dois. Duas observações sobre o sistema real: (1) as classes de negócio são registradas **pela interface** (`IClienteNegocio`), e os controllers pedem a interface; (2) o bloco também registra o repositório genérico `IRepositorio<>`, a `IUnidadeDeTrabalho` e serviços de apoio (e-mail, fotos, ViaCEP, Telegram). Os construtores reais costumam receber mais de uma dependência; nos exemplos abaixo mostramos só a parte que importa para entender a ideia.

## 1. O que você já sabe: uma classe que precisa de outra

Em POO você já viu composição: uma classe que *tem* outra como campo. No PIM, cada camada precisa da camada de baixo para trabalhar:

- o `ClienteController` precisa de um `IClienteNegocio` para aplicar as regras;
- o `ClienteNegocio` (que implementa `IClienteNegocio`) precisa de um `IClienteRepositorio` para ler e gravar;
- o `ClienteRepositorio` precisa de um `AppDbContext` para falar com o banco;
- o `AppDbContext` precisa da connection string para saber qual banco abrir.

Cada uma dessas classes recebe a outra **no construtor**. Abra `MaxiMassas.Negocio\ClienteNegocio.cs` e veja:

```
public ClienteNegocio(
    IClienteRepositorio repo,
    IViaCepService viacep,
    IMapper mapper,
    ILogger<ClienteNegocio> logger)
{
    _repo = repo;
    _viacep = viacep;
    _mapper = mapper;
    _logger = logger;
}
```

Isso você já conhece: um construtor com parâmetros, que guarda cada valor num campo. A pergunta é: **quem chama esse construtor, e de onde vêm o repositório, o serviço de CEP, o mapper e o logger?**

## 2. O jeito sem injeção de dependência (e por que não serve)

Sem injeção de dependência, cada controller teria de montar a cadeia inteira na mão, com `new`, de baixo para cima (versão simplificada, só com o repositório):

```
// NÃO FAÇA ASSIM — é só para entender o problema
public IActionResult Index()
{
    var opcoes = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer("Server=localhost;Database=MaxiMassas;Trusted_Connection=True;TrustServerCertificate=True")
        .Options;
    var contexto    = new AppDbContext(opcoes);
    var repositorio = new ClienteRepositorio(contexto);
    var negocio     = new ClienteNegocio(repositorio /* e ainda viacep, mapper, logger... */);

    var clientes = negocio.GetAllAsync();
    ...
}
```

Isso funciona, mas cria três problemas:

1. **A connection string se espalha pelo código.** Ela apareceria em todo controller, de toda entidade, nos dois projetos. O manual proíbe: ela tem de ficar no `appsettings.json`. E no dia em que o servidor mudar de nome, você teria de caçar todas as cópias.
2. **A Web e a Api passam a mexer no Repositório.** O controller estaria criando um `ClienteRepositorio` e um `AppDbContext`, o que quebra a regra do manual: *"os projetos Web e Api não devem acessar o banco de dados nem a camada de Repositório diretamente"*.
3. **Ninguém fecha a conexão com o banco.** O contexto foi criado e nunca é descartado. Com muitos acessos, as conexões se acumulam até o SQL Server recusar novas.

## 3. A ideia: pedir em vez de criar

A injeção de dependência inverte quem faz o trabalho. Em vez de cada classe **criar** o que precisa, ela só **pede** no construtor. Quem cria e entrega é o ASP.NET Core, seguindo uma lista que você escreve **uma única vez** no `Program.cs`.

Pense numa lanchonete. O atendente anota "um X-burguer" e não vai até a cozinha fritar o hambúrguer. A cozinha tem a receita: um X-burguer leva pão, hambúrguer e queijo; o hambúrguer leva carne e tempero. O atendente só pede, e o lanche chega montado. No PIM, o controller é o atendente, o `Program.cs` é o livro de receitas, e o ASP.NET Core é a cozinha.

O controller fica assim, sem nenhum `new`:

```
public class ClienteController : ControllerBase
{
    private readonly IClienteNegocio _svc;

    public ClienteController(IClienteNegocio svc)   // só pede (a interface)
    {
        _svc = svc;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<IEnumerable<ClienteResponse>>.Ok(await _svc.GetAllAsync()));   // e usa
}
```

## 4. A lista de montagem, linha por linha

No `Program.cs` da Web e no da Api existe este bloco (igual nos dois):

```
builder.Services.AddDbContext<AppDbContext>(opcoes => opcoes.UseSqlServer(connectionString));
builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IClienteNegocio, ClienteNegocio>();
```

Leia cada linha como uma frase:

| Linha                                                            | Em português                                                                                                                        |
| ---------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `AddDbContext<AppDbContext>(... UseSqlServer(connectionString))` | Quando alguém pedir um `AppDbContext`, crie um que use SQL Server com a connection string do `appsettings.json`.                    |
| `AddScoped<IClienteRepositorio, ClienteRepositorio>()`           | Quando alguém pedir um `IClienteRepositorio` (a interface), entregue um `ClienteRepositorio` (a classe que implementa a interface). |
| `AddScoped<IClienteNegocio, ClienteNegocio>()`                   | Quando alguém pedir um `IClienteNegocio` (a interface), entregue um `ClienteNegocio` (a classe que implementa a interface).         |

As duas últimas linhas são polimorfismo, que você já viu em POO: o controller declara que precisa de *algo que cumpra o contrato* `IClienteNegocio`, e o `Program.cs` decide qual classe concreta cumpre esse contrato. Se um dia o grupo trocar o Entity Framework por Dapper, escreve outra classe que implementa a mesma interface e muda só a linha do repositório; o `ClienteNegocio` nem fica sabendo.

**Por que dois `Program.cs`?** Porque a Web e a Api são dois programas separados: cada um sobe sozinho e tem a sua própria lista de montagem. Por isso o bloco é igual nos dois, e toda linha nova vai para os dois.

**O que é `AddScoped`?** Diz *quanto tempo* o objeto vive. *Scoped* significa um objeto novo para cada requisição (cada clique na Web, cada chamada da Api), jogado fora quando a resposta sai. O `AddDbContext` funciona do mesmo jeito, e é ao jogar o contexto fora que a conexão com o banco é fechada — o problema 3 da seção 2 se resolve sozinho. Existem também `AddSingleton` (um objeto só para o sistema inteiro) e `AddTransient` (um novo a cada pedido); no sistema real o `AddSingleton` é usado só em serviços sem estado, como o de fotos. Para repositórios e classes de negócio, use sempre `AddScoped`.

## 5. O que acontece quando o usuário abre a tela de clientes

1. O navegador (ou o app) pede `/api/cliente`.
2. O ASP.NET Core vê que precisa criar um `ClienteController` e olha o construtor dele: ele pede um `IClienteNegocio`.
3. Procura `IClienteNegocio` na lista do `Program.cs`. Acha: entregue um `ClienteNegocio`. Olha o construtor dele: pede um `IClienteRepositorio` (e também ViaCEP, mapper e logger, que também estão na lista).
4. Procura `IClienteRepositorio` na lista. A lista diz: entregue um `ClienteRepositorio`. Olha o construtor dele: pede um `AppDbContext`.
5. Procura `AppDbContext`. A lista diz como criar, com a connection string.
6. Agora monta de baixo para cima: contexto → repositório → negócio → controller. E chama a ação (`GetAll`).
7. A resposta sai, e tudo o que foi criado para esta requisição é descartado.

Você não escreve nada disso. Escreve só os construtores e as linhas do `Program.cs`.

## 6. Receita: criando a entidade `Ingresso`

Sempre que criar uma entidade nova, os construtores seguem o mesmo molde do Cliente:

```
// MaxiMassas.Repositorio\IngressoRepositorio.cs
public class IngressoRepositorio : IIngressoRepositorio
{
    private readonly AppDbContext _contexto;

    public IngressoRepositorio(AppDbContext contexto)        // pede o banco
    {
        _contexto = contexto;
    }
    ...
}

// MaxiMassas.Negocio\IIngressoNegocio.cs  (o contrato: a lista de operações)
public interface IIngressoNegocio
{
    Task<IEnumerable<IngressoResponse>> GetAllAsync();
    ...
}

// MaxiMassas.Negocio\IngressoNegocio.cs
public class IngressoNegocio : IIngressoNegocio
{
    private readonly IIngressoRepositorio _repositorio;

    public IngressoNegocio(IIngressoRepositorio repositorio)  // pede o repositório
    {
        _repositorio = repositorio;
    }
    ...
}

// MaxiMassas.Api\Controllers\IngressoController.cs  (e o equivalente na Web)
public class IngressoController : ControllerBase
{
    private readonly IIngressoNegocio _svc;

    public IngressoController(IIngressoNegocio svc)          // pede o negócio (interface)
    {
        _svc = svc;
    }
    ...
}
```

E acrescente **duas linhas em cada `Program.cs`** (o da Web e o da Api), logo abaixo das do Cliente:

```
builder.Services.AddScoped<IIngressoRepositorio, IngressoRepositorio>();
builder.Services.AddScoped<IIngressoNegocio, IngressoNegocio>();
```

O `AddDbContext` não se repete: o mesmo `AppDbContext` serve a todas as entidades. O que muda nele é ganhar o `DbSet<Ingresso>`.

Se o negócio de Ingresso precisar consultar clientes (por exemplo, para saber se o comprador está ativo), é só pedir os dois no construtor:

```
public IngressoNegocio(IIngressoRepositorio repositorio, IClienteRepositorio clientes)
```

Como os dois já estão na lista, o ASP.NET entrega os dois.

## 7. Os erros que você vai ver

As mensagens abaixo seguem o formato real do ASP.NET Core, com os nomes das classes do projeto.

**Esqueceu de registrar a classe de negócio** (a linha `AddScoped<IIngressoNegocio, IngressoNegocio>()`). O sistema sobe, mas a tela ou o endpoint devolve erro 500 com:

```
InvalidOperationException: Unable to resolve service for type
'MaxiMassas.Negocio.IClienteNegocio' while attempting to activate
'MaxiMassas.Api.Controllers.ClienteController'.
```

Leia assim: *"não consegui criar o ClienteController, porque ele pediu um IClienteNegocio e ninguém me ensinou a entregar um"*. Correção: a linha que falta no `Program.cs` — e confira se ela está nos **dois**.

**Esqueceu de registrar o repositório** (a linha `AddScoped<IIngressoRepositorio, IngressoRepositorio>()`). Aqui o sistema nem chega a subir: o `dotnet run` ou o F5 para na hora com:

```
System.AggregateException: Some services are not able to be constructed
(... Unable to resolve service for type 'MaxiMassas.Repositorio.Interfaces.IClienteRepositorio'
while attempting to activate 'MaxiMassas.Negocio.ClienteNegocio'.)
```

O ASP.NET confere a lista ao iniciar e percebe que o negócio pede algo que não está nela. Correção: a linha do repositório nos dois `Program.cs`.

**Escreveu `new ClienteNegocio(...)` no controller.** Dá trabalho e quebra a regra do manual: para criar o negócio você teria de criar também o repositório, o contexto e os demais serviços, e voltaria ao problema da seção 2. Peça a interface no construtor do controller.

**Registrou a classe sem a interface** (`AddScoped<ClienteNegocio>()` ou `AddScoped<ClienteRepositorio>()`). O controller pede `IClienteNegocio` (e o negócio pede `IClienteRepositorio`), que não estão na lista, e dá o mesmo erro dos casos acima. A linha precisa dizer as duas coisas: `AddScoped<IClienteNegocio, ClienteNegocio>()`.

## 8. Resumo para colar no caderno

- Toda classe **pede** o que precisa no construtor (de preferência pela interface) e **nunca** dá `new` em controller, negócio, repositório ou contexto.
- O `Program.cs` é a lista de montagem. Cada entidade nova = **duas linhas** (repositório e negócio), nos **dois** `Program.cs`.
- A linha sempre diz `<Interface, Classe>`.
- Erro com *Unable to resolve service for type 'X'* = falta registrar `X` no `Program.cs`.
