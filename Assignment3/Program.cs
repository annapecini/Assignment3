using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

            if (string.IsNullOrEmpty(r.Method)) reasons.Add("missing method");
            if (r.Method != "echo" && string.IsNullOrEmpty(r.Path)) reasons.Add("missing resource");
            if (string.IsNullOrEmpty(r.Date)) reasons.Add("missing date");
            try
            {
                Convert.ToDouble(r.Date);
            }
            catch (Exception e)
            {
                reasons.Add("illegal date");
            }
            

            if (!string.IsNullOrEmpty(r.Method) && r.Method != "" && !r.Method.IsIn("create", "read", "update", "delete", "echo")) {
                reasons.Add("illegal method");
            }
            
            if (r.Method.IsIn("create", "update", "echo"))
            {
                
                if (string.IsNullOrEmpty(r.Body))
                    reasons.Add("missing body");
                else if (r.Method.IsIn("create", "update"))
                {
                    switch (r.Method)
                    {
                        case "create" when reasons.Count == 0 && !string.IsNullOrEmpty(r.Path) && r.Path.Length != "/api/categories".Length:
                        case "update" when reasons.Count == 0 && !string.IsNullOrEmpty(r.Path) && r.Path.Length <= "/api/categories/".Length:
                            return "4 Bad Request";
                        default:
                            try
                            {
                                var body = JsonConvert.DeserializeObject<Category>(r.Body);
                                if (string.IsNullOrEmpty(body.Cid.ToString()) || string.IsNullOrEmpty(body.Name))
                                    reasons.Add("illegal body");
                            }
                            catch (JsonReaderException)
                            {
                                reasons.Add("illegal body");
                            }

                            break;
                    }
                }
                
            }

            if (reasons.Count > 0) {
                error = "4 ";
            }
            for (var i = 0; i < reasons.Count; i++)
            {
                if (i != 0) {
                    error += ", ";
                }
                error += reasons[i];
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
//                resp.Body = "";
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
                    CaseEcho(r, ref resp);
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
            if (r.Path == "/api/categories")
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
                    resp.Body = JsonConvert.SerializeObject(GetCategory(id));
                    return;
                }
                resp.Status = "5 Not found";
            }
        }
        
        private static void CaseCreate(Request r, ref Response resp) {            
            if (IsPathOk(r, ref resp, false, true) == 1)
                return;

            var body = JsonConvert.DeserializeObject<Category>(r.Body);
            Interlocked.Increment(ref Globals.Cid);
            var cat = new Category {Cid = Globals.Cid - 1, Name = body.Name};
            Globals.Db.Add(cat);
            resp.Status = "2 Created";
            resp.Body = JsonConvert.SerializeObject(cat);
        }

        private static void CaseUpdate(Request r, ref Response resp)
        {
            if (IsPathOk(r, ref resp) == 1)
            {
                return;
            }

            var id = IsIdOk(r, ref resp);
            if (id == -1)
            {
                resp.Status = "5 Not found";
                return;
            }

            try
            {
                var body = JsonConvert.DeserializeObject<Category>(r.Body);
                resp.Body = "";
                if (!DoesIdExists(id))
                {
                    resp.Status = "5 Not found";
                    return;
                }
                GetCategory(id).Name = body.Name;
                resp.Status = "3 Updated";
            }
            catch (JsonReaderException)
            {
                resp.Status = "4 Bad Request";
            }
        }

        private static Category GetCategory(int id)
        {
            foreach (var t in Globals.Db)
            {
                if (t.Cid == id)
                    return t;
            }
            return null;
        }
        
        private static void CaseDelete(Request r, ref Response resp) {
            if (IsPathOk(r, ref resp) == 1) {
                return;
            }

            var id = IsIdOk(r, ref resp);
            if (id == -1) {
                resp.Status = "5 Not found";
                return;
            }

            resp.Body = "";
            if (DoesIdExists(id)) {
                Globals.Db.RemoveAt(Globals.Db.IndexOf(GetCategory(id)));
                resp.Status = "1 Ok";
                return;
            }
            resp.Status = "5 Not found";
        }
        
        private static void CaseEcho(Request r, ref Response resp) {
            resp.Status = "1 Ok";
            resp.Body = r.Body;
        }
        // End of cases

        /**
         * Check if id is in the database
         */
        private static bool DoesIdExists(int id)
        {
            foreach (var t in Globals.Db)
            {
                if (t.Cid == id)
                    return true;
            }
            return false;
        }
        
        /**
         * Check if the path starts with "/categories/" or "/categories"
         * The 'bool full' is used to check if there is a "/" after "categories" or not
         */
        private static int IsPathOk(Request r, ref Response resp, bool full = true, bool strict = false)
        {
            var url = "/api/categories" + (full ? "/" : "");
            if (r.Path.StartsWith(url)) {
                if (!strict) return 0;
                Console.WriteLine(r.Path.Length);
                if (r.Path.Length == 15)
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
                id = Convert.ToInt32(r.Path.Substring(16));
            } catch (FormatException) {
                resp.Status = "4 Bad request";
            }

            return id;
        }

        public static void Main(string[] args) {

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
            }
                _server.Stop();


            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }
        
        private static void HandleClient() {
            _data = null;
            // Get a stream object for reading and writing
            var stream = _client.GetStream();

            // Loop to receive all the data sent by the client.
            try
            {
                int i;
                while ((i = stream.Read(Bytes, 0, Bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    _data = Encoding.ASCII.GetString(Bytes, 0, i);

                    // Process the data sent by the client.
                    var r = JsonConvert.DeserializeObject<Request>(_data);

                    var response = DealWithRequest(r);

                    // Send back a response.
                    var data = JsonConvert.SerializeObject(response);
                    Console.WriteLine("Received: {1}\nSent: {0}", data, _data);
                    stream.Write(Encoding.ASCII.GetBytes(data), 0, data.Length);
                }
            }
            catch (IOException)
            {
                
            }

            // Shutdown and end connection
//            _client.Close();
        }
    }
}