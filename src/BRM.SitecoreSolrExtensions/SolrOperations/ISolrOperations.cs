namespace BRM.Indexing.SitecoreSolrExtensions.SolrOperations
{
    using System.Collections.Generic;
    using Sitecore.ContentSearch.SolrNetExtension;

    //Interface to work around internal property
    public interface ISolrOperations
    {
        ISolrOperationsEx<Dictionary<string, object>> SolrOperations { get; }
    }
}