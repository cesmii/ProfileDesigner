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
        _cloudLibResolver.FilterPendingNodeSet = (n => true); // All pending nodesets
        var cacheManager = new UANodeSetCacheManager(myNodeSetCache, _cloudLibResolver);
        resultSet = cacheManager.ImportNodeSets(nodeSetXmlStringList, false, userToken);
    }
    finally
    {
        _cloudLibResolver.OnDownloadNodeSet -= callback;
    }
```

### Filter by user id
```c#
        _cloudLibResolver.FilterPendingNodeSet = (n =>
            {
                return n.Metadata.UserId == userId;
            }
        );
```

### Filter by user id for the CESMII cloud library when used via CESMII Profile Designer
```c#
        _cloudLibResolver.FilterPendingNodeSet = (n =>
            {
                return n.Metadata.UserId == userId 
                || (n.Metadata?.AdditionalProperties?
                     .Any(p => p.Name == ICloudLibDal<CloudLibProfileModel>.strCESMIIUserInfo 
                               && p.Value.StartsWith($"{userId,}")) ?? false);
            }
        );
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

