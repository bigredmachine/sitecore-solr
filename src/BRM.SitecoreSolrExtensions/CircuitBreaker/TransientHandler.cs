using System;
using BRM.Indexing.Domain;
using Polly.CircuitBreaker;

namespace BRM.Indexing.SitecoreSolrExtensions.CircuitBreaker
{
    public class TransientHandler : ITransientHandler
    {
        private readonly CircuitBreakerPolicy _circuitBreakerPolicy;

        public TransientHandler(CircuitBreakerPolicy circuitBreakerPolicy)
        {
            _circuitBreakerPolicy = circuitBreakerPolicy;
        }

        public TResult Execute<TResult>(Func<TResult> action)
        {
            try
            {
                return _circuitBreakerPolicy.Execute(action);
            }
            catch (BrokenCircuitException ex)
            {
                throw new SolrCircuitBreakerException("Solr Circuit Broken", ex);
            }
        }
    }
}