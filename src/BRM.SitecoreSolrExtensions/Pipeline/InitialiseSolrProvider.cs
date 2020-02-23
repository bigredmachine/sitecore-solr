namespace BRM.Indexing.SitecoreSolrExtensions.Pipeline
{
    using BRM.Indexing.SitecoreSolrExtensions.SolrOperations;
    using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
    using Sitecore.Pipelines;

    public class InitialiseSolrProvider
    {
        public void Process(PipelineArgs args)
        {
            if (IntegrationHelper.IsSolrConfigured())
            {
                IntegrationHelper.ReportDoubleSolrConfigurationAttempt(this.GetType());
            }
            else
            {
                //Override with our custom SolrStartup
                new SolrStartup().Initialize();
            }
        }
    }
}