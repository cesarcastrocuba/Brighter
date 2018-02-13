using System;
using System.Collections.Concurrent;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Paramore.Brighter.MessagingGateway.RMQ
{
    public sealed class BrighterBasicConsumer : DefaultBasicConsumer, IDisposable
    {
        private readonly IModel _channel;
        private readonly BlockingCollection<Message> _messages;
        private readonly RmqMessageCreator _rmqMessageCreator;

        public BrighterBasicConsumer(IModel channel) : base(channel)
        {
            _channel = channel;
            _messages = new BlockingCollection<Message>();
            _rmqMessageCreator = new RmqMessageCreator();
        }

        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            bool addedToInternalQueue = false;
            try
            {
                var basicDeliverEventArgs = new BasicDeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange,
                    routingKey, properties, body);

                var message = _rmqMessageCreator.CreateMessage(basicDeliverEventArgs);

                addedToInternalQueue = _messages.TryAdd(message);
            }
            finally
            {
                if (!addedToInternalQueue)
                {
                    _channel.BasicNack(deliveryTag, false, true);
                }
            }
        }

        public Message Dequeue(int timeOut)
        {
            return _messages.TryTake(out Message message, timeOut) ? message : new Message();
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messages?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
