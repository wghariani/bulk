using System;
using ServiceStack;
using ServiceStack.RabbitMq;
using ServiceStack.Text;
using Documat.LogSender;

namespace Documat.LogReceiver
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var rabbitMqServer = new RabbitMqServer();

            // Register to receive log entries
            rabbitMqServer.RegisterHandler<RequestLogEntry>(message =>
                {
                    var requestLogEntry = message.GetBody();

                    // log request log entry to console 
                    requestLogEntry.PrintDump();

                    return null;
                });
            rabbitMqServer.Start();

            "Listening for logs and messages".Print();

            Console.ReadLine();

            rabbitMqServer.Dispose();
        }
    }
}
