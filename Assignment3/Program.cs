using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Assignment3
{
    internal class Request
    {
        public string Method;
        public string Path;
        public string Date;
        public string Body;

        public Request(string method, string path, string date, string body)
        {
            Method = method;
            Path = path;
            Date = date;
            Body = body;
        }
    }
    

    internal static class Program
    {
        private static TcpListener _server;
        private static TcpClient _client;
        private static int _counter;

        private static string _data;

        // Buffer for reading data
        private static readonly byte[] Bytes = new byte[256];


        private static bool IsIn<T>(this T source, params T[] values)
        {
            return ((IList) values).Contains(source);
        }

        private static Response DealWithRequest(Request r)
        {
            var resp = Response();
            if (!r.Method.IsIn("create", "read", "update", "delete", "echo"))
            {
                resp.Status = 4;
                resp.Body = "Illegal method";
            }

            return resp;
        }

        private static void Main(string[] args)
        {

            try
            {
                // Set the TcpListener on port 5000.
                const int port = 5000;
                var localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener server = new TcpListener(port);
                _server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                _server.Start();

                // Enter the listening loop.
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    _client = _server.AcceptTcpClient();
                    _counter += 1;
                    Console.WriteLine("Connected!" + "Client No:" + Convert.ToString(_counter));
                    var connect = new Thread(HandleClient);
                    connect.Start();

                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                _server.Stop();
            }


            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }
        
       
        
        private static void HandleClient()
        {
            _data = null;
            // Get a stream object for reading and writing
            var stream = _client.GetStream();

            int i;

            // Loop to receive all the data sent by the client.
            while ((i = stream.Read(Bytes, 0, Bytes.Length)) != 0)
            {
                // Translate data bytes to a ASCII string.
                _data = Encoding.ASCII.GetString(Bytes, 0, i);
                Console.WriteLine("Received: {0}", _data);

                // Process the data sent by the client.
                _data = _data.ToUpper();
                var r = JsonConvert.DeserializeObject<Request>(_data);

                var response = DealWithRequest(r);

                var msg = Encoding.ASCII.GetBytes(_data);
                // Send back a response.
                stream.Write(msg, 0, msg.Length);
                Console.WriteLine("Sent: {0}", _data);
            }

            // Shutdown and end connection
            _client.Close();
        }

        
    }
}
