using System.Linq;
using Sitecore.Abstractions;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Pipelines.QueryGlobalFilters;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.ContentSearch.Security;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.ContentSearch.Utilities;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrProvider
{
    public class SolrSearchContextDoNotSwallowError : SolrSearchContext, IProviderSearchContext
    {
        private readonly IContentSearchConfigurationSettings _contentSearchSettings;

        public SolrSearchContextDoNotSwallowError(SolrSearchIndex solrSearchIndex, SearchSecurityOptions options)
            : base(solrSearchIndex, options)
        {
            _contentSearchSettings = solrSearchIndex.Locator.GetInstance<IContentSearchConfigurationSettings>();
        }

        IQueryable<TItem> Sitecore.ContentSearch.IProviderSearchContext.GetQueryable<TItem>()
        {
            return ((IProviderSearchContext)this).GetQueryable<TItem>(new IExecutionContext[0]);
        }

        IQueryable<TItem> Sitecore.ContentSearch.IProviderSearchContext.GetQueryable<TItem>(IExecutionContext executionContext)
        {
            return ((IProviderSearchContext)this).GetQueryable<TItem>(new IExecutionContext[]{
                executionContext
            });
        }

        public override IQueryable<TItem> GetQueryable<TItem>(params IExecutionContext[] executionContexts)
        {
            LinqToSolrIndexDoNotSwallowError<TItem> lingToSolrIndexDoNotSwallowError = new LinqToSolrIndexDoNotSwallowError<TItem>(this, executionContexts);

            if (_contentSearchSettings.EnableSearchDebug())
            {
                ((IHasTraceWriter)lingToSolrIndexDoNotSwallowError).TraceWriter = new LoggingTraceWriter(SearchLog.Log);
            }

            IQueryable<TItem> queryable = lingToSolrIndexDoNotSwallowError.GetQueryable();
            if (typeof(TItem).IsAssignableFrom(typeof(SearchResultItem)))
            {
                QueryGlobalFiltersArgs globalFiltersArgs = new QueryGlobalFiltersArgs(lingToSolrIndexDoNotSwallowError.GetQueryable(), typeof(TItem), executionContexts.ToList());
                this.Index.Locator.GetInstance<BaseCorePipelineManager>().Run("contentSearch.getGlobalLinqFilters", globalFiltersArgs);
                queryable = (IQueryable<TItem>)globalFiltersArgs.Query;
            }

            return queryable;
        }
    }
}