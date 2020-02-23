using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Abstractions.Factories;
using Sitecore.ContentSearch.Security;
using Sitecore.ContentSearch.SolrProvider.Factories;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrProvider
{
    public class SolrContextFactoryNoSwallowError : SolrContextFactory
    {
        private readonly ILinqToIndexFactory _linqToSolrFactory;
        private readonly bool _doNotSwallowError;

        public SolrContextFactoryNoSwallowError(ILinqToIndexFactory linqToSolrFactory, string doNotSwallowError)
            : base(linqToSolrFactory)
        {
            _linqToSolrFactory = linqToSolrFactory;
            if (!string.IsNullOrWhiteSpace(doNotSwallowError))
            {
                bool.TryParse(doNotSwallowError, out _doNotSwallowError);
            }
        }

        protected override IProviderSearchContext GetSearchContext(Sitecore.ContentSearch.SolrProvider.SolrSearchIndex searchIndex, SearchSecurityOptions options)
        {
            if (_doNotSwallowError)
            {
                return new SolrSearchContextDoNotSwallowError(searchIndex, _linqToSolrFactory, options);
            }

            return base.GetSearchContext(searchIndex, options);
        }
    }
}