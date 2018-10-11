using System;
using System.Collections.Generic;
using System.Text;

namespace Assignment3
{
    class Response
    {
        public string Status { get; set; }
        public string Body { get; set; }
        
        public Response(string status, string body)
        {
            this.Status = status;
            this.Body = body;
        }
    }
}
