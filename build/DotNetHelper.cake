// ---
var restoreTargetName = "DotNet-Restore";
var buildTargetName = "DotNet-Build";

public static class DotNetHelperSettings
{
  public static RestoreTarget = "DotNet-Restore";

  public static List<FilePath> ProjectPaths = new List<FilePath>();
  public static List<string> NugetSources = new List<string>();  
  public static Action<DotNetCoreRestoreSettings> RestoreSettingsConfig;

  public static DotNetCoreRestoreSettings GetRestoreSettings()
  {
    DotNetCoreRestoreSettings restoreSettings = new DotNetCoreRestoreSettings();
    if(RestoreSettingsConfig != null)
      RestoreSettingsConfig(restoreSettings);

    foreach(var src in NugetSources)
    {
      restoreSettings.Sources = restoreSettings.Sources.Union(new string[] { src });
    }

    return restoreSettings;
  }
}

Task("DotNet-Restore")
	.Does(() =>
	{
    var restoreSettings = DotNetHelperSettings.GetRestoreSettings();

    foreach(var path in DotNetHelperSettings.ProjectPaths)
    {
      Information("Restoring: {0}", path.FullPath);

      DotNetCoreRestore(
        path,
        restoreSettings);  
    }
	});