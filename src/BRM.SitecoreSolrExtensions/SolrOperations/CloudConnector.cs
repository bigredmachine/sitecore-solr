using System.Collections.Generic;
using Sitecore.ContentSearch.SolrProvider.SolrConnectors;
using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
using Sitecore.Diagnostics;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrOperations
{
    public class CloudConnector : SolrCloudConnector
    {
        protected override void BuildOperationsFactory(string solrAddress, ISolrFactory<Dictionary<string, object>> solrFactory)
        {
            //Ensure being passed in the overriden SolrFactory
            var brmSolrFactory = solrFactory as SolrFactory<Dictionary<string, object>>;
            Assert.IsNotNull(brmSolrFactory, "Expected solrFactory to be of Type SolrFactory");

            //Pass in upcast solr factory
            this.OperationsFactory = new OperationsFactory(solrAddress, brmSolrFactory);
        }
    }
}