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


### README unificado adicionando as "configurações do Docker Compose e do SQL Server/RabbitMQ", detalhando o passo a passo de execução completo ###

---

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
🛒 Sistema de Gerenciamento de Estoque e Vendas – Microserviços
📌 Descrição

Este projeto implementa uma arquitetura de microserviços para gerenciamento de estoque e vendas em uma plataforma de e-commerce.
A solução contempla:

EstoqueService → CRUD de produtos, controle de estoque e verificação de disponibilidade.

VendasService → Criação, consulta e gerenciamento de pedidos com integração ao estoque.

ApiGateway → Centraliza todas as requisições, roteando para os microserviços corretos.

RabbitMQ → Comunicação assíncrona entre os serviços.

JWT → Autenticação e proteção dos endpoints.

⚙️ Tecnologias

.NET 8 / C#

Entity Framework Core

SQL Server (com suporte a InMemory DB para testes)

RabbitMQ

JWT (Json Web Token)

xUnit para testes automatizados

Ocelot API Gateway

Swagger para documentação de APIs

Docker / Docker Compose

🏗️ Arquitetura
Cliente → API Gateway → EstoqueService
             ↘→ VendasService

RabbitMQ → Comunicação assíncrona entre Estoque e Vendas

🐳 Docker Compose – Infraestrutura

Arquivo docker-compose.yml:

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


⚙️ Todas as configurações de conexão (SQL Server e RabbitMQ) estão alinhadas com os arquivos appsettings.json.

🚀 Executando o Projeto – Passo a Passo
1️⃣ Subir a infraestrutura com Docker
docker-compose up -d


Verifique os containers:

docker ps


SQL Server: localhost:1433

RabbitMQ: localhost:5672 (Management UI em http://localhost:15672, usuário: admin, senha: admin)

2️⃣ Executar os microserviços localmente
dotnet run --project EstoqueService
dotnet run --project VendasService
dotnet run --project ApiGateway

3️⃣ Acessar Swagger

EstoqueService → http://localhost:5068/swagger

VendasService → http://localhost:5111/swagger

API Gateway → http://localhost:5271/swagger

4️⃣ Executar os testes automatizados
dotnet test


EstoqueService.Tests → CRUD + validações de estoque.

VendasService.Tests → criação/validação de pedidos + integração RabbitMQ fake.

API Gateway → roteamento e segurança via JWT.

✅ Todos os testes devem ser aprovados.

🔑 Autenticação JWT

Todos os endpoints de escrita (POST, PUT, DELETE) exigem JWT.

POST /api/Auth/login → gera token JWT.

Usar Bearer {token} no header Authorization para acessar endpoints protegidos.

📦 Endpoints Principais
🔹 EstoqueService (http://localhost:5068)

Auth

POST /api/Auth/login → autenticação

Produtos

GET /api/Produtos → listar todos os produtos

GET /api/Produtos/{id} → obter produto por ID

POST /api/Produtos → criar produto (JWT)

PUT /api/Produtos/{id} → atualizar produto (JWT)

DELETE /api/Produtos/{id} → excluir produto (JWT)

GET /api/Produtos/{id}/disponibilidade/{quantidade} → verificar disponibilidade

🔹 VendasService (http://localhost:5111)

Auth

POST /api/Auth/login → autenticação

Pedidos

GET /api/Pedidos → listar pedidos

POST /api/Pedidos → criar pedido (JWT)

GET /api/Pedidos/{id} → buscar pedido por ID

PUT /api/Pedidos/{id} → atualizar pedido (JWT)

DELETE /api/Pedidos/{id} → excluir pedido (JWT)

GET /api/Pedidos/consulta → consulta avançada (filtros, paginação, ordenação)

POST /api/Pedidos/reenviar-rabbit/{id} → reenviar evento RabbitMQ

🔹 API Gateway (http://localhost:5271)

O ApiGateway redireciona todas as requisições externas para os microserviços.

Estoque

/estoque/produtos → EstoqueService /api/Produtos

/estoque/produtos/{id} → EstoqueService /api/Produtos/{id}

Vendas

/vendas/pedidos → VendasService /api/Pedidos

/vendas/pedidos/{id} → VendasService /api/Pedidos/{id}

/vendas/pedidos/consulta → VendasService /api/Pedidos/consulta

/vendas/pedidos/reenviar-rabbit/{id} → VendasService /api/Pedidos/reenviar-rabbit/{id}

📊 Logs e Monitoramento
🆕 Melhorias Recentes (Outubro/2025)

O EstoqueService recebeu uma grande melhoria nos logs do consumidor RabbitMQ, agora com:

Estrutura visual clara e padronizada.

Exibição de:

ID do Pedido e Cliente;

Data/hora de processamento;

Total de itens;

Estoque anterior, quantidade retirada e estoque atualizado.

Logs marcando início e fim do processamento (📥 INÍCIO PROCESSAMENTO / ✅ Estoque atualizado com sucesso).

Melhoria na decodificação de caracteres UTF-8 (nomes de clientes com acentuação).

Essas melhorias tornaram a auditoria e rastreabilidade muito mais fáceis entre Estoque e Vendas.

✅ Status Final
Serviço	Status	Observações
EstoqueService	✅ 100% funcional	Logs aprimorados e integração RabbitMQ estável
VendasService	✅ 100% funcional	Comunicação com Estoque validada
API Gateway	✅ 100% funcional	JWT e roteamento testados
RabbitMQ	✅ 100% funcional	Fila consistente
Testes automatizados	✅ Todos aprovados	Incluindo integração entre serviços
📌 Próximos Passos (Futuros / Opcionais)

🔁 Ajuste de estoque em alterações de pedidos

Implementar cálculo de diferença entre pedido anterior e novo, permitindo reposição automática em casos de diminuição da quantidade.

Exigirá armazenamento de histórico de pedidos processados e comparação antes/depois.

📜 Auditoria aprimorada de logs

Persistir logs em tabela dedicada e incluir identificação de origem do evento (Vendas → Estoque).

📘 Documentação final (Swagger e README)

Consolidar exemplos de requisições e fluxos RabbitMQ.

📈 Monitoramento (Prometheus / Grafana)

Adicionar métricas de filas, tempo médio de processamento e falhas.

🧩 Conclusão

✅ Todos os microserviços, integrações, testes, logs e autenticação JWT estão totalmente implementados e funcionais.
💡 O sistema está 100% estável, preparado para auditoria aprimorada e ajustes dinâmicos de estoque em futuras versões.




