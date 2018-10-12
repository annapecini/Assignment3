﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Assignment3 {

    internal static class Program {
  
        private static TcpListener _server;
        private static TcpClient _client;
        private static int _counter;

        private static string _data;

        // Buffer for reading data
        private static readonly byte[] Bytes = new byte[256];


        private static bool IsIn<T>(this T source, params T[] values) {
            return ((IList) values).Contains(source);
        }

        private static string CheckError(Request r) {
            string error = null;

            var reasons = new List<string>();

            if (r.Method == "") reasons.Add("missing method");
            if (r.Path == "") reasons.Add("missing path");
            if (r.Date == 0) reasons.Add("missing date");

            if (r.Method != "" && !r.Method.IsIn("create", "read", "update", "delete", "echo")) {
                reasons.Append("illegal method");
            }

            if (reasons.Count > 0) {
                error = "4 ";
            }
            for (var i = 0; i < reasons.Count; i++)
            {
                if (i != 0) {
                    error = error + ", ";
                }
                error = error + reasons[i];
            }
            return error;
        }
        
        private static Response DealWithRequest(Request r) {
            var resp = new Response();
            string err;
            
//            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
//            dateTime = dateTime.AddSeconds(r.Date);

            if ((err = CheckError(r)) != null) {
                resp.Status = err;
                resp.Body = "";
                return resp;
            }

            switch (r.Method) {
                case "create":
                    CaseCreate(r, ref resp);
                    break;
                case "read":
                    CaseRead(r, ref resp);
                    break;
                case "update":
                    CaseUpdate(r, ref resp);
                    break;
                case "delete":
                    CaseDelete(r, ref resp);
                    break;
                case "echo":
                    CaseEcho(ref resp);
                    break;
            }
            return resp;
        }
        
        /**
         * The Response resp if passed by reference so we don't have to return it in every functions
         */
        
        // Different cases
        
        private static void CaseRead(Request r, ref Response resp) {
            if (IsPathOk(r, ref resp, false) == 1)
                return;
            if (r.Path == "/categories")
            {
                resp.Status = "1 Ok";
                resp.Body = JsonConvert.SerializeObject(Globals.Db);
            }
            else
            {
                var id = IsIdOk(r, ref resp);
                if (id == -1) {
                    return;
                }
                if (DoesIdExists(id))
                {
                    resp.Status = "1 Ok";
                    resp.Body = JsonConvert.SerializeObject(Globals.Db[id]);
                    return;
                }
                resp.Status = "5 Not found";
            }
        }
        
        private static void CaseCreate(Request r, ref Response resp) {
            if (IsPathOk(r, ref resp, false) == 1)
                return;
            
            var body = JsonConvert.DeserializeObject<Category>(r.Body);
            Interlocked.Increment(ref Globals.Cid);
            var cat = new Category {Uid = Globals.Cid - 1, Name = body.Name};
            Globals.Db.Add(cat);
            resp.Status = "2 Created";
            resp.Body = JsonConvert.SerializeObject(cat);
        }
        
        private static void CaseUpdate(Request r, ref Response resp) {
            if (IsPathOk(r, ref resp) == 1) {
                return;
            }
            var id = IsIdOk(r, ref resp);
            if (id == -1) {
                return;
            }           
            
            var body = JsonConvert.DeserializeObject<Category>(r.Body);
            resp.Body = "";
            if (DoesIdExists(id)) {
                Globals.Db[id].Name = body.Name;
                resp.Status = "3 Updated";
                return;
            }
            resp.Status = "4 Bad Request";
        }
        
        private static void CaseDelete(Request r, ref Response resp) {
            if (IsPathOk(r, ref resp) == 1) {
                return;
            }

            var id = IsIdOk(r, ref resp);
            if (id == -1) {
                return;
            }

            resp.Body = "";
            if (DoesIdExists(id)) {
                Globals.Db.RemoveAt(id);
                resp.Status = "1 Ok";
                return;
            }
            resp.Status = "5 Not found";
        }
        
        private static void CaseEcho(ref Response resp) {
            resp.Status = "1 Ok";
            resp.Body = "";
        }
        // End of cases

        /**
         * Check if id is in the database
         */
        private static bool DoesIdExists(int id)
        {
            foreach (var t in Globals.Db)
            {
                if (t.Uid == id)
                    return true;
            }
            return false;
        }
        
        /**
         * Check if the path starts with "/categories/" or "/categories"
         * The 'bool full' is used to check if there is a "/" after "categories" or not
         */
        private static int IsPathOk(Request r, ref Response resp, bool full = true)
        {
            var url = "/categories" + (full ? "/" : "");
            if (r.Path.StartsWith(url)) {
                return 0;
            }
            resp.Status = "4 Bad request";
            return 1;
        }

        /**
         * Check if the path contains an id and if this id is only numeric
         */
        private static int IsIdOk(Request r, ref Response resp) {
            var id = -1;
            
            try {
                id = Convert.ToInt32(r.Path.Substring(12));
            } catch (FormatException) {
                resp.Status = "4 Bad request";
            }

            return id;
        }

        private static void Main(string[] args) {

            try {
                // Set the TcpListener on port 5000.
                const int port = 5000;
                var localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener server = new TcpListener(port);
                _server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                _server.Start();

                // Enter the listening loop.
                while (true) {
                    Console.WriteLine("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    _client = _server.AcceptTcpClient();
                    _counter += 1;
                    Console.WriteLine("Connected!" + "Client No:" + Convert.ToString(_counter));
                    var connect = new Thread(HandleClient);
                    connect.Start();

                }
            }
            catch (SocketException e) {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally {
                // Stop listening for new clients.
                _server.Stop();
            }


            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }
        
        private static void HandleClient() {
            _data = null;
            // Get a stream object for reading and writing
            var stream = _client.GetStream();

            int i;

            // Loop to receive all the data sent by the client.
            while ((i = stream.Read(Bytes, 0, Bytes.Length)) != 0) {
                // Translate data bytes to a ASCII string.
                _data = Encoding.ASCII.GetString(Bytes, 0, i);
                Console.WriteLine("Received: {0}", _data);

                // Process the data sent by the client.
                var r = JsonConvert.DeserializeObject<Request>(_data);

                var response = DealWithRequest(r);

                // Send back a response.
                var data = JsonConvert.SerializeObject(response);
                stream.Write(Encoding.ASCII.GetBytes(data), 0, data.Length);
            }

            // Shutdown and end connection
            _client.Close();
        }
    }
}