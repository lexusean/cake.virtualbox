// ---
#l "local:?path=CommandHelper.cake"
#l "local:?path=BuildHelper.cake"

public class ProjectConfiguration
{
  public string ProjectAlias { get; set; }
  public FilePath ProjectFile { get; set; }
  public string Configuration { get; set; }
  public string Platform { get; set; }
  public string Framework { get; set; }

  public ProjectConfiguration(string projectAlias)
  { 
    if(string.IsNullOrWhiteSpace(projectAlias))
      throw new ArgumentNullException("projectAlias");
  }

  public IEnumerable<SolutionProject> GetProjects(
    ICakeContext context)
  {
    var projects = new List<ProjectParserResult>();

    if((this.ProjectFile.GetExtension() ?? string.Empty).Equals("sln", StringComparison.InvariantCultureIgnoreCase))
    {
      var slnParse = this.Context.ParseSolution(this.ProjectFile);
      if(slnParse == null)
        return projects;

      return slnParse.Projects;
    }

    return projects;
  }

  public IEnumerable<string> GetOutputPaths(
    ICakeContext context)
  {
    var projects = this.GetProjects(context);
    foreach(var project in projects)
    {
      var paths = new List<string>();
      var baseOutputPath = string.Format("./**/{0}/**bin", project.Name);
      
      if(string.IsNullOrEmpty(this.Configuration))
        baseOutputPath = string.Format("{0}/**/{1}", baseOutputPath, this.Configuration);

      if(string.IsNullOrEmpty(this.Framework))
        baseOutputPath = string.Format("{0}/**/{1}", baseOutputPath, this.Framework);

      yield return baseOutputPath;
    }
  }
}

public class DotNetCoreHelperModel
{
  public static DotNetCoreHelperModel CreateCoreHelper(
    ICakeContext context,
    BuildHelperModel buildHelper,
    CommandHelperModel commandHelper)
  {
    return new DotNetCoreHelperModel(context, buildHelper, commandHelper);
  }

  private List<ProjectConfiguration> _Projects = new List<ProjectConfiguration>();
  public List<ProjectConfiguration> Projects 
  { 
    get { return this._Projects; } 
  }

  private ICakeContext Context { get; set; }
  private BuildHelperModel BuildHelper { get; set; }
  private CommandHelperModel CommandHelper { get; set; }

  private DotNetCoreHelperModel(
    ICakeContext context,
    BuildHelperModel buildHelper,
    CommandHelperModel commandHelper)
  {
    if(context == null)
      throw new ArgumentNullException("context", "Context cannot be null");

    if(buildHelper == null)
      throw new ArgumentNullException("buildHelper", "buildHelper cannot be null. missing BuildHelper.cake reference");

    if(commandHelper == null)
      throw new ArgumentNullException("commandHelper", "commandHelper cannot be null. missing CommandHelper.cake reference");

    this.Context = context;
    this.BuildHelper = buildHelper;
    this.CommandHelper= commandHelper;
  }

  public void AddProjectConfig(
    string projectName,
    FilePath projectOrSlnFile, 
    string configuration,
    string platform = "",
    string framework = "")
  {
    if(projectOrSlnFile)
      throw new ArgumentNullException("projectOrSlnFile", "Requires Project File");

    if(!this.Context.FileExists(projectOrSlnFile))
      throw new ArgumentException("projectOrSlnFile", "Requires Project File To Exist");

    if(string.IsNullOrWhiteSpace(configuration))
      throw new ArgumentNullException("configuration", "Requires Project Configuration");

    var newConfig = new ProjectConfiguration(projectName)
    {
      ProjectFile = projectOrSlnFile,
      Configuration = configuration,
      Platform = platform,
      Framework = framework
    };

    this.AddProjectConfig(newConfig);
  }

  public CakeTaskBuilder<ActionTask> AddToClean(string taskName)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      return null;

    var allTask = this.BuildHelper.CleanTask();
    var newTask = this.BuildHelper.CleanTask(taskName);

    allTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToPreBuild(string taskName)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      return null;

    var allTask = this.BuildHelper.PreBuildTask();
    var newTask = this.BuildHelper.PreBuildTask(taskName);

    allTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToBuild(string taskName)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      return null;

    var allTask = this.BuildHelper.BuildTask();
    var newTask = this.BuildHelper.BuildTask(taskName);

    allTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToPostBuild(string taskName)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      return null;

    var allTask = this.BuildHelper.PostBuildTask();
    var newTask = this.BuildHelper.PostBuildTask(taskName);

    allTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToPackage(string taskName)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      return null;

    var allTask = this.BuildHelper.PackageBuildTask();
    var newTask = this.BuildHelper.PackageBuildTask(taskName);

    allTask
      .IsDependentOn(newTask);

    return newTask;
  }

  private void AddProjectConfig(ProjectConfiguration config)
  {
    if(config == null)
      throw new ArgumentNullException("config");

    this.Projects.Add(config);

    this.RegisterCleanTasks(config);

  }

  private void RegisterCleanTasks(ProjectConfiguration config)
  {
    this.AddToClean(config.ProjectAlias)
      .Does(() =>
      {
        var clnSettings = new DotNetCoreCleanSettings();
        clnSettings.Configuration = string.IsNullOrWhiteSpace(config.Configuration) ? null : config.Configuration;
        clnSettings.Framework = string.IsNullOrWhiteSpace(config.Framework) ? null : config.Framework;

        this.Context.DotNetCoreClean(config.ProjectFile, clnSettings);

        var paths = config.GetOutputPaths(this.Context).ToList();
        foreach(var path in paths)
        {
          this.Context.CleanDirectories(path);
        }
      });
    
  }
}

var DotNetCoreHelper = DotNetCoreHelperModel.CreateCoreHelper(Context, BuildHelper CommandHelper);