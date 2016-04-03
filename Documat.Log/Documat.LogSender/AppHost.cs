using System;
using Funq;
using ServiceStack;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;

namespace Documat.LogSender
{
    public class AppHost : AppSelfHostBase
    {
        public AppHost()
            : base("Hello World",
                   typeof(HelloService).Assembly)
        {
        }

        public override void Configure(Container container)
        {
            container.Register<IMessageService>(arg => new RabbitMqServer());

            // adding request logs feature with custom request logger
            this.Plugins.Add(new RequestLogsFeature
            {
                RequestLogger = new MessageServiceRequestLogger("Hello World Service on {0}".Fmt(Environment.MachineName))
                {
                    MessageService = container.Resolve<IMessageService>()
                }
            });
        }
    }
}
