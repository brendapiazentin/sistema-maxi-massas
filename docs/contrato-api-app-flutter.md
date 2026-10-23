# Contrato da API para o aplicativo Flutter (Maxi Massas)

**Endereço base (ambiente de demonstração na AWS):** `https://maximassasitapolis.duckdns.org/api/app`
Observação: é um subdomínio gratuito (DuckDNS) com IP fixo e certificado HTTPS válido (Let's Encrypt); não é preciso liberar HTTP simples no app.

## Autenticação
- Rotas marcadas como **[anônimo]** não exigem login.
- Rotas **[cliente]** usam **HTTP Basic**: cabeçalho `Authorization: Basic base64(email:senha)` do cliente. Não há token: o app reenvia as credenciais a cada chamada. `GET /api/app/me` serve como "login".
- Falha de credencial devolve **401** (sem o cabeçalho `WWW-Authenticate`).

## Formato de resposta
`{ "success": true, "message": "...", "data": ..., "errors": [] }`. Enums aceitam o nome (ex.: `"Entrega"`) ou o número.

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
| GET | `/cep/{cep}` | anônimo | Logradouro, bairro, cidade e UF (ViaCEP) |
| GET | `/produtos` | anônimo | Cardápio com preço e disponibilidade |
| POST | `/pedidos` | cliente | Cria pedido (vira venda com origem "App") |
| GET | `/pedidos` | cliente | Histórico, mais recentes primeiro |
| GET | `/pedidos/{id}` | cliente | Um pedido, com status atualizado |

Toda operação de perfil e endereço devolve o **perfil completo atualizado**: o servidor decide o resultado (por exemplo, qual endereço é o principal) e o app apenas o reflete.

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
  "horario": "18:00",
  "formaPagamento": "Pix",
  "observacao": "Sem cebola" }
```
- `tipoEntrega`: `Retirada` (1) ou `Entrega` (2). Em `Retirada`, `endereco` é dispensável.
- `formaPagamento`: `Dinheiro`, `CartaoDebito`, `CartaoCredito`, `Pix`, `Transferencia`, `Outro`. O pagamento é **presencial**, na entrega ou na retirada; o app não processa pagamento.
- O cliente **não envia** preço, desconto, taxa nem status: o servidor decide.

## Pendências conhecidas
- Regras de janela de horário de entrega e de taxa por forma de pagamento ainda dependem de confirmação com a Karina.
- Não há envio de notificações (decisão do levantamento de requisitos).
