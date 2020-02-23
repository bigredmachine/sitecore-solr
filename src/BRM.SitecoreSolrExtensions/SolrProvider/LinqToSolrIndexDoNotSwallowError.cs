﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using BRM.Indexing.Domain;
using BRM.Indexing.SitecoreSolrExtensions.SolrOperations;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Factories;
using Sitecore.ContentSearch.Linq.Methods;
using Sitecore.ContentSearch.Linq.Parsing;
using Sitecore.ContentSearch.Linq.Solr;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.ContentSearch.SolrProvider.Logging;
using Sitecore.ContentSearch.Utilities;
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
        private const string LogQueryHelperType = "LogQueryHelper";
        private const string LogHelperPropertyName = "logHelper";
        private const string LogSolrQueryExceptionMethod = "LogSolrQueryException";
        private const string LogSolrQueryMethod = "LogSolrQuery";
        private const string HandleUnionMethod = "HandleUnion";

        private delegate void LogSolrQueryExceptionDelegate(Exception exception);
        private delegate void LogSolrQueryDelegate(string serializedQuery, QueryOptions queryOptions);
        private delegate SolrQueryResults<Dictionary<string, object>> HandleUnionDelegate(UnionMethod unionMethod);

        private readonly LogSolrQueryExceptionDelegate _logQueryExceptionDelegate;
        private readonly HandleUnionDelegate _handleUnionDelegate;
        private readonly LogSolrQueryDelegate _logSolrQueryDelegate;
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

            var handleUnionMethod = linqToSolrIndexType.GetMethod(HandleUnionMethod,
               BindingFlags.Static | BindingFlags.NonPublic,
               null,
               new[]
               {
                   typeof(UnionMethod)
               },
               null);

            _handleUnionDelegate = (HandleUnionDelegate)Delegate.CreateDelegate(typeof(HandleUnionDelegate), handleUnionMethod);

            _solrLoggingSerializer = new SolrLoggingSerializer();

            var solrCircuitBreakerPolicyProvider = DependencyResolver.Current.GetService<ISolrCircuitBreakerPolicyProvider>();
            _transientHandler = solrCircuitBreakerPolicyProvider.GetCircuitBreakerPolicy(context.Index.Name);

            var logHelperField = linqToSolrIndexType.GetField(LogHelperPropertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            var logHelper = logHelperField.GetValue(this);
            var logHelperType = linqToSolrIndexType.GetNestedType(LogQueryHelperType, BindingFlags.NonPublic | BindingFlags.Instance);
            Type logHelperGenericType = logHelperType.MakeGenericType(typeof(TItem));


            var logSolrQueryExceptionMethod = logHelperGenericType.GetMethod(LogSolrQueryExceptionMethod, 
                BindingFlags.Public | BindingFlags.Instance, 
                null,
                new[]
              {
          typeof(Exception)
              },
              null);

            var logSolrQueryMethod = logHelperGenericType.GetMethod(LogSolrQueryMethod,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[]
              {
          typeof(string),
          typeof(QueryOptions)
              },
              null);

            _logQueryExceptionDelegate = (LogSolrQueryExceptionDelegate)Delegate.CreateDelegate(typeof(LogSolrQueryExceptionDelegate), logHelper, logSolrQueryExceptionMethod);
            _logSolrQueryDelegate = (LogSolrQueryDelegate)Delegate.CreateDelegate(typeof(LogSolrQueryDelegate), logHelper, logSolrQueryMethod);
        }

        internal SolrQueryResults<Dictionary<string, object>> Execute(SolrCompositeQuery compositeQuery, Type resultType)
        {
            return _transientHandler.Execute(() => ExecuteInternal(compositeQuery, resultType));
        }

        public override TResult Execute<TResult>(SolrCompositeQuery compositeQuery)
        {
            if (EnumerableLinq.ShouldExecuteEnumerableLinqQuery((IQuery)compositeQuery))
                return EnumerableLinq.ExecuteEnumerableLinqQuery<TResult>((IQuery)compositeQuery);
            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(SearchResults<>))
            {
                Type genericArgument = typeof(TResult).GetGenericArguments()[0];
                SolrQueryResults<Dictionary<string, object>> solrQueryResults = this.Execute(compositeQuery, genericArgument);
                Type type = typeof(SolrSearchResults<>).MakeGenericType(genericArgument);
                MethodInfo methodInfo = this.GetType().GetMethod("ApplyScalarMethods", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(typeof(TResult), genericArgument);
                object instance = ReflectionUtility.CreateInstance(type, (object)_context, (object)solrQueryResults, (object)compositeQuery);
                return (TResult)methodInfo.Invoke((object)this, new object[3]
                {
          (object) compositeQuery,
          instance,
          (object) solrQueryResults
                });
            }
            if (typeof(TResult) == typeof(SolrQueryResults<Dictionary<string, object>>))
                return (TResult)System.Convert.ChangeType((object)this.Execute(compositeQuery, typeof(SearchResults<>)), typeof(TResult));
            SolrQueryResults<Dictionary<string, object>> solrQueryResults1 = this.Execute(compositeQuery, typeof(TResult));
            SolrSearchResults<TResult> processedResults = new SolrSearchResults<TResult>(_context, solrQueryResults1, compositeQuery);
            return this.ApplyScalarMethods<TResult, TResult>(compositeQuery, processedResults, solrQueryResults1);
        }

        public override IEnumerable<TElement> FindElements<TElement>(
      SolrCompositeQuery compositeQuery)
        {
            return EnumerableLinq.ShouldExecuteEnumerableLinqQuery((IQuery)compositeQuery) ? 
                EnumerableLinq.ExecuteEnumerableLinqQuery<IEnumerable<TElement>>((IQuery)compositeQuery) : 
                new SolrSearchResults<TElement>(_context, this.Execute(compositeQuery, typeof(TElement)), compositeQuery).GetSearchResults();
        }

        internal SolrQueryResults<Dictionary<string, object>> ExecuteInternal(
            SolrCompositeQuery compositeQuery,
            Type resultType)
        {
            QueryMethod queryMethod = compositeQuery.Methods.FirstOrDefault<QueryMethod>();
            return queryMethod != null && queryMethod is UnionMethod unionMethod ? 
                _handleUnionDelegate(unionMethod) : 
                this.ExecuteQuery(compositeQuery, this.BuildQueryOptions(compositeQuery));
        }

        private QueryOptions BuildQueryOptions(SolrCompositeQuery compositeQuery)
        {
            return this.SolrQueryOptionsBuilder.Build(compositeQuery, (IProviderSearchContext)_context);
        }

        private SolrQueryResults<Dictionary<string, object>> ExecuteQuery(
      SolrCompositeQuery compositeQuery,
      QueryOptions options)
        {
            //Cast index to ISolrOperations, to get use of SolrOperations internal method
            ISolrOperations solrSearchIndex = _context.Index as ISolrOperations;

            this.SetContextCultureFromExecutionContext(compositeQuery);
            string str = _solrLoggingSerializer.SerializeQuery((ISolrQuery)compositeQuery.Query);
            _logSolrQueryDelegate(str, options);
            try
            {
                return solrSearchIndex.SolrOperations.Query(str, options);
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case SolrConnectionException _:
                    case SolrNetException _:
                        _logQueryExceptionDelegate(ex);
                        //change to now swallow error
                        throw;
                    default:
                        throw;
                }
            }
        }

        private void SetContextCultureFromExecutionContext(SolrCompositeQuery compositeQuery)
        {
            this.UpdateFieldNameTranslatorCultureContext((IEnumerable<CultureExecutionContext>)compositeQuery.ExecutionContexts.OfType<CultureExecutionContext>().ToList<CultureExecutionContext>());
        }

        private void UpdateFieldNameTranslatorCultureContext(
          IEnumerable<CultureExecutionContext> cultureExecutionContext)
        {
            if (cultureExecutionContext == null)
                return;
            this.FieldNameTranslator.Accept((IExecutionContext)cultureExecutionContext.FirstOrDefault<CultureExecutionContext>((Func<CultureExecutionContext, bool>)(executionContext => executionContext.PredicateType != CulturePredicateType.Not)));
        }
    }
}