# Contrato da API para o aplicativo Flutter (Maxi Massas)

Última atualização: 12/11/2026.

**Endereço base (ambiente de demonstração na AWS):** `https://maximassasitapolis.duckdns.org/api/app`
Observação: é um subdomínio gratuito (DuckDNS) com IP fixo e certificado HTTPS válido (Let's Encrypt); não é preciso liberar HTTP simples no app.

## Autenticação
- Rotas marcadas como **[anônimo]** não exigem login.
- Rotas **[cliente]** usam **HTTP Basic**: cabeçalho `Authorization: Basic base64(email:senha)` do cliente. Não há token: o app reenvia as credenciais a cada chamada. `GET /api/app/me` serve como "login".
- Falha de credencial devolve **401** (sem o cabeçalho `WWW-Authenticate`).

## Formato de resposta
`{ "success": true, "message": "...", "data": ..., "errors": [] }`. Enums aceitam o nome (ex.: `"Entrega"`) ou o número.

## Erros
- **400**: violação de regra de negócio (por exemplo, estoque insuficiente, produto inativo, horário inválido). A mensagem explicativa vem em `message` e deve ser exibida ao cliente. A resposta não expõe o nome técnico da exceção.
- **401**: credencial inválida ou ausente nas rotas **[cliente]**.
- **404**: recurso inexistente (por exemplo, produto sem foto, CEP não encontrado, pedido de outro cliente).
- Erros inesperados do servidor devolvem 500 com mensagem genérica.

## Endpoints (prefixo `/api/app`)
| Método | Rota | Acesso | Função |
|---|---|---|---|
| POST | `/cadastro` | anônimo | Cria a conta; devolve o perfil |
| POST | `/senha/esqueci` | anônimo | Envia código de 6 dígitos por e-mail (válido por 15 min) |
| POST | `/senha/redefinir` | anônimo | E-mail, código e nova senha |
| GET | `/me` | cliente | Perfil e endereços (usado como login) |
| PUT | `/me` | cliente | Altera nome, e-mail e telefone |
| POST | `/enderecos` | cliente | Salva endereço novo |
| PUT | `/enderecos/{id}` | cliente | Altera endereço |
| DELETE | `/enderecos/{id}` | cliente | Remove endereço |
| PUT | `/enderecos/{id}/principal` | cliente | Define endereço principal |
| GET | `/entrega` | anônimo | Taxa de entrega e mínimo para entrega grátis |
| GET | `/horarios?tipo=Entrega\|Retirada` | anônimo | Horários disponíveis para o tipo informado |
| GET | `/cep/{cep}` | anônimo | Logradouro, bairro, cidade e UF (ViaCEP) |
| GET | `/produtos` | anônimo | Cardápio: **somente produtos ativos**, com preço, disponibilidade e foto |
| GET | `/produtos/{id}/foto` | anônimo | Imagem JPEG do produto (404 se não houver foto) |
| POST | `/pedidos` | cliente | Cria pedido (vira venda com origem "App") |
| GET | `/pedidos` | cliente | Histórico, mais recentes primeiro |
| GET | `/pedidos/{id}` | cliente | Um pedido, com status atualizado |

Toda operação de perfil e endereço devolve o **perfil completo atualizado**: o servidor decide o resultado (por exemplo, qual endereço é o principal) e o app apenas o reflete.

## Cardápio: `GET /produtos`
Cada item de `data`:
```json
{ "id": 3, "nome": "Pizza Calabresa", "descricao": "250 g", "preco": 39.9,
  "categoria": "Massas", "disponivel": true, "estoque": 12,
  "fotoUrl": "/api/app/produtos/3/foto?v=639000000000000000" }
```
- **Ativo x Inativo:** produto marcado como **Inativo** no Sistema Web **não vem** na lista; o app não deve oferecê-lo. É diferente de "Em falta".
- **Em falta:** produto ativo com estoque zero vem com `disponivel: false` e `estoque: 0`; o app o exibe esmaecido e sem permitir compra.
- **Pedido com produto inativo:** se o app ainda tiver um produto inativo em cache e o enviar em `POST /pedidos`, o servidor recusa (400) com a mensagem de que o produto não está mais disponível. Ao receber esse erro, o app deve recarregar o cardápio.
- **Foto:** `fotoUrl` é um **caminho relativo ao servidor**; a URL completa é `https://maximassasitapolis.duckdns.org` + `fotoUrl`. Vale `null` quando o produto não tem foto (o app mostra uma imagem padrão). O parâmetro `?v=` muda sempre que a foto é trocada, então o app pode guardar a imagem em cache por tempo longo usando a URL inteira como chave (o servidor responde com cache de longa duração, `immutable`). A imagem é JPEG de até 800 px no lado maior.
- Preços, estoque, nomes e fotos vêm do mesmo cadastro do Sistema Web; o app não guarda nada próprio do cardápio.

## Horários: `GET /horarios?tipo=Entrega|Retirada`
O app deve **oferecer apenas os horários devolvidos por esta rota**; ele não calcula janelas por conta própria.

**Formato da resposta:** `data` é uma lista de objetos com três campos de texto. O campo `valor` é o que deve ser enviado no campo `horario` de `POST /pedidos`; `dia` e `hora` servem para exibição.
```json
{ "success": true, "message": "Operacao realizada com sucesso",
  "data": [
    { "valor": "12/11/2026 18:00", "dia": "Hoje",    "hora": "18:00" },
    { "valor": "13/11/2026 18:30", "dia": "Amanhã",  "hora": "18:30" },
    { "valor": "14/11/2026 13:00", "dia": "Sáb, 14/11", "hora": "13:00" }
  ],
  "errors": [] }
```

Regras aplicadas pelo servidor (classe `HorarioFuncionamento`, fuso de Brasília):
- Intervalos de 30 minutos, antecedência mínima de 30 minutos e agendamento até 2 dias à frente (hoje + 2).
- O fim de cada janela é o **último horário que o cliente pode escolher** (comparação "menor ou igual"), tanto na entrega quanto na retirada.
- Janelas vigentes (atualizadas em 07/11/2026):

| Dia | Retirada | Entrega |
|---|---|---|
| Segunda a sexta | 10h às 19h | 18h às 19h30 |
| Sábado | 10h às 19h | 13h às 19h30 |
| Domingo | 10h às 13h | 10h às 13h |

- **Formato do horário:** `dd/MM/yyyy HH:mm` (data e hora juntas, pois o agendamento pode ser em dia futuro).
- **Entrega somente em Itápolis.**

## Recuperação de senha
Fluxo em dois passos, sem sessão:
1. `POST /senha/esqueci` com `{ "email": "..." }`. A resposta é **sempre a mesma**, exista ou não conta com esse e-mail (não revela cadastros). Se houver conta, o servidor envia um código de 6 dígitos para o e-mail cadastrado.
2. `POST /senha/redefinir` com `{ "email": "...", "codigo": "123456", "novaSenha": "..." }`. Em caso de sucesso, a senha é trocada e o app volta para o login.

Regras: o código vale **15 minutos**, aceita **5 tentativas erradas**, tem **uso único** e só o código mais recente vale (um novo pedido invalida o anterior). Há no máximo **1 e-mail por minuto** por cliente. O e-mail sai de uma conta Gmail dedicada e, nos primeiros envios, **pode cair na caixa de spam**; o app deve avisar o usuário para verificar esse local.

## Corpos de requisição
**Cadastro** `POST /cadastro`
```json
{ "nome": "Maria", "email": "maria@exemplo.com", "telefone": "16999999999", "senha": "..." }
```
**Endereço** `POST` ou `PUT /enderecos`
```json
{ "logradouro": "Rua X", "numero": "10", "complemento": null, "bairro": "Centro",
  "cep": "14900000", "cidade": "Itápolis", "estado": "SP", "principal": true }
```
**Pedido** `POST /pedidos`
```json
{ "itens": [ { "produtoId": 1, "quantidade": 2 } ],
  "tipoEntrega": "Entrega",
  "endereco": { "...mesmos campos do endereço..." },
  "horario": "14/11/2026 18:00",
  "formaPagamento": "Pix",
  "observacao": "Sem cebola" }
```
- `tipoEntrega`: `Retirada` (1) ou `Entrega` (2). Em `Retirada`, `endereco` é dispensável.
- `horario`: o campo `valor` de um dos horários devolvidos por `GET /horarios` para o mesmo tipo. O servidor revalida e grava a data e o horário no pedido; horário fora da janela é recusado (400).
- `formaPagamento`: `Dinheiro`, `CartaoDebito`, `CartaoCredito`, `Pix`, `Transferencia`, `Outro`. O pagamento é **presencial**, na entrega ou na retirada; o app não processa pagamento.
- **O app não pode enviar a forma de pagamento "Pago pela plataforma"** (valor 7 do enum): ela é exclusiva de vendas do UAI Rango lançadas no painel do Sistema Web.
- O cliente **não envia** preço, desconto, taxa nem status: o servidor decide.

## Status do pedido
O Sistema Web mantém o status interno da venda; o app o traduz para o cliente:

| Status interno | Entrega (exibido ao cliente) | Retirada (exibido ao cliente) |
|---|---|---|
| Aberta / EmEntrega | Em preparo | Agendado |
| EmProcessamento ("Saiu para entrega", só delivery) | Saiu para entrega | não se aplica |
| Concluida / Paga | Entregue | Retirado |
| Cancelada | Cancelado | Cancelado |

O detalhe do pedido é atualizado automaticamente pelo app a cada 20 segundos, e a aba Pedidos atualiza ao ser aberta.

## Fora do escopo por decisão do cliente
Notificações push, pagamento dentro do aplicativo, avaliação de pedidos e autenticação por token (JWT) **não fazem parte do escopo**, por decisão dos proprietários.

## Pendências conhecidas
- Fotos enviadas pelo painel com orientação definida só pelos metadados da câmera (EXIF) podem aparecer giradas; o ideal é enviar fotos já na orientação correta.
