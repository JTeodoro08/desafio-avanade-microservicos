namespace VendasService.Services
{
    public interface IRabbitMqProducerService
    {
        void EnviarEventoPedido(PedidoMessage pedido, string tipoEvento);
    }
}


