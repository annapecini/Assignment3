using System;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Globalization;

namespace Client
{
    internal static class Program
    {
        public static double DateToUnixTimestamp(string date)
        {
            string format = "dd/MM/yyyy";
            CultureInfo provider = CultureInfo.InvariantCulture;
            var value = DateTime.ParseExact( date, format, provider);
            return (new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)-
                    TimeZoneInfo.ConvertTimeToUtc(value)).TotalSeconds;
        }

        private static void Main(string[] args)
        {
            try 
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer 
                // connected to the same address as specified by the server, port
                // combination.
                const int port = 5000;
                var client = new TcpClient("127.0.0.1", port);
    
                // Translate the passed message into ASCII and store it as a Byte array.
                var stream = client.GetStream();
//                var data = System.Text.Encoding.ASCII.GetBytes(args[1]);         
                while (true)
                {
                    // Get a client stream for reading and writing.
                    //  Stream stream = client.GetStream();


                    // Send the message to the connected TcpServer. 
                    Console.WriteLine("Method ?");
                    var method = Console.ReadLine();
                    Console.WriteLine("Path ?");
                    var path = Console.ReadLine();
                    Console.WriteLine("Date (dd/mm/yyyy) ?");
                    var date = Console.ReadLine();
                    Console.WriteLine("Body ?");
                    var body = Console.ReadLine();
                    var data = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new
                    {
                        Method = method,
                        Path = path,
                        // Convert date input to UNIX timestamp
                        Date = DateToUnixTimestamp(date),
                        Body = body
                    }));
                    stream.Write(data, 0, data.Length);
                    Console.WriteLine("Sent: {0}", data);

                    // Receive the TcpServer.response.

                    // Buffer to store the response bytes.
                    data = new byte[256];

                    // String to store the response ASCII representation.

                    // Read the first batch of the TcpServer response bytes.
                    var bytes = stream.Read(data, 0, data.Length);
                    var responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    Console.WriteLine("Received: {0}", responseData);
                }

                // Close everything.
                stream.Close();         
                client.Close();         
            } 
            catch (ArgumentNullException e) 
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            } 
            catch (SocketException e) 
            {
                Console.WriteLine("SocketException: {0}", e);
            }
    
            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }
    }
}