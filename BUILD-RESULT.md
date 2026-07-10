# Multi-addon build result

Status: **SUCCESS**

Commit tested: `6715cee48037bfbb1440ecb93ad64ae5612310ce`

```text
  Determining projects to restore...
  Restored /home/runner/work/Gelato/Gelato/Gelato.csproj (in 2.28 sec).
/home/runner/work/Gelato/Gelato/Common.cs(472,26): warning CS8601: Possible null reference assignment. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Common.cs(361,17): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Controllers/PalcoCacheController.cs(118,33): warning CS8604: Possible null reference argument for parameter 'address' in 'MailAddress.MailAddress(string address, string? displayName)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Services/CatalogImportService.cs(78,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Controllers/CatalogController.cs(20,21): warning CS9113: Parameter 'libraryManager' is unread. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Controllers/GelatoApiController.cs(43,26): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Filters/SearchActionFilter.cs(31,23): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Controllers/GelatoApiController.cs(151,28): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Filters/SearchActionFilter.cs(129,23): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Filters/SearchActionFilter.cs(140,23): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Filters/SearchActionFilter.cs(178,22): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/CollectionManagerDecorator.cs(14,25): warning CS9113: Parameter 'manager' is unread. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/PlaylistManagerDecorator.cs(15,25): warning CS9113: Parameter 'manager' is unread. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/GelatoManager.cs(230,20): warning CS8600: Converting null literal or possible null value to non-nullable type. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Filters/InsertActionFilter.cs(79,26): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/MediaSourceManagerDecorator.cs(86,13): warning CS8604: Possible null reference argument for parameter 'ctx' in 'bool ActionContextExtensions.TryGetUserId(HttpContext ctx, out Guid userId)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/MediaSourceManagerDecorator.cs(102,25): warning CS8604: Possible null reference argument for parameter 'ctx' in 'bool ActionContextExtensions.IsInsertableAction(HttpContext ctx)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/MediaSourceManagerDecorator.cs(214,34): warning CS8604: Possible null reference argument for parameter 'value' in 'void Dictionary<string, string>.Add(string key, string value)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/GelatoManager.cs(415,29): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/GelatoManager.cs(483,24): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/GelatoManager.cs(519,13): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/MediaSourceManagerDecorator.cs(392,13): warning CS8604: Possible null reference argument for parameter 'ctx' in 'string? ActionContextExtensions.GetActionName(HttpContext ctx)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Plugin.cs(97,56): warning CS8604: Possible null reference argument for parameter 'baseConfig' in 'PluginConfiguration UserConfig.ApplyOverrides(PluginConfiguration baseConfig)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Plugin.cs(100,58): warning CS8604: Possible null reference argument for parameter 'cfg' in 'GelatoStremioProvider GelatoStremioProviderFactory.Create(PluginConfiguration cfg)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
  Gelato -> /home/runner/work/Gelato/Gelato/bin/Release/net9.0/Gelato.dll

Build succeeded.

/home/runner/work/Gelato/Gelato/Common.cs(472,26): warning CS8601: Possible null reference assignment. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Common.cs(361,17): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Controllers/PalcoCacheController.cs(118,33): warning CS8604: Possible null reference argument for parameter 'address' in 'MailAddress.MailAddress(string address, string? displayName)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Services/CatalogImportService.cs(78,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Controllers/CatalogController.cs(20,21): warning CS9113: Parameter 'libraryManager' is unread. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Controllers/GelatoApiController.cs(43,26): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Filters/SearchActionFilter.cs(31,23): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Controllers/GelatoApiController.cs(151,28): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Filters/SearchActionFilter.cs(129,23): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Filters/SearchActionFilter.cs(140,23): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Filters/SearchActionFilter.cs(178,22): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/CollectionManagerDecorator.cs(14,25): warning CS9113: Parameter 'manager' is unread. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/PlaylistManagerDecorator.cs(15,25): warning CS9113: Parameter 'manager' is unread. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/GelatoManager.cs(230,20): warning CS8600: Converting null literal or possible null value to non-nullable type. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Filters/InsertActionFilter.cs(79,26): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/MediaSourceManagerDecorator.cs(86,13): warning CS8604: Possible null reference argument for parameter 'ctx' in 'bool ActionContextExtensions.TryGetUserId(HttpContext ctx, out Guid userId)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/MediaSourceManagerDecorator.cs(102,25): warning CS8604: Possible null reference argument for parameter 'ctx' in 'bool ActionContextExtensions.IsInsertableAction(HttpContext ctx)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/MediaSourceManagerDecorator.cs(214,34): warning CS8604: Possible null reference argument for parameter 'value' in 'void Dictionary<string, string>.Add(string key, string value)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/GelatoManager.cs(415,29): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/GelatoManager.cs(483,24): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/GelatoManager.cs(519,13): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Decorators/MediaSourceManagerDecorator.cs(392,13): warning CS8604: Possible null reference argument for parameter 'ctx' in 'string? ActionContextExtensions.GetActionName(HttpContext ctx)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Plugin.cs(97,56): warning CS8604: Possible null reference argument for parameter 'baseConfig' in 'PluginConfiguration UserConfig.ApplyOverrides(PluginConfiguration baseConfig)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
/home/runner/work/Gelato/Gelato/Plugin.cs(100,58): warning CS8604: Possible null reference argument for parameter 'cfg' in 'GelatoStremioProvider GelatoStremioProviderFactory.Create(PluginConfiguration cfg)'. [/home/runner/work/Gelato/Gelato/Gelato.csproj]
    24 Warning(s)
    0 Error(s)

Time Elapsed 00:00:05.23
```
