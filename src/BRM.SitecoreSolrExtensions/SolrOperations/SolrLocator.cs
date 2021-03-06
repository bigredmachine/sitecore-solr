﻿using System;
using System.Collections.Generic;
using BRM.Indexing.Domain;
using BRM.Indexing.SitecoreSolrExtensions.CircuitBreaker;
using BRM.Indexing.SitecoreSolrExtensions.Configuration;
using Polly;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
using SolrNet.Exceptions;

namespace BRM.Indexing.SitecoreSolrExtensions.SolrOperations
{
    public class SolrLocator<T> : DefaultSolrLocator<T>
    {
        public SolrLocator() : base()
        {
            //Override SolrFactory
            SolrFactory = new SolrFactory<T>(this, SolrContentSearchManager.SolrSettings);

            //For each index create a circuit breaker, and add to service locator
            foreach(var index in SolrContentSearchManager.Indexes)
            {
                var transientHandler = CreateCircuitBreakerForCore(index.Name);
                this.AddTransientHandler(transientHandler, index.Name);
            }
        }

        public void AddTransientHandler(ITransientHandler handler, string name)
        {
            if (!KeyedServiceCollection.ContainsKey(name))
            {
                KeyedServiceCollection.Add(name, new Dictionary<string, object>());
            }

            var collection = KeyedServiceCollection[name];
            collection[typeof(ITransientHandler).Name] = handler;
        }

        private ITransientHandler CreateCircuitBreakerForCore(string core)
        {
            Action<Exception, TimeSpan> onBreak = (exception, timespan) =>
            {
                //Log here on circuit break
            };
            Action onReset = () =>
            {
                //Log here on resumption of service
            };

            var corePolicy = Policy
                .Handle<SolrConnectionException>()
                .CircuitBreaker(Settings.CircuitBreakerConsecutiveExceptionsBeforeBreaking,
                TimeSpan.FromMinutes(Settings.CircuitBreakerDurationInMins),
                onBreak,
                onReset);

            return new TransientHandler(corePolicy);
        }
    }
}