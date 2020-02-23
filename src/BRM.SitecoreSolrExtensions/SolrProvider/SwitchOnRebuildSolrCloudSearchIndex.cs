namespace BRM.Indexing.SitecoreSolrExtensions.SolrProvider
{
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

    public class SwitchOnRebuildSolrCloudSearchIndex : Sitecore.ContentSearch.SolrProvider.SwitchOnRebuildSolrCloudSearchIndex, ISolrOperations
    {
        public SwitchOnRebuildSolrCloudSearchIndex(string name,
            string mainalias,
            string rebuildalias,
            string activecollection,
            string rebuildcollection,
            IIndexPropertyStore propertyStore)
            : this (name, mainalias, rebuildalias, activecollection, rebuildcollection, null, propertyStore)
        {
        }

        public SwitchOnRebuildSolrCloudSearchIndex(string name,
            string mainalias,
            string rebuildalias,
            string activecollection,
            string rebuildcollection,
            string doNotSwallowError,
            IIndexPropertyStore propertystore)
            : base(name,
                  mainalias,
                  rebuildalias,
                  activecollection,
                  rebuildcollection,
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