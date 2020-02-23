namespace BRM.Indexing.SitecoreSolrExtensions.CircuitBreaker
{
    using BRM.Indexing.Domain;
    using CommonServiceLocator;

    //This needs to be registered in your DI container
    public class SolrCircuitBreakerPolicyProvider : ISolrCircuitBreakerPolicyProvider
    {
        public ITransientHandler GetCircuitBreakerPolicy(string indexName)
        {
            return ServiceLocator.Current.GetInstance<ITransientHandler>(indexName);
        }
    }
}