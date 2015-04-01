using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using mq_entities;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace mq_client
{
    class Program
    {
        static void Main(string[] args)
        {
            Person p = new Person();
            p.name = "Testing Name";
            p.age = 90;
            p.SetPicture(Image.FromFile(@"C:\Users\gilmichael.regalado\Desktop\login.png"));
            
            var rpcClient = new RPCClient();
            Console.WriteLine(" [x] Requesting info for '{0}'", p.name);
            
            var response = rpcClient.Call(p);
            Person m = JsonConvert.DeserializeObject<Person>(response);

            Console.WriteLine(" [.] Got '{0}'", m.name);
            

            rpcClient.Close();
            Console.ReadKey();
        }
    }

    class RPCClient
    {
        private IConnection connection;
        private IModel channel;
        private string replyQueueName;
        private QueueingBasicConsumer consumer;
        
        public RPCClient()
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.UserName = "overwatch";
            factory.Password = "overwatch";
            factory.VirtualHost = "/";
            factory.Protocol = Protocols.DefaultProtocol;
            // factory.HostName = "phtemhm-autovm1";
            factory.HostName = "10.164.246.171";
            factory.Port = AmqpTcpEndpoint.UseDefaultPort;
            factory.RequestedConnectionTimeout = 1000 * 60 * 2;

            connection = factory.CreateConnection();
            channel = connection.CreateModel();



            replyQueueName = channel.QueueDeclare();
            consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(replyQueueName, true, consumer);
        }

        public string Call(Person person)
        {
            var corrId = Guid.NewGuid().ToString();
            var props = channel.CreateBasicProperties();
            props.ReplyTo = replyQueueName;
            props.CorrelationId = corrId;

            string messageString = JsonConvert.SerializeObject(person);
            var messageBytes = Encoding.UTF8.GetBytes(messageString);

            Console.WriteLine(" [/] Sending message '{0}' bytes long.", messageBytes.Length);
            
            channel.BasicPublish("", "rpc_queue", props, messageBytes);
            
            while (true)
            {
                var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
                if (ea.BasicProperties.CorrelationId == corrId)
                {
                    return Encoding.UTF8.GetString(ea.Body);
                }
            }
        }

        public void Close()
        {
            connection.Close();
        }
    }
}
