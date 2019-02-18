# sitecore-solr
Sitecore Solr Enhancements/Playground.

This is example code of how you could extend the Sitecore Solr provider.
It's not production code, and currently is untested at present.
Included are example of how to override behaviour for Sitecore 9, but previously achieved the same behaviour in Sitecore 8.1/8.2. (Would be more sitecore support fixes required, as SolrCloud wasn't offically supported until Sitecore 9.0 update 2)

This code is written by myself, and not associated with any previous or current employers.

By sharing this code I hope to show the struggles trying to add in common distributed application code patterns,
and hope that future versions of Sitecore add in extensibility points to make this easier to achieve.

Along with these Patterns, unshared are the Sitecore Support fixes.
These fix a number of issues, and you'll need to contact Sitecore support for.
Some patches may not be compatible/needed in your specific setup.
So always contact Sitecore support to see if a particular combination of support fixes work together or need to be reissued.
As well as of course thoroughly testing, before taking anything near production.

As I can't share Sitecore dll's here, your need to rebuild this application once you have the support dlls, rather than using the offical nuget feed.

No support or guarantees exist with this code.
Please use/extend at your own risk. 

## Sitecore Support Fixes for Solr ##

| KB        | Title           | Description | Notes  |
| ------------- |:-------------:| -----:| -----:|
| 96016      | IIS recycle - switches alias on indexing job complete, even though job cancelled/incomplete | Without this fix, can get incomplete/partially rebuilt index going live | Fixed in Sitecore 9 initial release [Sitecore Support Github 96016](https://github.com/SitecoreSupport/Sitecore.Support.96016) |
| 235313     | SolrContentSearch.SolrSettings.OptimizeOnRebuildEnabled is not used   | Without this fix, you'll be triggering an Optimise operation on Solr, which can cause a latency spike |  Bug fix was in 96016 but omitted when integrated in the product, if you don't need 96016 as on Sitecore 9, then use this one instead |
| 163850.171950 | Patch for IsSolrAliveAgent to update SolrStatus and process index reinitialization correctly  | Without this patch, indexing won't reinitialise correctly when Solr comes back online, if Solr was down when sitecore starts up  |   [Sitecore Support Github 163850.171950](https://github.com/SitecoreSupport/Sitecore.Support.163850.171950)  |
| 96740.127177.155383 | Incorrect data indexing if the ContentSearch.Indexing.DisableDatabaseCaches setting value is set to true | If you have this setting enabled, without this patch, you can get the wrong version of an item being indexed, and sometime duplicates copies of an item in the index | [Sitecore Support Github 96740.127177.155383](https://github.com/SitecoreSupport/Sitecore.Support.127177.155383) |
| 252532 | If 'IndexAllFields=false' the 'IncludedFields' are indexed as string values | Without this patch, your multivalues fields will be indexed as strings not collections and you won't be able to queries won't work as expected | New bug in Sitecore 9 [Sitecore Support Github 252532](https://github.com/SitecoreSupport/Sitecore.Support.252532) |
| 285907 | Aggregate bug fix of: 127550: | Without this, you may find your rebuild speeds are slower in sitecore 9 | New bug in Sitecore 9, contact sitecore support for Aggregate bug fix|
| 127550 | SolrFieldNameTranslator is not able to resolve field configuration if fieldName consists of more than one word. | See 285907 | See 285907|
| 204414 | Search Log is flooded with WARN messages when there are more than one typeMatch with the same "type" attribute | See 285907 | See 285907|
| 195567 | Solr Search Provider uses search index to determine field type during indexing | See 285907 | See 285907|
| 285903 | OnPublishEndAsynchronousSingleInstanceStrategy which overrides Run() method and initializes LimitedConcurrencyLevelTaskSchedulerForIndexing singleton with incorrect MaxThreadLimit value | See 285907 | See 285907, fix for just [285903 available on github](https://github.com/SitecoreSupport/Sitecore.Support.285903)|
