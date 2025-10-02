#!/usr/bin/env bash
set -euo pipefail

# =========================
# MINI E2E TEST - Fluxo Completo
# =========================

# Configurações
VENDAS_URL="http://localhost:5111"
ESTOQUE_URL="http://localhost:5068"
PRODUTO_ID=2
QUANTIDADE=3
CLIENTE="Cliente Teste"
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImFkbWluIiwibmJmIjoxNzU4NTkwMDM0LCJleHAiOjE3NTg1OTM2MzQsImlhdCI6MTc1ODU5MDAzNCwiaXNzIjoiRGVzYWZpb0F2YW5hZGUiLCJhdWQiOiJDbGllbnRlQVBJIn0.6-1Qn-erxkqsxKsetxkDlrNoNoN6WsdNwUoEx9zh8aM"  # coloque o token obtido no login

echo "=============================="
echo "MINI E2E TEST - Fluxo Completo"
echo "ProdutoId: $PRODUTO_ID | Qtd do pedido: $QUANTIDADE"
echo "=============================="

# 1) Estoque antes
echo ">> Estoque antes do pedido:"
curl -s "$ESTOQUE_URL/api/Produtos/$PRODUTO_ID" | jq '.' || curl -s "$ESTOQUE_URL/api/Produtos/$PRODUTO_ID"

# 2) Criar pedido
echo ">> Criando pedido..."
curl -s -X POST "$VENDAS_URL/api/Pedidos" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{\"clienteNome\":\"$CLIENTE\",\"itens\":[{\"produtoId\":$PRODUTO_ID,\"quantidade\":$QUANTIDADE}]}" | jq '.' || echo "Pedido enviado."

# 3) Espera 3 segundos para processamento assíncrono do RabbitMQ
echo ">> Aguardando estoque atualizar..."
sleep 3

# 4) Estoque depois
echo ">> Estoque após processamento:"
curl -s "$ESTOQUE_URL/api/Produtos/$PRODUTO_ID" | jq '.' || curl -s "$ESTOQUE_URL/api/Produtos/$PRODUTO_ID"

echo "=============================="
echo "MINI E2E TEST FINALIZADO"
echo "=============================="


