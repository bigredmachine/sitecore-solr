using BRM.Indexing.SitecoreSolrExtensions.Configuration;
using HttpWebAdapters;
using Sitecore.ContentSearch.SolrNetExtension;
using Sitecore.ContentSearch.SolrNetExtension.Impl;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.ContentSearch.SolrProvider.Abstractions;
using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
using SolrNet;
using SolrNet.Impl;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrOperations
{
    public class SolrFactory<T> : ISolrFactory<T>
    {
        private readonly DefaultSolrLocator<T> locator;
        private readonly BaseSolrSpecificSettings solrSettings;

        public SolrFactory(DefaultSolrLocator<T> locator, BaseSolrSpecificSettings solrSettings)
        {
            this.locator = locator;
            this.solrSettings = solrSettings;
        }

        public ISolrConnectionEx CreateConnection(string serverUrl)
        {
            SolrConnectionEx solrConnectionEx = new SolrConnectionEx(serverUrl, this.solrSettings.SendPostRequests());
            int num = this.solrSettings.ConnectionTimeout();
            solrConnectionEx.Timeout = num;
            IHttpWebRequestFactory webRequestFactory = this.locator.HttpWebRequestFactory;
            solrConnectionEx.HttpWebRequestFactory = webRequestFactory;
            ISolrCache solrCache = this.GetSolrCache();
            solrConnectionEx.Cache = solrCache;
            return (ISolrConnectionEx)solrConnectionEx;
        }

        //Extra method added
        public ISolrConnectionEx CreateQueryConnection(string serverUrl)
        {
            SolrConnectionEx solrConnectionEx = new SolrConnectionEx(serverUrl, this.solrSettings.SendPostRequests());
            //New Setting - timeout for queries
            int num = Settings.ConnectionTimeoutForQueries;
            solrConnectionEx.Timeout = num;
            IHttpWebRequestFactory webRequestFactory = this.locator.HttpWebRequestFactory;
            solrConnectionEx.HttpWebRequestFactory = webRequestFactory;
            ISolrCache solrCache = this.GetSolrCache();
            solrConnectionEx.Cache = solrCache;
            return (ISolrConnectionEx)solrConnectionEx;
        }

        public ISolrOperationsEx<T> CreateServer(ISolrConnection connection)
        {
            //Call extra method
            return CreateServer(connection, connection);
        }

        //Extra method added
        public ISolrOperationsEx<T> CreateServer(ISolrConnection connection, ISolrConnection queryConnection)
        {
            return (ISolrOperationsEx<T>)new SolrServerEx<T>(this.CreateBasicServer(connection, queryConnection), this.locator.MappingManager, this.locator.MappingValidator);
        }

        public ISolrBasicOperationsEx<T> CreateBasicServer(ISolrConnection connection)
        {
            //Call extra method
            return CreateBasicServer(connection, connection);
        }

        //Extra method added
        public ISolrBasicOperationsEx<T> CreateBasicServer(ISolrConnection connection, ISolrConnection queryConnection)
        {
            //Make use of queryConnection
            SolrQueryExecuterEx<T> solrQueryExecuterEx = new SolrQueryExecuterEx<T>(this.locator.ResponseParser, queryConnection, this.locator.QuerySerializer, this.locator.FacetQuerySerializer, this.locator.MoreLikeThisHandlerQueryResultParser, this.locator.SuggestHandlerQueryResultParser, SolrContentSearchManager.SpellCheckHandler)
            {
                SuggestHandler = SolrContentSearchManager.SuggestHandler
            };
            return (ISolrBasicOperationsEx<T>)new SolrBasicServerEx<T>(connection, (ISolrQueryExecuterEx<T>)solrQueryExecuterEx, this.locator.DocumentSerializer, this.locator.SchemaParser, this.locator.HeaderParser, this.locator.QuerySerializer, this.locator.DihStatusParser, this.locator.ExtractResponseParser);
        }

        public ISolrBasicReadOnlyOperations<T> CreateBasicReadOnlyServer(ISolrConnection connection)
        {
            return (ISolrBasicReadOnlyOperations<T>)this.CreateBasicServer(connection);
        }

        public ISolrCoreAdminEx CreateCoreAdmin(ISolrConnection connection)
        {
            return (ISolrCoreAdminEx)new SolrCoreAdminEx(connection, this.locator.HeaderParser, this.locator.StatusResponseParser);
        }

        private ISolrCache GetSolrCache()
        {
            return !this.solrSettings.EnableHttpCache() ? (ISolrCache)new NullCache() : (ISolrCache)new HttpRuntimeCache();
        }
    }
}
