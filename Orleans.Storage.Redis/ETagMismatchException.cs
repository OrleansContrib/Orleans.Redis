using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Storage
{
    public class ETagMismatchException : Exception
    {
        public string ETag { get; set; }

        public ETagMismatchException(string etag, string message) : base(message)
        {
            ETag = etag;
        }
    }
}
