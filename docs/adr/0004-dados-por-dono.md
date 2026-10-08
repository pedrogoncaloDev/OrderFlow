# ADR 0004: Cadastros isolados por dono (mini ERP)

**Status:** Aceita. Ajusta o enquadramento do `CONTEXT.md` ("clientes fazem pedidos"): o OrderFlow é um mini ERP, em que o usuário logado é o operador do negócio e cadastra os próprios clientes, produtos e pedidos.

## Contexto
O modelo inicial não tinha o conceito de "de quem é" um cadastro: qualquer produto ou cliente seria visível para qualquer usuário autenticado, e o e-mail do cliente era único no sistema inteiro (dois usuários não poderiam cadastrar o mesmo cliente). Isso não serve para um sistema em que cada usuário gerencia o próprio negócio.

## Decisão
- `products` e `customers` ganham `owner_id` (obrigatório, FK para `users`, `Restrict`). O dono é sempre o usuário do token (claim `sub`), definido no servidor; nunca vem do corpo da requisição.
- Todo acesso aos cadastros filtra por `owner_id`. Registro de outro dono responde **404**, igual a um registro inexistente, para não revelar que ele existe.
- Os filtros são explícitos em cada consulta do controller (sem filtro global do EF Core), seguindo a simplicidade do ADR 0003. Cada endpoint novo precisa repetir o filtro; os testes de integração cobrem o isolamento entre dois usuários.
- A unicidade do e-mail do cliente passa a ser **por dono**: índice único em (`owner_id`, `email`).
- Regras de exclusão mantidas: produto vendido e cliente com pedidos não podem ser apagados (`Restrict`); a API responde **409**.
- Listagens são paginadas (`page`, `pageSize` de 1 a 100) e ordenadas por nome.
- Valores inválidos são barrados também no banco (`price >= 0`, `stock_quantity >= 0`).

## Consequências
- Pedidos (`orders`) ainda não têm `owner_id`: ao implementar o módulo de pedidos, a mesma regra vale (dono do pedido e checagem de que cliente e produtos pertencem ao mesmo dono).
- O vínculo `users.customer_id` e o papel `Customer` não fazem sentido nesse modelo e deixam de ser usados; a limpeza fica para quando o módulo de pedidos definir os papéis.
- Várias pessoas compartilhando a mesma empresa exigiriam um `tenant_id` (entidade `Company`) no lugar do `owner_id`. Não é necessário agora; a migração seria trocar a coluna de dono.
- As migrations que adicionam `owner_id` assumem tabelas vazias (não havia endpoint para criar esses cadastros antes).
