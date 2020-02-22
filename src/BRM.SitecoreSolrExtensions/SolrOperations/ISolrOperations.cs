using System.Collections.Generic;
using Sitecore.ContentSearch.SolrNetExtension;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrOperations
{
    //Interface to work around internal property
    public interface ISolrOperations
    {
        ISolrOperationsEx<Dictionary<string, object>> SolrOperations { get; }
    }
}