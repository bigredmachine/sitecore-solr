using Sitecore;

namespace BRM.Indexing.SitecoreSolrExtensions.Configuration
{
    public class Settings
    {
        public static string IndexingInstance = StringUtil.GetString(new string[]
        {
            Sitecore.Configuration.Settings.GetSetting("ContentSearch.Solr.IndexingInstance"),
            Sitecore.Configuration.Settings.InstanceName
        });

        public static int CircuitBreakerConsecutiveExceptionsBeforeBreaking = Sitecore.Configuration.Settings.GetIntSetting("ContentSearch.Solr.CircuitBreaker.ConsecutiveExceptionsBeforeBreaking", 2);
        public static int CircuitBreakerDurationInMins = Sitecore.Configuration.Settings.GetIntSetting("ContentSearch.Solr.CircuitBreaker.DurationInMins", 1);
    }
}