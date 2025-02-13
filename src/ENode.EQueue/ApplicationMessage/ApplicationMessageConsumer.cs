﻿using System.Text;
using ECommon.Components;
using ECommon.Serializing;
using ENode.Infrastructure;
using EQueue.Clients.Consumers;
using EQueue.Protocols;
using IQueueMessageHandler = EQueue.Clients.Consumers.IMessageHandler;

namespace ENode.EQueue
{
    public class ApplicationMessageConsumer : IQueueMessageHandler
    {
        private const string DefaultMessageConsumerGroup = "ApplicationMessageConsumerGroup";
        private readonly Consumer _consumer;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ITypeCodeProvider _typeCodeProvider;
        private readonly IMessageProcessor<ProcessingApplicationMessage, IApplicationMessage, bool> _processor;

        public Consumer Consumer { get { return _consumer; } }

        public ApplicationMessageConsumer(string groupName = null, ConsumerSetting setting = null)
        {
            _consumer = new Consumer(groupName ?? DefaultMessageConsumerGroup, setting ?? new ConsumerSetting
            {
                MessageHandleMode = MessageHandleMode.Sequential
            });
            _jsonSerializer = ObjectContainer.Resolve<IJsonSerializer>();
            _processor = ObjectContainer.Resolve<IMessageProcessor<ProcessingApplicationMessage, IApplicationMessage, bool>>();
            _typeCodeProvider = ObjectContainer.Resolve<ITypeCodeProvider>();
        }

        public ApplicationMessageConsumer Start()
        {
            _consumer.SetMessageHandler(this).Start();
            return this;
        }
        public ApplicationMessageConsumer Subscribe(string topic)
        {
            _consumer.Subscribe(topic);
            return this;
        }
        public ApplicationMessageConsumer Shutdown()
        {
            _consumer.Shutdown();
            return this;
        }

        void IQueueMessageHandler.Handle(QueueMessage queueMessage, IMessageContext context)
        {
            var messageType = _typeCodeProvider.GetType<IApplicationMessage>(queueMessage.Code);
            var message = _jsonSerializer.Deserialize(Encoding.UTF8.GetString(queueMessage.Body), messageType) as IApplicationMessage;
            var processContext = new EQueueProcessContext(queueMessage, context);
            var processingMessage = new ProcessingApplicationMessage(message, processContext);
            _processor.Process(processingMessage);
        }
    }
}
