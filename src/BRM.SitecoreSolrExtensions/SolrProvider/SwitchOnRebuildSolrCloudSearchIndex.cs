using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using BRM.Indexing.SitecoreSolrExtensions.Configuration;
using BRM.Indexing.SitecoreSolrExtensions.SolrOperations;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.ContentSearch.Security;
using Sitecore.ContentSearch.SolrNetExtension;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.ContentSearch.Utilities;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrProvider
{
    public class SwitchOnRebuildSolrCloudSearchIndex : Sitecore.ContentSearch.SolrProvider.SwitchOnRebuildSolrCloudSearchIndex, ISolrOperations
    {
        private bool _doNotSwallowError;

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
                  propertystore)
        {
            if (!string.IsNullOrWhiteSpace(doNotSwallowError))
            {
                bool.TryParse(doNotSwallowError, out _doNotSwallowError);
            }
        }

        public override IProviderSearchContext CreateSearchContext(SearchSecurityOptions options = SearchSecurityOptions.Default)
        {
            if (Group == IndexGroup.Experience)
            {
                return new SolrAnalyticsSearchContext(this, options);
            }

            if (!_doNotSwallowError)
            {
                return new SolrSearchContext(this, options);
            }

            if (_doNotSwallowError && !IsInitialized)
            {
                throw new Sitecore.Exceptions.ConfigurationException("Index not yet initialized!");
            }

            return new SolrSearchContextDoNotSwallowError(this, options);
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

        protected override void PerformRebuild(bool resetIndex, bool optimizeOnComplete, IndexingOptions indexingOptions, CancellationToken cancellationToken)
        {
            if (!this.ShouldStartIndexing(indexingOptions))
            {
                return;
            }

            using (new RebuildIndexingTimer(PropertyStore))
            {
                if (resetIndex)
                {
                    Reset(this.RebuildSolrOperations, RebuildCore);
                }

                using (IProviderUpdateContext coreUpdateContext = this.CreateTemporaryCoreUpdateContext(this.RebuildSolrOperations))
                {
                    foreach (IProviderCrawler crawler in Crawlers)
                    {
                        crawler.RebuildFromRoot(coreUpdateContext, indexingOptions, cancellationToken);
                    }

                    coreUpdateContext.Commit();
                }

                if (optimizeOnComplete)
                {
                    //Customisation - sitecore bug fix 235313
                    if (SolrContentSearchManager.SolrSettings.OptimizeOnRebuildEnabled())
                    {
                        CrawlingLog.Log.Debug(string.Format("[Index={0}] Optimizing core [Core: {1}]", Name, RebuildCore), null);
                        this.RebuildSolrOperations.Optimize();
                    }
                }
            }

            if ((this.IndexingState & IndexingState.Stopped) == IndexingState.Stopped)
            {
                CrawlingLog.Log.Debug(string.Format("[Index={0}] Swapping of cores was not done since full rebuild was stopped...", Name), null);
            }
            else
            {
                SwapAfterRebuild();
            }
        }
    }
}