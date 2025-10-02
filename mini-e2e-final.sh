#!/usr/bin/env bash
set -euo pipefail

# Config
VENDAS_URL="http://localhost:5111"
ESTOQUE_URL="http://localhost:5068"
PRODUTO_ID=2
QUANTIDADE=3
CLIENTE="Cliente Teste"

echo "=============================="
echo "MINI E2E FINAL TEST"
echo "ProdutoId: $PRODUTO_ID | Qtd do pedido: $QUANTIDADE"
echo "=============================="

# Função para checar estoque
check_estoque() {
  local produto_id=$1
  local label=$2
  echo ">> Estoque $label:"
  curl -s "$ESTOQUE_URL/api/Produtos/$produto_id" | jq '.'
}

# 1) Estoque antes
check_estoque $PRODUTO_ID "antes do pedido"

# 2) Criar pedido
echo ">> Criando pedido..."
response=$(curl -s -X POST "$VENDAS_URL/api/Pedidos" \
  -H "Content-Type: application/json" \
  -d "{\"ClienteNome\":\"$CLIENTE\",\"Itens\":[{\"ProdutoId\":$PRODUTO_ID,\"Quantidade\":$QUANTIDADE}]}")

pedido_id=$(echo "$response" | jq '.id')

if [[ -n "$pedido_id" && "$pedido_id" != "null" ]]; then
  echo "✅ Pedido criado com ID: $pedido_id"
else
  echo "❌ Falha ao criar pedido"
  exit 1
fi

# 3) Espera 3 segundos para processamento
echo ">> Aguardando estoque atualizar (3s)..."
sleep 3

# 4) Estoque após criação
check_estoque $PRODUTO_ID "após processamento"

# 5) Reenviar pedido
echo ">> Reenviando pedido para RabbitMQ..."
reenviar_response=$(curl -s -X POST "$VENDAS_URL/api/Pedidos/reenviar-rabbit/$pedido_id" -H "accept: */*")
echo "$reenviar_response"

# 6) Estoque após reenvio
check_estoque $PRODUTO_ID "após reenvio"

echo "=============================="
echo "MINI E2E FINAL TEST FINISHED"
echo "=============================="

# Log resumido
echo "✅ Log Resumido:"
echo "Pedido ID: $pedido_id"
echo "Produto ID: $PRODUTO_ID"
echo "Quantidade pedida: $QUANTIDADE"

