#!/bin/bash

# =============================
# 🚀 Mini E2E Teste - Vendas + Estoque
# =============================

# Variáveis
VENDAS_URL="http://localhost:5111"
ESTOQUE_URL="http://localhost:5068"
USERNAME="admin"
PASSWORD="1234"

# 1️⃣ Gerar token JWT
echo "[INFO] Gerando token JWT..."
TOKEN=$(curl -s -X POST "$VENDAS_URL/api/auth/login" \
-H "Content-Type: application/json" \
-d "{\"username\": \"$USERNAME\", \"password\": \"$PASSWORD\"}" | sed -E 's/.*"token":"([^"]+)".*/\1/')


if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
    echo "[ERRO] Falha ao gerar token."
    exit 1
fi

echo "[OK] Token gerado: $TOKEN"

# 2️⃣ Criar pedido
echo "[INFO] Criando pedido..."
CREATE_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$VENDAS_URL/api/Pedidos" \
-H "Content-Type: application/json" \
-H "Authorization: Bearer $TOKEN" \
-d '{
  "ClienteNome": "Cliente Teste",
  "Itens": [
    { "ProdutoId": 1, "Quantidade": 2 },
    { "ProdutoId": 2, "Quantidade": 1 }
  ]
}')

if [ "$CREATE_RESPONSE" == "201" ] || [ "$CREATE_RESPONSE" == "200" ]; then
    echo "[OK] Pedido criado com sucesso!"
else
    echo "[ERRO] Falha ao criar pedido. Status HTTP: $CREATE_RESPONSE"
fi

# 3️⃣ (Opcional) Consultar produtos no EstoqueService
echo "[INFO] Listando produtos no estoque..."
curl -s -X GET "$ESTOQUE_URL/estoque/produtos" \
-H "Authorization: Bearer $TOKEN" | jq

echo "[INFO] Teste finalizado."
