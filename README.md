# 📖 README.md (versão unificada)

# 🛒 Sistema de Gerenciamento de Estoque e Vendas – Microserviços

## 📌 Descrição
Este projeto implementa uma arquitetura de **microserviços** para gerenciamento de estoque e vendas em uma plataforma de e-commerce.  
A solução contempla:

- **EstoqueService** → CRUD de produtos, controle de estoque e verificação de disponibilidade.  
- **VendasService** → Criação, consulta e gerenciamento de pedidos com integração ao estoque.  
- **ApiGateway** → Centraliza todas as requisições, roteando para os microserviços corretos.  
- **RabbitMQ** → Comunicação assíncrona entre os serviços.  
- **JWT** → Autenticação e proteção dos endpoints.  

---

## ⚙️ Tecnologias
- .NET 8 / C#
- Entity Framework Core
- SQL Server (com suporte a InMemory DB para testes)
- RabbitMQ
- JWT (Json Web Token)
- xUnit para testes
- Ocelot API Gateway
- Swagger para documentação de APIs

---

## 🏗️ Arquitetura
```

Cliente → API Gateway → EstoqueService
↘→ VendasService

RabbitMQ → Comunicação assíncrona entre Estoque e Vendas

````

---

## 🚀 Executando o Projeto

1. **Subir RabbitMQ** (Docker ou local)
```bash
docker run -d --hostname rabbit --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
````

2. **Executar os microserviços**

```bash
dotnet run --project EstoqueService
dotnet run --project VendasService
dotnet run --project ApiGateway
```

3. **Acessar Swagger**:

* Estoque: `http://localhost:5068/swagger`
* Vendas: `http://localhost:5111/swagger`
* Gateway: `http://localhost:5271/swagger`

4. **Executar os testes automatizados**

```bash
dotnet test
```

---

## 🔑 Autenticação

Todos os endpoints de escrita (POST/PUT/DELETE) exigem **JWT**.
Fluxo:

1. `POST /api/Auth/login` → gera token JWT.
2. Usar `Bearer {token}` no header `Authorization` para acessar endpoints protegidos.

---

## 📦 Endpoints Principais

### 🔹 EstoqueService (`http://localhost:5068`)

**Auth**

* `POST /api/Auth/login` → autenticação

**Produtos**

* `GET /api/Produtos` → listar todos os produtos
* `GET /api/Produtos/{id}` → obter produto por ID
* `POST /api/Produtos` → criar produto (JWT)
* `PUT /api/Produtos/{id}` → atualizar produto (JWT)
* `DELETE /api/Produtos/{id}` → excluir produto (JWT)
* `GET /api/Produtos/{id}/disponibilidade/{quantidade}` → verificar disponibilidade

---

### 🔹 VendasService (`http://localhost:5111`)

**Auth**

* `POST /api/Auth/login` → autenticação

**Pedidos**

* `GET /api/Pedidos` → listar pedidos
* `POST /api/Pedidos` → criar pedido (JWT)
* `GET /api/Pedidos/{id}` → buscar pedido por ID
* `PUT /api/Pedidos/{id}` → atualizar pedido (JWT)
* `DELETE /api/Pedidos/{id}` → excluir pedido (JWT)
* `GET /api/Pedidos/consulta` → consulta avançada (filtros, paginação, ordenação)
* `POST /api/Pedidos/reenviar-rabbit/{id}` → reenviar evento RabbitMQ

---

### 🔹 API Gateway (`http://localhost:5271`)

O **ApiGateway** redireciona todas as requisições externas para os microserviços.
Configuração principal (via `ocelot.json`):

**Estoque**

* `/estoque/produtos` → EstoqueService `/api/Produtos`
* `/estoque/produtos/{id}` → EstoqueService `/api/Produtos/{id}`

**Vendas**

* `/vendas/pedidos` → VendasService `/api/Pedidos`
* `/vendas/pedidos/{id}` → VendasService `/api/Pedidos/{id}`
* `/vendas/pedidos/consulta` → VendasService `/api/Pedidos/consulta`
* `/vendas/pedidos/reenviar-rabbit/{id}` → VendasService `/api/Pedidos/reenviar-rabbit/{id}`

---

## 🧪 Testes

* **EstoqueService.Tests** → CRUD de produtos + validações.
* **VendasService.Tests** → criação/validação de pedidos, integração RabbitMQ fake.
* **ApiGateway** → testes de roteamento (via curl/Postman).

Todos os testes **aprovados** ✅

---

## 📊 Logs e Monitoramento

* Logs básicos de operações nos serviços.
* RabbitMQ registra eventos de atualização de estoque e pedidos.

---

## ✅ Status Final

* EstoqueService: 100% implementado e testado
* VendasService: 100% implementado e testado
* ApiGateway: 100% implementado e testado
* RabbitMQ: 100% integrado
* JWT: 100% funcional
* Testes automatizados: concluídos ✅
* Documentação Swagger + README unificado: concluído ✅

## 📌 Próximos Passos (Futuros / Opcional)

* Melhorar logs para auditoria completa.
* Adicionar monitoramento (Prometheus / Grafana).
* Criar microsserviço extra (pagamentos/envios) para escalar o sistema.




