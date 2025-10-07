# README unificado adicionando as "configurações do Docker Compose e do SQL Server/RabbitMQ", detalhando o passo a passo de execução completo #

# 📖 README.md (versão unificada final)

# 🛒 Sistema de Gerenciamento de Estoque e Vendas – Microserviços

## 📌 Descrição

Este projeto implementa uma arquitetura de **microserviços** para gerenciamento de estoque e vendas em uma plataforma de e-commerce.
A solução contempla:

* **EstoqueService** → CRUD de produtos, controle de estoque e verificação de disponibilidade.
* **VendasService** → Criação, consulta e gerenciamento de pedidos com integração ao estoque.
* **ApiGateway** → Centraliza todas as requisições, roteando para os microserviços corretos.
* **RabbitMQ** → Comunicação assíncrona entre os serviços.
* **JWT** → Autenticação e proteção dos endpoints.

---

## ⚙️ Tecnologias

* .NET 8 / C#
* Entity Framework Core
* SQL Server (com suporte a InMemory DB para testes)
* RabbitMQ
* JWT (Json Web Token)
* xUnit para testes
* Ocelot API Gateway
* Swagger para documentação de APIs
* Docker/Docker Compose

---

## 🏗️ Arquitetura

```
Cliente → API Gateway → EstoqueService
             ↘→ VendasService

RabbitMQ → Comunicação assíncrona entre Estoque e Vendas
```

## 🐳 Docker Compose – Infraestrutura

Arquivo `docker-compose.yml`:

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sqlserver
    environment:
      SA_PASSWORD: "StrongPass@123"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    networks:
      - ecommerce-network

  rabbitmq:
    image: rabbitmq:3-management
    container_name: rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: admin
    ports:
      - "5672:5672"    
      - "15672:15672"  
    networks:
      - ecommerce-network

networks:
  ecommerce-network:
    driver: bridge
```

> Observação: Todas as configurações de conexão (SQL Server e RabbitMQ) já estão de acordo com `appsettings.json`.

---

## 🚀 Executando o Projeto – Passo a Passo

### 1️⃣ Subir a infraestrutura com Docker

```bash
docker-compose up -d
```

Verifique os containers:

```bash
docker ps
```

* SQL Server: `localhost:1433`
* RabbitMQ: `localhost:5672` (Management UI em `http://localhost:15672`, usuário: admin, senha: admin)

---

### 2️⃣ Executar os microserviços localmente

```bash
dotnet run --project EstoqueService
dotnet run --project VendasService
dotnet run --project ApiGateway
```

---

### 3️⃣ Acessar Swagger

* EstoqueService: `http://localhost:5068/swagger`
* VendasService: `http://localhost:5111/swagger`
* API Gateway: `http://localhost:5271/swagger`

---

### 4️⃣ Executar os testes automatizados

```bash
dotnet test
```

* **EstoqueService.Tests** → CRUD de produtos + validações.
* **VendasService.Tests** → criação/validação de pedidos, integração RabbitMQ fake.
* **API Gateway** → testes de roteamento via curl/Postman.

Todos os testes devem ser aprovados ✅

---

## 🔑 Autenticação

Todos os endpoints de escrita (POST/PUT/DELETE) exigem **JWT**.

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

**Estoque**

* `/estoque/produtos` → EstoqueService `/api/Produtos`
* `/estoque/produtos/{id}` → EstoqueService `/api/Produtos/{id}`

**Vendas**

* `/vendas/pedidos` → VendasService `/api/Pedidos`
* `/vendas/pedidos/{id}` → VendasService `/api/Pedidos/{id}`
* `/vendas/pedidos/consulta` → VendasService `/api/Pedidos/consulta`
* `/vendas/pedidos/reenviar-rabbit/{id}` → VendasService `/api/Pedidos/reenviar-rabbit/{id}`

---

## 📊 Logs e Monitoramento

* Logs básicos implementados nos serviços.
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

---

## 📌 Próximos Passos (Futuros / Opcional)

* Melhorar logs para auditoria completa.
* Adicionar monitoramento (Prometheus / Grafana).
* Criar microsserviço extra (pagamentos/envios) para escalar o sistema.





📖 README.md (versão unificada final – 07/10/2025)

# 📖 Sistema de Gerenciamento de Estoque e Vendas – Microserviços

## 🛒 Descrição

Projeto de microserviços para gerenciamento de estoque e vendas em e-commerce, contemplando:

* **EstoqueService** → CRUD de produtos, controle de estoque e verificação de disponibilidade  
* **VendasService** → Criação, consulta e gerenciamento de pedidos com integração ao estoque  
* **ApiGateway** → Centraliza requisições e roteia para os microserviços  
* **RabbitMQ** → Comunicação assíncrona entre serviços  
* **JWT** → Autenticação e proteção dos endpoints  

---

## ⚙️ Tecnologias

* .NET 8 / C#  
* Entity Framework Core  
* SQL Server (com suporte a InMemory DB para testes)  
* RabbitMQ  
* JWT (Json Web Token)  
* xUnit para testes automatizados  
* Ocelot API Gateway  
* Swagger para documentação  
* Docker / Docker Compose  

---

## 🏗️ Arquitetura



Cliente → API Gateway → EstoqueService
↘→ VendasService

RabbitMQ → Comunicação assíncrona entre Estoque e Vendas


---

## 🐳 Docker Compose – Infraestrutura

Arquivo `docker-compose.yml`:

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sqlserver
    environment:
      SA_PASSWORD: "StrongPass@123"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    networks:
      - ecommerce-network

  rabbitmq:
    image: rabbitmq:3-management
    container_name: rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: admin
    ports:
      - "5672:5672"
      - "15672:15672"
    networks:
      - ecommerce-network

networks:
  ecommerce-network:
    driver: bridge


As configurações de conexão estão alinhadas com appsettings.json.

🚀 Executando o Projeto
1️⃣ Subir infraestrutura
docker-compose up -d
docker ps


SQL Server → localhost:1433

RabbitMQ → localhost:5672 (Management UI: http://localhost:15672, usuário: admin, senha: admin)

2️⃣ Executar microserviços
dotnet run --project EstoqueService
dotnet run --project VendasService
dotnet run --project ApiGateway

3️⃣ Acessar Swagger

EstoqueService → http://localhost:5068/swagger

VendasService → http://localhost:5111/swagger

API Gateway → http://localhost:5271/swagger

4️⃣ Testes automatizados
dotnet test


EstoqueService.Tests → CRUD e validações

VendasService.Tests → pedidos e integração RabbitMQ fake

API Gateway → roteamento e JWT

✅ Todos os testes devem passar

🔑 Autenticação JWT

POST /api/Auth/login → gera token

Usar Bearer {token} no header Authorization

📦 Endpoints Principais
🔹 EstoqueService (http://localhost:5068)

Produtos

GET /api/Produtos → listar

GET /api/Produtos/{id} → buscar por ID

POST /api/Produtos → criar (JWT)

PUT /api/Produtos/{id} → atualizar (JWT)

DELETE /api/Produtos/{id} → excluir (JWT)

GET /api/Produtos/{id}/disponibilidade/{quantidade} → verificar estoque

🔹 VendasService (http://localhost:5111)

Pedidos

GET /api/Pedidos → listar

POST /api/Pedidos → criar (JWT)

GET /api/Pedidos/{id} → buscar por ID

PUT /api/Pedidos/{id} → atualizar (JWT)

DELETE /api/Pedidos/{id} → excluir (JWT)

GET /api/Pedidos/consulta → filtros, paginação e ordenação

POST /api/Pedidos/reenviar-rabbit/{id} → reenviar evento RabbitMQ

🔹 API Gateway (http://localhost:5271)

Estoque

/estoque/produtos → EstoqueService /api/Produtos

/estoque/produtos/{id} → EstoqueService /api/Produtos/{id}

Vendas

/vendas/pedidos → VendasService /api/Pedidos

/vendas/pedidos/{id} → VendasService /api/Pedidos/{id}

/vendas/pedidos/consulta → VendasService /api/Pedidos/consulta

/vendas/pedidos/reenviar-rabbit/{id} → VendasService /api/Pedidos/reenviar-rabbit/{id}

📊 Logs e Monitoramento

Logs básicos nos serviços

RabbitMQ registra eventos de estoque e pedidos

Melhorias recentes:

Estrutura clara e padronizada

ID do pedido, cliente, data/hora, total de itens, estoque anterior/atual

Marcação de início/fim do processamento (📥 / ✅)

Suporte UTF-8 completo

✅ Status Final
Serviço	Status	Observações
EstoqueService	✅ 100%	Logs aprimorados e RabbitMQ estável
VendasService	✅ 100%	Comunicação com Estoque validada
API Gateway	✅ 100%	JWT e roteamento testados
RabbitMQ	✅ 100%	Fila consistente
Testes	✅ Todos aprovados	Incluindo integração entre serviços
📌 Próximos Passos / Futuro

Ajuste automático de estoque em alterações de pedidos

Auditoria de logs persistente com origem do evento

Consolidar exemplos de Swagger + RabbitMQ

Monitoramento via Prometheus / Grafana

🧩 Conclusão

✅ Microserviços, integrações, testes, logs e JWT totalmente implementados.
💡 Sistema está estável e pronto para auditoria e ajustes dinâmicos de estoque.