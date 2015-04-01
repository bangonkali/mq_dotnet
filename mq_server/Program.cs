using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using mq_entities;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using System.Timers;
using Topshelf;
using System.Drawing;
using System.IO;

namespace mq_server
{
    public class MQ_Server
    {
        readonly Timer _timer;
        private ConnectionFactory factory;

        public MQ_Server()
        {
            // _timer = new Timer(1000) { AutoReset = true };
            // _timer.Elapsed += (sender, eventArgs) => Console.WriteLine("It is {0} and all is well", DateTime.Now);
        }

        public void Start()
        {
//            _timer.Start();
            ServerStart();
        }

        public void Stop()
        {
//            _timer.Stop();
        }

        private void ServerStart()
        {
            factory = new ConnectionFactory();
            factory.UserName = "overwatch";
            factory.Password = "overwatch";
            factory.VirtualHost = "/";
            factory.Protocol = Protocols.DefaultProtocol;
            // factory.HostName = "phtemhm-autovm1";
            factory.HostName = "phtemhm-autovm1";
            factory.Port = AmqpTcpEndpoint.UseDefaultPort;

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare("rpc_queue", false, false, false, null);
                    channel.BasicQos(0, 1, false);
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume("rpc_queue", false, consumer);
                    Console.WriteLine(" [x] Awaiting RPC requests");

                    while (true)
                    {
                        string response = null;
                        var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();

                        var body = ea.Body;
                        var props = ea.BasicProperties;
                        var replyProps = channel.CreateBasicProperties();
                        replyProps.CorrelationId = props.CorrelationId;

                        try
                        {
                            response = DoWork(body);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(" [.] " + e.Message);
                            response = "";
                        }
                        finally
                        {
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            channel.BasicPublish("", props.ReplyTo, replyProps, responseBytes);
                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                    }
                }
            }
        }

        private string DoWork(byte[] message)
        {
            // Deserialize object
            string messageString = Encoding.UTF8.GetString(message);

            Console.WriteLine(" [/] Received message '{0}' bytes long.", message.Length);

            Person m = JsonConvert.DeserializeObject<Person>(messageString);

            // Do some work.
            m.name = "Some Person: " + m.name;
            Image img = m.GetPicture();
            var myUniqueFileName = Path.Combine(Path.GetTempPath(), string.Format(@"{0}.png", Guid.NewGuid()));
            img.Save(myUniqueFileName);
            img.Dispose();
            Process.Start(myUniqueFileName);

            // Return serialized object.
            return JsonConvert.SerializeObject(m);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<MQ_Server>(s =>
                {
                    s.ConstructUsing(name => new MQ_Server());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Sample Topshelf Host");
                x.SetDisplayName("Stuff"); 
                x.SetServiceName("Stuff");
            });
        }
    }
}
