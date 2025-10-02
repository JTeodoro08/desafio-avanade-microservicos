namespace VendasService.Services
{
    public interface IRabbitMqProducerService
    {
        void EnviarPedido(PedidoMessage pedido);
    }
}

