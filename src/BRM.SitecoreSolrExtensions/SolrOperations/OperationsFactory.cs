using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;
using Sitecore.ContentSearch.SolrNetExtension;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.ContentSearch.SolrProvider.SolrOperations;
using Sitecore.Diagnostics;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrOperations
{
    public class OperationsFactory : SolrOperationsFactory
    {
        private readonly ConcurrentDictionary<string, ISolrOperationsEx<Dictionary<string, object>>> innerList = new ConcurrentDictionary<string, ISolrOperationsEx<Dictionary<string, object>>>();
        private readonly SolrFactory<Dictionary<string, object>> solrFactory;

        public OperationsFactory()
          : this(SolrContentSearchManager.SolrSettings.ServiceAddress(), ServiceLocator.Current.GetInstance<SolrFactory<Dictionary<string, object>>>())
        {
        }

        public OperationsFactory(string endpoint, SolrFactory<Dictionary<string, object>> solrFactory) : base(endpoint, solrFactory)
        {
            Assert.ArgumentNotNull((object)solrFactory, "solrFactory");
            this.solrFactory = solrFactory;
        }

        public virtual ISolrOperationsEx<Dictionary<string, object>> GetSolrOperations(string core)
        {
            if (!this.innerList.ContainsKey(core))
                this.AddCore(core);
            return this.innerList[core];
        }

        protected void Clear()
        {
            this.innerList.Clear();
        }

        private void AddCore(string core)
        {
            this.innerList[core] = this.CreateOperation(core);
        }

        private ISolrOperationsEx<Dictionary<string, object>> CreateOperation(string core)
        {
            Assert.ArgumentNotNull((object)core, "core");
            string endPoint = string.Format("{0}/{1}", Endpoint, core);
            //Override creation to pass in query connection
            return solrFactory.CreateServer(solrFactory.CreateConnection(endPoint),
                solrFactory.CreateQueryConnection(endPoint));
        }
    }
}