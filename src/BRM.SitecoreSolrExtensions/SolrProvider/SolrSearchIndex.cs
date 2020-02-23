using System.Collections.Generic;
using System.Configuration;
using BRM.Indexing.SitecoreSolrExtensions.Configuration;
using BRM.Indexing.SitecoreSolrExtensions.SolrOperations;
using Sitecore.ContentSearch.Abstractions.Factories;
using Sitecore.ContentSearch.Linq.Factories;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.ContentSearch.SolrNetExtension;
using Sitecore.ContentSearch.SolrProvider.Abstractions;
using Sitecore.ContentSearch.SolrProvider.Factories;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrProvider
{
    public class SolrSearchIndex : Sitecore.ContentSearch.SolrProvider.SolrSearchIndex, ISolrOperations
    {
        public SolrSearchIndex(string name,
            string activecollection,
            IIndexPropertyStore propertyStore)
            : this (name, activecollection, null, propertyStore)
        {
        }

        public SolrSearchIndex(string name,
            string activecollection,
            string doNotSwallowError,
            IIndexPropertyStore propertystore)
            : base(name,
                  activecollection,
                  propertystore,
                  (ISolrProviderContextFactory)new SolrContextFactoryNoSwallowError((ILinqToIndexFactory)new SolrLinqToIndexFactory((IQueryableFactory)new DefaultQueryableFactory()), doNotSwallowError),
                  null)
        {
        }

        //Expose internal method through an explict interface
        ISolrOperationsEx<Dictionary<string, object>> ISolrOperations.SolrOperations
        {
            get { return base.SolrOperations; }
        }

        public override void Rebuild(bool resetIndex = true, bool optimizeOnComplete = true)
        {
            if (Sitecore.Configuration.Settings.InstanceName != Settings.IndexingInstance)
            {
                throw new ConfigurationException("Failed to rebuild index, please use indexing instance!");
            }

            base.Rebuild(resetIndex, optimizeOnComplete);
        }
    }
}