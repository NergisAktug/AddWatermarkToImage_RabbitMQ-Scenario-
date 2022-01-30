
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using RabbitMQWeb.Watermark.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text;
using System.Drawing;

namespace RabbitMQWeb.Watermark.BackgroundServices
{
    public class ImageWatermarkProcessBackgroundService : BackgroundService
    {
        //kanal icin ilgili service'den instance olusturuldu
        private readonly RabbitMQClientService _rabbitMQClientService;
        private readonly ILogger<ImageWatermarkProcessBackgroundService> _logger;
        private IModel _channel;//constructor'da degil farklı bir method'da set edilecegi icin readonly seklinde tanımlamadım.

        public ImageWatermarkProcessBackgroundService(RabbitMQClientService rabbitMQClientService, ILogger<ImageWatermarkProcessBackgroundService> logger)
        {
            _rabbitMQClientService = rabbitMQClientService;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _channel = _rabbitMQClientService.Connect();
            _channel.BasicQos(0,1,false);//birer birer queue alınıyor

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel); //queue'lar asenkron bir sekilde okunmaktadır.
            _channel.BasicConsume(RabbitMQClientService.QueueName,false,consumer);
            //Event dinleniyor..
            consumer.Received += Consumer_Received;

            return Task.CompletedTask;
        }

        private Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            //Resme image ekleme olayı burada gerceklesiyor.

            try
            {
                var productImageCreatedEvent = JsonSerializer.Deserialize<productImageCreatedEvent>(Encoding.UTF8.GetString(@event.Body.ToArray()));
                //wwwroot/images Klasorundeki gelen ImageName isimli dosyanın guncel directory'sini verir.
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", productImageCreatedEvent.ImageName);//Guncel directory'i verir

                var siteName = "www.mysite.com";

                using var img = Image.FromFile(path);

                using var graphic = Graphics.FromImage(img);

                var font = new Font(FontFamily.GenericMonospace, 40, FontStyle.Bold, GraphicsUnit.Pixel);

                var TextSize = graphic.MeasureString(siteName, font);
                var color = Color.FromArgb(128, 255, 255, 255);
                var brush = new SolidBrush(color);
                var position = new Point(img.Width - ((int)TextSize.Width + 30), img.Height - ((int)TextSize.Height + 30));

                graphic.DrawString(siteName, font, brush, position);

                img.Save("wwwroot/Images/watermarks/" + productImageCreatedEvent.ImageName);

                img.Dispose();//img'yi bellekten düşürüyoruz.
                graphic.Dispose();

                _channel.BasicAck(@event.DeliveryTag, false);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message);
            }


            return Task.CompletedTask;
            

        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

       

    }
}
