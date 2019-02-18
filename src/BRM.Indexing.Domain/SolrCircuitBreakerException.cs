using System;
using System.Runtime.Serialization;

namespace BRM.Indexing.Domain
{
    [Serializable]
    public class SolrCircuitBreakerException : Exception
    {
        public SolrCircuitBreakerException()
        {
        }

        public SolrCircuitBreakerException(string message) : base(message)
        {
        }

        public SolrCircuitBreakerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public SolrCircuitBreakerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}