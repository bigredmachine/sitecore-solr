namespace BRM.Indexing.Domain
{
    public interface ISolrCircuitBreakerPolicyProvider
    {
        ITransientHandler GetCircuitBreakerPolicy(string indexName);
    }
}