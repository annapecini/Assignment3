using System;
using System.Net.Sockets;

namespace Client
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            try 
            {
                if (args.Length < 2)
                    return;
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer 
                // connected to the same address as specified by the server, port
                // combination.
                const int port = 5000;
                var client = new TcpClient(args[0], port);
    
                // Translate the passed message into ASCII and store it as a Byte array.
                var data = System.Text.Encoding.ASCII.GetBytes(args[1]);         

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();
    
                var stream = client.GetStream();

                // Send the message to the connected TcpServer. 
                stream.Write(data, 0, data.Length);

                Console.WriteLine("Sent: {0}", args[1]);         

                // Receive the TcpServer.response.
    
                // Buffer to store the response bytes.
                data = new byte[256];

                // String to store the response ASCII representation.

                // Read the first batch of the TcpServer response bytes.
                var bytes = stream.Read(data, 0, data.Length);
                var responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);         

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