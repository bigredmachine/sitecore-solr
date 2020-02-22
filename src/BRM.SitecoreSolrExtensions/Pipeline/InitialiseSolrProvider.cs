using BRM.Indexing.SitecoreSolrExtensions.SolrOperations;
using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
using Sitecore.Pipelines;

namespace BRM.Indexing.SitecoreSolrExtensions.Pipeline
{
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