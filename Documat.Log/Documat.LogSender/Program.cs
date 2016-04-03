using System;
using ServiceStack;
using ServiceStack.Text;
using ServiceStack.RabbitMq;

namespace Documat.LogSender
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var licensePath = @"~/../../../../license.txt".MapHostAbsolutePath();
            Licensing.RegisterLicenseFromFileIfExists(licensePath);

            var url = "http://*:5555/";
            var appHost = new AppHost();
            appHost.Init();
            appHost.Start(url);

            "Visit http://localhost:5555/hello/wajdi to queue request log".Print();
            Console.ReadLine();
        }
    }
}
