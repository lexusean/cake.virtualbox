// ---
// Usage example:
//
// #l "local:?path=NugetHelper.cake"
//
// var nugetHelper = NugetHelper;
// nugetHelper.AddFeeds(url1, url2);
// nugetHelper.SetDefaultFeeds(url1, url2);

public class NugetHelperModel
{
  private const string DefaultNugetFeed = "https://api.nuget.org/v3/index.json";

  public static NugetHelperModel CreateNugetHelper(
    ICakeContext context)
  {
    return new NugetHelperModel(context);
  }

  public IEnumerable<string> SourceFeeds 
  { 
    get
    {
      return this._CustomFeeds.Union(this._DefaultFeeds).Distinct();
    } 
  }

  private ICakeContext Context;
  private List<string> _DefaultFeeds = new List<string>();
  private List<string> _CustomFeeds = new List<string>();

  private NugetHelperModel(ICakeContext context)
  {
    if(context == null)
			throw new ArgumentNullException("context", "context cannot be null.");

    this.Context = context;

    this.SetDefaultFeeds(DefaultNugetFeed);
  }

  public void AddFeeds(params string[] feedUrls)
  {
    this._CustomFeeds.AddRange(feedUrls);
  }

  public void SetDefaultFeeds(params string[] defaultFeedUrls)
  {
    this._DefaultFeeds.AddRange(defaultFeedUrls);
  }
}

var NugetHelper = NugetHelperModel.CreateNugetHelper(Context);