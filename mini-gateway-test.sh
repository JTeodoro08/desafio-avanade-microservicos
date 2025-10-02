#!/bin/bash
# ==========================================
# MINI GATEWAY TEST (Atualizado)
# Testa roteamento via API Gateway
# ==========================================

GATEWAY_URL="http://localhost:5271"

echo "=============================="
echo " MINI GATEWAY TEST INICIADO"
echo "=============================="

# 1. Testar EstoqueService
echo ""
echo ">> Listando produtos pelo Gateway..."
curl -s $GATEWAY_URL/estoque/produtos | jq '.'

echo ""
echo ">> Criando novo produto pelo Gateway..."
curl -s -X POST $GATEWAY_URL/estoque/produtos \
  -H "Content-Type: application/json" \
  -d '{"nome":"Mouse Gamer","descricao":"RGB","preco":120.50,"quantidade":50}' | jq '.'

echo ""
echo ">> Listando produtos novamente..."
curl -s $GATEWAY_URL/estoque/produtos | jq '.'

# 2. Testar VendasService
echo ""
echo ">> Criando pedido pelo Gateway..."
PEDIDO=$(curl -s -X POST $GATEWAY_URL/vendas/pedidos \
  -H "Content-Type: application/json" \
  -d '{
        "ClienteNome":"Cliente Gateway",
        "Itens":[
          { "ProdutoId":2, "Quantidade":3 }
        ]
      }')

echo $PEDIDO | jq '.'

PEDIDO_ID=$(echo $PEDIDO | jq -r '.id')

echo ""
echo ">> Listando pedidos pelo Gateway..."
curl -s $GATEWAY_URL/vendas/pedidos | jq '.'

# 3. Testar RabbitMQ Reenvio
if [ "$PEDIDO_ID" != "null" ]; then
  echo ""
  echo ">> Reenviando pedido $PEDIDO_ID via Gateway..."
  curl -s -X POST $GATEWAY_URL/vendas/pedidos/reenviar-rabbit/$PEDIDO_ID | jq '.'
fi

echo ""
echo "=============================="
echo " MINI GATEWAY TEST FINALIZADO"
echo "=============================="



