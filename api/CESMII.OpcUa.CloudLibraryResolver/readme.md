# Usage

## Basic

Pass resolver to UANodeSetCacheManager constructor:
```c#
    var cacheManager = new UANodeSetCacheManager(myNodeSetCache, _cloudLibResolver);
```
## With progress
```c#
    try
    {
        _cloudLibResolver.OnDownloadNodeSet += callback;
        var cacheManager = new UANodeSetCacheManager(myNodeSetCache, _cloudLibResolver);
        resultSet = cacheManager.ImportNodeSets(nodeSetXmlStringList, false, userToken);
    }
    finally
    {
        _cloudLibResolver.OnDownloadNodeSet -= callback;
    }
```

## With nodesets pending approval
```c#
    try
    {
        _cloudLibResolver.OnDownloadNodeSet += callback;
        _cloudLibResolver.FilterPendingNodeSet = (n => true);
        var cacheManager = new UANodeSetCacheManager(myNodeSetCache, _cloudLibResolver);
        resultSet = cacheManager.ImportNodeSets(nodeSetXmlStringList, false, userToken);
    }
    finally
    {
        _cloudLibResolver.OnDownloadNodeSet -= callback;
    }
```


## Dependency Injection
Add service in startup.cs / program.cs:
```c#
    services.AddCloudLibraryResolver();
```

Provide configuration:
```json
  "CloudLibrary": {
    "UserName": "something",
    "Password": "secure",
    "EndPoint": "https://localhost:5007"
  }
```

Request as IUANodeSetResolverWithPending:
```c#
services.GetService<IUANodeSetResolverWithPending>();
```

