// ---
// Usage example:
//
// #l "local:?path=DotNetCoreHelper.cake"
//
// var coreHelper = DotNetCoreHelper;
#addin "Cake.Incubator"

#l "local:?path=CommandHelper.cake"
#l "local:?path=BuildHelper.cake"
#l "local:?path=NugetHelper.cake"

using System.Text.RegularExpressions;

public class ProjectConfiguration
{
  private string _ProjectAlias = string.Empty;
  public string ProjectAlias 
  { 
    get { return this._ProjectAlias; } 
    set
    {
      var newVal = (value ?? string.Empty)
        .Replace(".", string.Empty)
        .Replace(" ", string.Empty);

      this._ProjectAlias = newVal;
    } 
  }

  public FilePath ProjectFile { get; set; }
  public FilePath RelativeProjectFile { get; private set; }
  public string Configuration { get; set; }
  public string Platform { get; set; }
  public string Framework { get; set; }
  public IEnumerable<SolutionProject> Projects { get; private set; }
  public IEnumerable<string> ProjectOutputDirectories { get; private set; }
  public DirectoryPath PostBuildOutputDirectory { get; set; }
  
  public IEnumerable<string> ProjectNameExcludeFilters { get; set; }


  public IEnumerable<FilePath> ProjectOutputFiles
  {
    get
    {
      //Filter out project
      var filePatterns = this.Projects
        .Where(t => 
        {
          if(!this.ProjectNameExcludeFilters.Any())
            return true;
            
          return !this.ProjectNameExcludeFilters.Any(x => Regex.IsMatch(t.Name, x));
        })
        .Select(t => new { Project = t, OutputDirectory = this.GetOutputDirectoryForProject(t) })
        .Select(t => 
        {
          return string.Format("{0}/{1}.dll", t.OutputDirectory, t.Project.Name);
        })
        .ToArray();

      return filePatterns
        .SelectMany(t => 
        {
          this.Context.Information("Glob Pattern: {0}", t);
          var matchingFiles = this.Context.GetFiles(t)
            .Union(this.Context.GetMatchingFiles(new FilePath(t)))
            .ToList();

          this.Context.Information("Matching Glob Pattern: {0}", matchingFiles.Count);
          return matchingFiles;
        })
        .Where(t => this.Context.FileExists(t));
    }
  }

  private ICakeContext Context;

  public ProjectConfiguration(string projectAlias)
  { 
    this.Projects = Enumerable.Empty<SolutionProject>();
    this.ProjectOutputDirectories = Enumerable.Empty<string>();
    this.ProjectNameExcludeFilters = new string[] { ".*\\.Test\\..*", ".*\\.Tests\\..*" };

    if(string.IsNullOrWhiteSpace(projectAlias))
      throw new ArgumentNullException("projectAlias");

    this.ProjectAlias = projectAlias;
  }

  public void SetContext(ICakeContext context)
  {
    this.Context = context;

    this.SetRelativeProjectFilePath(context);
    this.SetProjects(context);
    this.SetProjectOutputDirectories(context);
  }

  private void SetRelativeProjectFilePath(ICakeContext context)
  {
    if(context == null)
      throw new ArgumentNullException("context");

    var cwd = context.Directory("./");

    if(this.ProjectFile == null)
      throw new ArgumentNullException("ProjectFile");

    this.RelativeProjectFile = context.MakeAbsolute(cwd).GetRelativePath(context.MakeAbsolute(this.ProjectFile));
  }

  private void SetProjects(ICakeContext context)
  {
    var projects = new List<SolutionProject>();

    if((this.ProjectFile.GetExtension() ?? string.Empty).Equals(".sln", StringComparison.InvariantCultureIgnoreCase))
    {
      var slnParse = context.ParseSolution(this.ProjectFile);
      if(slnParse != null)
      {
        projects.AddRange(slnParse.Projects);
      }
    }

    this.Projects = projects;
  }
  
  private void SetProjectOutputDirectories(ICakeContext context)
  {
    var relativeBasePath = this.RelativeProjectFile.GetDirectory();

    var outputPaths = new List<string>();

    var projects = this.Projects;
    foreach(var project in projects)
    {
      context.Debug("Getting output paths for Project: {0}", project.Name);

      var paths = new List<string>();
      var baseOutputPath = this.GetOutputDirectoryForProject(project);
      outputPaths.Add(baseOutputPath);
    }

    this.ProjectOutputDirectories = outputPaths;
  }

  private string GetOutputDirectoryForProject(SolutionProject project)
  {
    var relativeBasePath = this.RelativeProjectFile.GetDirectory();

    var baseOutputPath = string.Format("{0}/**/{1}/bin", relativeBasePath.FullPath, project.Name);
      
    baseOutputPath = string.Format(
      "{0}{1}", 
      baseOutputPath, 
      string.IsNullOrEmpty(this.Configuration) ? "/**" : string.Format("/*({0})", this.Configuration));

    baseOutputPath = string.Format(
      "{0}{1}", 
      baseOutputPath, 
      string.IsNullOrEmpty(this.Framework) ? string.Empty : string.Format("/*({0})", this.Framework));

    return baseOutputPath;
  }
}

public class DotNetCoreHelperModel
{
  public static DotNetCoreHelperModel CreateCoreHelper(
    ICakeContext context,
    BuildHelperModel buildHelper,
    CommandHelperModel commandHelper,
    NugetHelperModel nugetHelper)
  {
    return new DotNetCoreHelperModel(context, buildHelper, commandHelper, nugetHelper);
  }

  private List<ProjectConfiguration> _Projects = new List<ProjectConfiguration>();
  public List<ProjectConfiguration> Projects 
  { 
    get { return this._Projects; } 
  }

  private ICakeContext Context { get; set; }
  private BuildHelperModel BuildHelper { get; set; }
  private CommandHelperModel CommandHelper { get; set; }
  private NugetHelperModel NugetHelper { get; set; }

  private DotNetCoreHelperModel(
    ICakeContext context,
    BuildHelperModel buildHelper,
    CommandHelperModel commandHelper,
    NugetHelperModel nugetHelper)
  {
    if(context == null)
      throw new ArgumentNullException("context", "Context cannot be null");

    if(buildHelper == null)
      throw new ArgumentNullException("buildHelper", "buildHelper cannot be null. missing BuildHelper.cake reference");

    if(commandHelper == null)
      throw new ArgumentNullException("commandHelper", "commandHelper cannot be null. missing CommandHelper.cake reference");

    if(nugetHelper == null)
      throw new ArgumentNullException("nugetHelper", "nugetHelper cannot be null. missing NugetHelper.cake reference");

    this.Context = context;
    this.BuildHelper = buildHelper;
    this.CommandHelper= commandHelper;
    this.NugetHelper = nugetHelper;
  }

  public ProjectConfiguration AddProjectConfig(
    string projectName,
    FilePath projectOrSlnFile, 
    string configuration,
    string platform = "",
    string framework = "")
  {
    if(projectOrSlnFile == null)
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

    return newConfig;
  }

  private void AddProjectConfig(ProjectConfiguration config)
  {
    if(config == null)
      throw new ArgumentNullException("config");

    config.SetContext(this.Context);

    this.Projects.Add(config);

    this.RegisterCleanTasks(config);
    this.RegisterPreBuildTasks(config);
    this.RegisterBuildTasks(config);
    this.RegisterPostBuildTasks(config);
  }

  private void RegisterCleanTasks(ProjectConfiguration config)
  {
    var relativeProjectPath = config.RelativeProjectFile;

    this.BuildHelper.AddToClean(config.ProjectAlias)
      .Does(() =>
      {
        var clnSettings = new DotNetCoreCleanSettings();
        clnSettings.Configuration = string.IsNullOrWhiteSpace(config.Configuration) ? null : config.Configuration;
        clnSettings.Framework = string.IsNullOrWhiteSpace(config.Framework) ? null : config.Framework;

        this.Context.DotNetCoreClean(relativeProjectPath.FullPath, clnSettings);
        var paths = config.ProjectOutputDirectories;
        
        foreach(var path in paths)
        {
          this.Context.Debug("Cleaning Directory: {0}", path);
          this.Context.CleanDirectories(path);
        }
      });
  }

  private void RegisterPreBuildTasks(ProjectConfiguration config)
  {
    var relativeProjectPath = config.RelativeProjectFile;

    this.BuildHelper.AddToPreBuild(config.ProjectAlias)
      .Does(() =>
      {
        var restoreSettings = new DotNetCoreRestoreSettings
        {
          Sources = this.NugetHelper.SourceFeeds.ToList()
        };        

        this.Context.DotNetCoreRestore(relativeProjectPath.FullPath, restoreSettings);
      });
  }

  private void RegisterBuildTasks(ProjectConfiguration config)
  {
    var relativeProjectPath = config.RelativeProjectFile;

    this.BuildHelper.AddToBuild(config.ProjectAlias)
      .Does(() =>
      {
        var buildSettings = new DotNetCoreBuildSettings();
        buildSettings.Configuration = config.Configuration;  
        if(!string.IsNullOrWhiteSpace(config.Framework))  
          buildSettings.Framework = config.Framework;

        this.Context.Information("Building {0}...", relativeProjectPath.FullPath);
        this.Context.DotNetCoreBuild(relativeProjectPath.FullPath, buildSettings);
      });
  }

  private void RegisterPostBuildTasks(ProjectConfiguration config)
  {
    var relativeProjectPath = config.RelativeProjectFile;

    this.BuildHelper.AddToPostBuild(config.ProjectAlias)
      .WithCriteria(() => 
        {
          var hasPostBuildOutputDir = config.PostBuildOutputDirectory != null;
          if(!hasPostBuildOutputDir)
          {
            this.Context.Debug("ProjectConfiguration {0} does not have post build directory set. Skipping Task...", config.ProjectAlias);
            return false;
          }

          var postBuildDirExists = this.Context.DirectoryExists(config.PostBuildOutputDirectory);
          if(!postBuildDirExists)
          {
            this.Context.Debug(
              "Post build directory for Project: {0} does not exist. Directory: {1}. Skipping Task...", config.ProjectAlias, config.PostBuildOutputDirectory.FullPath);
            return false;
          }

          return true;
        })
      .Does(() =>
      {
        foreach(var outputFile in config.ProjectOutputFiles)
        {
          this.Context.Information("Output File: {0}", outputFile);
        }
      });
  }
}

var DotNetCoreHelper = DotNetCoreHelperModel.CreateCoreHelper(Context, BuildHelper, CommandHelper, NugetHelper);