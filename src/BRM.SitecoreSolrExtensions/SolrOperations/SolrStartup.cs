using System.Collections.Generic;
using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
using System.Reflection;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrOperations
{
    public class SolrStartup : DefaultSolrStartUp
    {
        private const string DefaultSolrStartUp_Operations_PropertyName = "Operations";

        public SolrStartup() : base()
        {
        }

        public override void Initialize()
        {
            var solrLocator = new SolrLocator<Dictionary<string, object>>();
            var defaultSolrStartupType = typeof(DefaultSolrStartUp);
            defaultSolrStartupType.GetProperty(DefaultSolrStartUp_Operations_PropertyName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(this, solrLocator, null);

            base.Initialize();
        }
    }
}