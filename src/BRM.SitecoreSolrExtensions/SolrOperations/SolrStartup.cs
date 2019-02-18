using System;
using System.Collections.Generic;
using HttpWebAdapters;
using Sitecore.ContentSearch.SolrNetExtension.Schema;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.ContentSearch.SolrProvider.Abstractions;
using Sitecore.ContentSearch.SolrProvider.DocumentSerializers;
using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
using Sitecore.Diagnostics;
using SolrNet.Schema;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrOperations
{
    public class SolrStartup : DefaultSolrStartUp
    {
        public SolrStartup()
            : this (SolrContentSearchManager.HttpWebRequestFactory, SolrContentSearchManager.SolrSettings)
        {
        }

        public SolrStartup(IHttpWebRequestFactory requestFactory, BaseSolrSpecificSettings solrSettings)
        {
            Assert.ArgumentNotNull(solrSettings, nameof(solrSettings));
            Assert.ArgumentNotNull(requestFactory, nameof(requestFactory));
            this.SolrSettings = solrSettings;
            this.RequestFactory = requestFactory;
            //Override with our SolrLocator
            this.Operations = new SolrLocator<Dictionary<string, object>>()
            {
                HttpWebRequestFactory = requestFactory
            };
        }

        public override void Initialize()
        {
            if (!SolrContentSearchManager.IsEnabled)
            {
                throw new InvalidOperationException("Solr configuration is not enabled. Please check your settings and include files.");
            }

            this.Operations.DocumentSerializer = new SolrFieldBoostingDictionarySerializer(this.Operations.FieldSerializer);
            this.Operations.SchemaParser = SolrContentSearchManager.ManagedSchemaEnabled ? new SolrManagedSchemaParser() : (ISolrSchemaParser)new Sitecore.ContentSearch.SolrProvider.Parsers.SolrSchemaParser();
            //Override with out CloudConnector
            this.Operations.SolrConnector = this.SolrConnector ?? new CloudConnector();
            Microsoft.Practices.ServiceLocation.IServiceLocator locator = new DefaultServiceLocator<Dictionary<string, object>>(this.Operations);
            Microsoft.Practices.ServiceLocation.ServiceLocator.SetLocatorProvider(() => locator);
            SolrContentSearchManager.Initialize();
        }

        protected override BaseSolrSpecificSettings SolrSettings { get; }

        protected override IHttpWebRequestFactory RequestFactory { get; }

        protected override DefaultSolrLocator<Dictionary<string, object>> Operations { get; }
    }
}