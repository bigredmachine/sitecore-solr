using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Xml;
using BRM.Indexing.Domain;
using BRM.Indexing.SitecoreSolrExtensions.SolrOperations;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Factories;
using Sitecore.ContentSearch.Linq.Methods;
using Sitecore.ContentSearch.Linq.Nodes;
using Sitecore.ContentSearch.Linq.Parsing;
using Sitecore.ContentSearch.Linq.Solr;
using Sitecore.ContentSearch.SolrProvider.Logging;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Diagnostics;
using SolrNet;
using SolrNet.Commands.Parameters;
using SolrNet.Exceptions;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrProvider
{
    //Reflection required to work around internal/private references, not ideal
    //all to avoid returning back empty lists
    //and implement circuit breaker pattern
    public class LinqToSolrIndexDoNotSwallowError<TItem> : Sitecore.ContentSearch.SolrProvider.LinqToSolrIndex<TItem>
    {
        private const string SolrSearchResultsType = "Sitecore.ContentSearch.SolrProvider.SolrSearchResults`1";
        private const string GetSearchResultsMethod = "GetSearchResults";
        private const string ApplyScalarMethodsMethod = "ApplyScalarMethods";
        private const string BuildQueryOptionsMethod = "BuildQueryOptions";
        private const string DeterminateCultureContextsMethod = "DeterminateCultureContexts";
        private const string AddCultureToFilterQueriesMethod = "AddCultureToFilterQueries";

        private delegate QueryOptions BuildQueryOptionsDelegate(SolrCompositeQuery compositeQuery);
        private delegate List<CultureExecutionContext> DeterminateCultureContextsDelegate(SolrCompositeQuery compositeQuery);
        private delegate void AddCultureToFilterQueriesDelegate(List<CultureExecutionContext> cultureContexts, QueryOptions queryOptions);

        private readonly BuildQueryOptionsDelegate _buildQueryOptionsDelegate;
        private readonly DeterminateCultureContextsDelegate _determinateCultureContexts;
        private readonly AddCultureToFilterQueriesDelegate _addCultureToFilterQueries;
        private readonly Type _solrSearchResultsGenericType;
        private readonly MethodInfo _applyScalarMethodsGenericTypeMethodInfo;
        private readonly IContentSearchConfigurationSettings _contentSearchSettings;
        private readonly SolrSearchContextDoNotSwallowError _context;
        private readonly ITransientHandler _transientHandler;
        private readonly SolrLoggingSerializer _solrLoggingSerializer;

        public LinqToSolrIndexDoNotSwallowError(SolrSearchContextDoNotSwallowError context, IExecutionContext[] executionContexts)
            : base(context,
                executionContexts,
                (IQueryOptimizer)new SolrQueryOptimizer(),
                (QueryMapper<SolrCompositeQuery>)new SolrQueryMapper(
                    new SolrIndexParameters((IIndexValueFormatter)context.Index.Configuration.IndexFieldStorageValueFormatter,
                    (IFieldQueryTranslatorMap<IFieldQueryTranslator>)context.Index.Configuration.VirtualFields,
                    (FieldNameTranslator)context.Index.FieldNameTranslator, executionContexts,
                    (IFieldMapReaders)context.Index.Configuration.FieldMap, context.ConvertQueryDatesToUtc)
                ),
                (IIndexValueFormatter)context.Index.Configuration.IndexFieldStorageValueFormatter,
                (IQueryableFactory)new DefaultQueryableFactory(),
                (IExpressionParser)new ExpressionParser(typeof(TItem), typeof(TItem),
                (FieldNameTranslator)context.Index.FieldNameTranslator))
        {
            _context = context;
            _contentSearchSettings = context.Index.Locator.GetInstance<IContentSearchConfigurationSettings>();

            var linqToSolrIndexType = typeof(Sitecore.ContentSearch.SolrProvider.LinqToSolrIndex<TItem>);

            _solrSearchResultsGenericType = linqToSolrIndexType.Assembly.GetType(SolrSearchResultsType);
            _applyScalarMethodsGenericTypeMethodInfo = linqToSolrIndexType.GetMethod(ApplyScalarMethodsMethod, BindingFlags.Instance | BindingFlags.NonPublic);

            var buildQueryOptionsMethod = linqToSolrIndexType.GetMethod(BuildQueryOptionsMethod,
              BindingFlags.Instance | BindingFlags.NonPublic,
              null,
              new[]
              {
          typeof(SolrCompositeQuery)
              },
              null);

            var determinateCultureContextsMethod = linqToSolrIndexType.GetMethod(DeterminateCultureContextsMethod,
              BindingFlags.Instance | BindingFlags.NonPublic,
              null,
              new[]
              {
          typeof(SolrCompositeQuery)
              },
              null);

            var addCultureToFilterQueriesMethod = linqToSolrIndexType.GetMethod(AddCultureToFilterQueriesMethod,
              BindingFlags.Instance | BindingFlags.NonPublic,
              null,
              new[]
              {
          typeof(List<CultureExecutionContext>),
          typeof(QueryOptions)
              },
              null);

            _buildQueryOptionsDelegate = (BuildQueryOptionsDelegate)Delegate.CreateDelegate(typeof(BuildQueryOptionsDelegate), this, buildQueryOptionsMethod);
            _determinateCultureContexts = (DeterminateCultureContextsDelegate)Delegate.CreateDelegate(typeof(DeterminateCultureContextsDelegate), this, determinateCultureContextsMethod);
            _addCultureToFilterQueries = (AddCultureToFilterQueriesDelegate)Delegate.CreateDelegate(typeof(AddCultureToFilterQueriesDelegate), this, addCultureToFilterQueriesMethod);
            _solrLoggingSerializer = new SolrLoggingSerializer();

            var solrCircuitBreakerPolicyProvider = DependencyResolver.Current.GetService<ISolrCircuitBreakerPolicyProvider>();
            _transientHandler = solrCircuitBreakerPolicyProvider.GetCircuitBreakerPolicy(context.Index.Name);
        }

        internal SolrQueryResults<Dictionary<string, object>> Execute(SolrCompositeQuery compositeQuery, Type resultType)
        {
            return _transientHandler.Execute(() => ExecuteInternal(compositeQuery, resultType));
        }

        internal SolrQueryResults<Dictionary<string, object>> ExecuteInternal(SolrCompositeQuery compositeQuery, Type resultType)
        {
            if (!compositeQuery.Methods.Any<QueryMethod>() || compositeQuery.Methods.First<QueryMethod>().MethodType != QueryMethodType.Union)
            {
                return this.GetResult(compositeQuery, _buildQueryOptionsDelegate(compositeQuery));
            }

            UnionMethod unionMethod = compositeQuery.Methods.First<QueryMethod>() as UnionMethod;
            dynamic innerEnumerable = unionMethod.InnerEnumerable;
            dynamic obj = innerEnumerable.Execute<SolrQueryResults<Dictionary<string, object>>>(innerEnumerable.Expression);
            dynamic outerEnumerable = unionMethod.OuterEnumerable;
            dynamic obj1 = outerEnumerable.Execute<SolrQueryResults<Dictionary<string, object>>>(outerEnumerable.Expression);
            if (obj == (dynamic)null)
            {
                dynamic solrQueryResult = obj1;
                if (solrQueryResult == null)
                {
                    solrQueryResult = new SolrQueryResults<Dictionary<string, object>>();
                }
                return (SolrQueryResults<Dictionary<string, object>>)solrQueryResult;
            }
            if (obj1 == (dynamic)null)
            {
                return (SolrQueryResults<Dictionary<string, object>>)obj;
            }
            obj1.AddRange(obj);
            return (SolrQueryResults<Dictionary<string, object>>)obj1;
        }

        private SolrQueryResults<Dictionary<string, object>> GetResult(SolrCompositeQuery compositeQuery, QueryOptions queryOptions)
        {
            _addCultureToFilterQueries(_determinateCultureContexts(compositeQuery), queryOptions);

            string q = _solrLoggingSerializer.SerializeQuery(compositeQuery.Query);
            //Cast index to ISolrOperations, to get use of SolrOperations internal method
            ISolrOperations solrSearchIndex = _context.Index as ISolrOperations;

            try
            {
                if (!queryOptions.Rows.HasValue)
                {
                    queryOptions.Rows = _contentSearchSettings.SearchMaxResults();
                }

                SearchLog.Log.Info("Solr Query - ?q=" + q + "&" + string.Join("&", _solrLoggingSerializer.GetAllParameters(queryOptions).Select<KeyValuePair<string, string>, string>((Func<KeyValuePair<string, string>, string>)(p => string.Format("{0}={1}", (object)p.Key, (object)p.Value))).ToArray<string>()), (Exception)null);

                //Access to SolrOperations via interface, to internal property
                return solrSearchIndex.SolrOperations.Query(q, queryOptions);
            }
            catch (Exception ex)
            {
                if (!(ex is SolrConnectionException) && !(ex is SolrNetException))
                {
                    //Not swallowing errors
                    throw;
                }
                else
                {
                    string message1 = ex.Message;
                    if (ex.Message.StartsWith("<?xml"))
                    {
                        XmlDocument xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml(ex.Message);
                        XmlNode xmlNode1 = xmlDocument.SelectSingleNode("/response/lst[@name='error'][1]/str[@name='msg'][1]");
                        XmlNode xmlNode2 = xmlDocument.SelectSingleNode("/response/lst[@name='responseHeader'][1]/lst[@name='params'][1]/str[@name='q'][1]");
                        if (xmlNode1 != null && xmlNode2 != null)
                        {
                            SearchLog.Log.Error(string.Format("Solr Error : [\"{0}\"] - Query attempted: [{1}]", xmlNode1.InnerText, xmlNode2.InnerText), null);
                            //Not swallowing errors
                            throw;
                        }
                    }

                    Log.Error(message1, ex, this);
                    //Not swallowing errors
                    throw;
                }
            }
        }

        public override TResult Execute<TResult>(SolrCompositeQuery compositeQuery)
        {
            if (EnumerableLinq.ShouldExecuteEnumerableLinqQuery((IQuery)compositeQuery))
                return EnumerableLinq.ExecuteEnumerableLinqQuery<TResult>(compositeQuery);

            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(SearchResults<>))
            {
                Type genericArgument = typeof(TResult).GetGenericArguments()[0];
                SolrQueryResults<Dictionary<string, object>> solrQueryResults = Execute(compositeQuery, genericArgument);
                Type type = _solrSearchResultsGenericType.MakeGenericType(genericArgument);
                MethodInfo applyScalarMethods = _applyScalarMethodsGenericTypeMethodInfo.MakeGenericMethod(typeof(TResult), genericArgument);

                SelectMethod selectMethod = GetSelectMethod(compositeQuery);
                object solrSearchResults = ReflectionUtility.CreateInstance(type,
                    _context,
                    solrQueryResults,
                    selectMethod,
                    compositeQuery.ExecutionContexts,
                    compositeQuery.VirtualFieldProcessors);

                return (TResult)applyScalarMethods.Invoke(this, new object[3]
                {
                    compositeQuery,
                    solrSearchResults,
                    solrQueryResults
                });
            }
            else
            {
                SolrQueryResults<Dictionary<string, object>> solrQueryResults = Execute(compositeQuery, typeof(TResult));
                Type type = _solrSearchResultsGenericType.MakeGenericType(typeof(TResult));
                MethodInfo applyScalarMethods = _applyScalarMethodsGenericTypeMethodInfo.MakeGenericMethod(typeof(TResult), typeof(TResult));

                SelectMethod selectMethod = GetSelectMethod(compositeQuery);
                object solrSearchResults = ReflectionUtility.CreateInstance(type,
                    _context,
                    solrQueryResults,
                    selectMethod,
                    compositeQuery.ExecutionContexts,
                    compositeQuery.VirtualFieldProcessors);

                return (TResult)applyScalarMethods.Invoke(this, new object[3]
                {
                    compositeQuery,
                    solrSearchResults,
                    solrQueryResults
                });
            }
        }

        public override IEnumerable<TElement> FindElements<TElement>(SolrCompositeQuery compositeQuery)
        {
            if (EnumerableLinq.ShouldExecuteEnumerableLinqQuery((IQuery)compositeQuery))
            {
                return EnumerableLinq.ExecuteEnumerableLinqQuery<IEnumerable<TElement>>(compositeQuery);
            }

            SolrQueryResults<Dictionary<string, object>> solrQueryResults = Execute(compositeQuery, typeof(TElement));
            List<SelectMethod> list = compositeQuery.Methods.Where(m => m.MethodType == QueryMethodType.Select).Select(m => (SelectMethod)m).ToList();
            SelectMethod selectMethod = list.Count == 1 ? list[0] : null;

            Type type = _solrSearchResultsGenericType.MakeGenericType(typeof(TElement));

            var solrSearchResults = ReflectionUtility.CreateInstance(type,
                    _context,
                    solrQueryResults,
                    selectMethod,
                    compositeQuery.ExecutionContexts,
                    compositeQuery.VirtualFieldProcessors);

            MethodInfo getSearchResults = type.GetMethod(GetSearchResultsMethod);
            return (IEnumerable<TElement>)getSearchResults.Invoke(solrSearchResults, new object[] { });
        }

        private static SelectMethod GetSelectMethod(SolrCompositeQuery compositeQuery)
        {
            var selectMethods = compositeQuery.Methods.Where(m => m.MethodType == QueryMethodType.Select).Select(m => (SelectMethod)m).ToList();
            return selectMethods.Count == 1 ? selectMethods[0] : null;
        }
    }
}