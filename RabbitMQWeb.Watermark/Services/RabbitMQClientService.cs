using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace RabbitMQWeb.Watermark.Services
{
    //Bu kısım producer tarafı olmaktadır
    public class RabbitMQClientService:IDisposable
    {
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        public static string ExchangeName = "ImageDirectExchange";
        public static string RoutingWatermark="watermark-route-image";
        public static string QueueName = "queue-watermark-image";

        private readonly ILogger<RabbitMQClientService> _logger;

        public RabbitMQClientService(ConnectionFactory connectionFactory,ILogger<RabbitMQClientService> logger)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
        }

        //Bir model yani bir channel donecegim;
        public IModel Connect()
        {
            _connection= _connectionFactory.CreateConnection();
            if (_channel is { IsOpen:true })//zaten bir kanal var ise
            {
                return _channel;
            }

            _channel=_connection.CreateModel();
            _channel.ExchangeDeclare(exchange: ExchangeName,type:ExchangeType.Direct,durable:true,autoDelete:false);
            _channel.QueueDeclare(queue:QueueName,durable:true,exclusive:false,autoDelete:false,null);
            _channel.QueueBind(queue: QueueName,exchange:ExchangeName, routingKey: RoutingWatermark, null);

            _logger.LogInformation("Connection established with RabbitMQ...");

            return _channel;
        }

        public void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            //_channel = default;//_channel string bir ifade oldugu icin defaulta set etmek null'a set etmektir
            _connection?.Close();//Connection varsa close ediliyor
            _connection?.Dispose();//kapattıktan sonra dispose edilir.

            _logger.LogInformation("Lost connection to RabbitMQ...");
            
        }
    }
}
