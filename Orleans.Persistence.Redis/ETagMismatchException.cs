using System;

namespace Orleans.Persistence
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
