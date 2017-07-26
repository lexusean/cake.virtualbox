// ---
// Usage example:
//
// #l "local:?path=DotNetCoreHelper.cake"
//
// var coreHelper = DotNetCoreHelper;
#l "local:?path=CommandHelper.cake"
#l "local:?path=BuildHelper.cake"
#l "local:?path=TestHelper.cake"
#l "local:?path=NugetHelper.cake"

using System.Text.RegularExpressions;

public class DotNetCoreHelperModel
{
  public class ProjectConfiguration
  {
    private FilePath _ProjectFile = null;
    public FilePath ProjectFile 
    { 
      get { return this._ProjectFile; } 
      private set
      {
        this._ProjectFile = value;
        this.ResetCache();
      } 
    }

    private string _ProjectAlias = string.Empty;
    public string ProjectAlias 
    { 
      get 
      { 
        if(string.IsNullOrWhiteSpace(this._ProjectAlias) && this.Context != null)
        {
          this._ProjectAlias = this.ProjectFile.GetFilenameWithoutExtension().FullPath
            .Replace(".", string.Empty)
            .Replace(" ", string.Empty);
        }

        return this._ProjectAlias;
      } 
      set
      {
        this._ProjectAlias = (value ?? string.Empty)
          .Replace(".", string.Empty)
          .Replace(" ", string.Empty);
      } 
    }

    private FilePath _RelativeProjectFile = null;
    public FilePath RelativeProjectFile 
    { 
      get
      {
        if(this._RelativeProjectFile == null)
        {
          this._RelativeProjectFile = this.GetRelativeProjectFilePath();
        }

        return this._RelativeProjectFile;
      } 
    }

    public string Configuration { get; set; }
    public string Framework { get; set; }
    public string Platform { get; set; }

    private List<SolutionProject> _Projects = null;
    public IEnumerable<SolutionProject> Projects 
    { 
      get
      {
        if(this._Projects == null)
        {
          this._Projects = this.GetProjects();
        }

        return this._Projects ?? Enumerable.Empty<SolutionProject>();
      } 
    }

    private List<SolutionProject> _TestProjects = null;
    public IEnumerable<SolutionProject> TestProjects 
    { 
      get
      {
        if(this._TestProjects == null)
        {
          this._TestProjects = this.GetProjects();
        }

        return this._TestProjects ?? Enumerable.Empty<SolutionProject>();
      } 
    }

    private List<string> _OutputDirectories = null;
    public IEnumerable<string> OutputDirectories 
    { 
      get
      {
        if(this._OutputDirectories == null)
        {
          this._OutputDirectories = this.GetOutputDirectories();
        }

        return this._OutputDirectories ?? Enumerable.Empty<string>();
      }
    }
    
    private DirectoryPath _ArtifactDirectory = null;
    public DirectoryPath ArtifactDirectory 
    { 
      get
      {
        if(this._ArtifactDirectory == null)
          return null;

        var path = this._ArtifactDirectory.Combine(this.ProjectAlias);

        if(!string.IsNullOrWhiteSpace(this.Configuration))
          path = path.Combine(new DirectoryPath(this.Configuration));
        
        if(!string.IsNullOrWhiteSpace(this.Framework))
          path = path.Combine(new DirectoryPath(this.Framework));

        if(!string.IsNullOrWhiteSpace(this.Platform))
          path = path.Combine(new DirectoryPath(this.Platform));

        return path;
      }
      set { this._ArtifactDirectory = value; } 
    }
    
    public List<string> ArtifactExcludeFilters { get; set; }

    public IEnumerable<FilePath> Artifacts
    {
      get
      {
        //Filter out project
        var filePatterns = this.Projects
          .Where(t => 
          {
            if(!this.ArtifactExcludeFilters.Any())
              return true;
              
            return !this.ArtifactExcludeFilters.Any(x => Regex.IsMatch(t.Name, x));
          })
          .Select(t => new { Project = t, OutputDirectory = this.GetOutputDirectoryForProject(t) })
          .Select(t => 
          {
            return new 
            { 
              Project = t.Project, 
              SearchPattern = string.Format("{0}/{1}.*", t.OutputDirectory, t.Project.Name),
              IsMatch = new Func<FilePath, bool>(path => path.GetFilenameWithoutExtension().FullPath == t.Project.Name)
            };
          })
          .ToArray();

        return filePatterns
          .SelectMany(t => 
          {
            var matchingFiles = this.Context.GetFiles(t.SearchPattern)
              .Where(x => t.IsMatch(x));

            return matchingFiles;
          })
          .Where(t => this.Context.FileExists(t));
      }
    }

    private ICakeContext Context;

    public ProjectConfiguration(ICakeContext context, FilePath projectFile)
    { 
      if(context == null)
        throw new ArgumentNullException("context");

      if(projectFile == null)
        throw new ArgumentNullException("projectFile");

      this.ProjectFile = projectFile;
      this.Context = context;

      this.ArtifactExcludeFilters = new List<string>();
    }

    private FilePath GetRelativeProjectFilePath()
    {
      var context = this.Context;
      if(context == null)
        return null;

      var cwd = context.Directory("./");

      if(this.ProjectFile == null)
        return null;

      return context.MakeAbsolute(cwd).GetRelativePath(context.MakeAbsolute(this.ProjectFile));
    }

    private List<SolutionProject> GetProjects()
    {
      var context = this.Context;
      if(context == null)
        return null;

      var projects = new List<SolutionProject>();

      if((this.ProjectFile.GetExtension() ?? string.Empty).Equals(".sln", StringComparison.InvariantCultureIgnoreCase))
      {
        var slnParse = context.ParseSolution(this.ProjectFile);
        if(slnParse != null)
        {
          projects.AddRange(slnParse.Projects);
        }
      }

      return projects;
    }
    
    private List<string> GetOutputDirectories()
    {
      var context = this.Context;
      if(context == null)
        return null;

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

      return outputPaths;
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

    private void ResetCache()
    {
      this._ProjectAlias = null;
      this._RelativeProjectFile = null;
      this.Configuration = null;
      this.Platform = null;
      this.Framework = null;
      this._Projects = null;
      this._TestProjects = null;
      this._OutputDirectories = null;
      this._ArtifactDirectory = null;
    }
  }

  public class TestConfiguration
  {
    public List<string> ProjectNameFilters { get; set; }
    public List<string> TestCategories { get; set; }
    public bool IsXUnit { get; set; }
    public bool NoBuild { get; set; }

    public TestConfiguration()
    {
      this.ProjectNameFilters = new List<string>(new string[] { ".*\\.Test\\..*", ".*\\.Tests\\..*" });
      this.TestCategories = new List<string>();
      this.NoBuild = true;
    } 

    public IEnumerable<SolutionProject> GetTestProjects(ProjectConfiguration projConfig)
    {
      return projConfig.Projects
        .Where(t => 
        {
          if(!this.ProjectNameFilters.Any())
            return false;
            
          return this.ProjectNameFilters.Any(x => Regex.IsMatch(t.Name, x));
        });
    }
  }

  public static DotNetCoreHelperModel CreateCoreHelper(
    ICakeContext context,
    BuildHelperModel buildHelper,
    TestHelperModel testHelper,
    CommandHelperModel commandHelper,
    NugetHelperModel nugetHelper)
  {
    return new DotNetCoreHelperModel(context, buildHelper, testHelper, commandHelper, nugetHelper);
  }

  private List<ProjectConfiguration> _Projects = new List<ProjectConfiguration>();
  public List<ProjectConfiguration> Projects 
  { 
    get { return this._Projects; } 
  }

  private ICakeContext Context { get; set; }
  private BuildHelperModel BuildHelper { get; set; }
  private TestHelperModel TestHelper { get; set; }
  private CommandHelperModel CommandHelper { get; set; }
  private NugetHelperModel NugetHelper { get; set; }

  private DotNetCoreHelperModel(
    ICakeContext context,
    BuildHelperModel buildHelper,
    TestHelperModel testHelper,
    CommandHelperModel commandHelper,
    NugetHelperModel nugetHelper)
  {
    if(context == null)
      throw new ArgumentNullException("context", "Context cannot be null");

    if(buildHelper == null)
      throw new ArgumentNullException("buildHelper", "buildHelper cannot be null. missing BuildHelper.cake reference");

    if(testHelper == null)
      throw new ArgumentNullException("testHelper", "testHelper cannot be null. missing TestHelper.cake reference");

    if(commandHelper == null)
      throw new ArgumentNullException("commandHelper", "commandHelper cannot be null. missing CommandHelper.cake reference");

    if(nugetHelper == null)
      throw new ArgumentNullException("nugetHelper", "nugetHelper cannot be null. missing NugetHelper.cake reference");

    this.Context = context;
    this.BuildHelper = buildHelper;
    this.TestHelper = testHelper;
    this.CommandHelper= commandHelper;
    this.NugetHelper = nugetHelper;
  }

  public ProjectConfiguration GetProjectConfig(FilePath projectFile)
  {
     var projConfig = new ProjectConfiguration(this.Context, projectFile);

     return projConfig;
  }

  public void AddProjectConfig(ProjectConfiguration config)
  {
    if(config == null)
      throw new ArgumentNullException("config");

    this.Projects.Add(config);

    this.RegisterCleanTasks(config);
    this.RegisterPreBuildTasks(config);
    this.RegisterBuildTasks(config);
    this.RegisterPostBuildTasks(config);
  }

  public TestConfiguration GetTestConfig(
    params string[] testCategories)
  {
    return new TestConfiguration()
    {
      TestCategories = new List<string>(testCategories)
    };
  }

  public void AddTestConfig(ProjectConfiguration projectConfig, TestConfiguration testConfig)
  {
    if(projectConfig == null)
      throw new ArgumentNullException("projectConfig");
    
    if(testConfig == null)
      throw new ArgumentNullException("testConfig");

    projectConfig.ArtifactExcludeFilters = testConfig.ProjectNameFilters;

    this.RegisterTestTasks(projectConfig, testConfig);
  }

  private void RegisterCleanTasks(ProjectConfiguration config)
  {
    this.BuildHelper.AddToClean(config.ProjectAlias);
    
    this.BuildHelper.AddToClean("DotNetCoreClean", true, config.ProjectAlias)
      .Does(() =>
      {
        var clnSettings = new DotNetCoreCleanSettings();
        clnSettings.Configuration = string.IsNullOrWhiteSpace(config.Configuration) ? null : config.Configuration;
        clnSettings.Framework = string.IsNullOrWhiteSpace(config.Framework) ? null : config.Framework;

        this.Context.DotNetCoreClean(config.RelativeProjectFile.FullPath, clnSettings);
      });

    this.BuildHelper.AddToClean("ProjectOutputs", true, config.ProjectAlias)
      .Does(() =>
      {
        var paths = config.OutputDirectories;
        
        foreach(var path in paths)
        {
          this.Context.Debug("Cleaning Directory: {0}", path);
          this.Context.CleanDirectories(path);
        }
      });

    this.BuildHelper.AddToClean("Artifacts", true, config.ProjectAlias)
      .Does(() =>
      {
        var postBuildDir = config.ArtifactDirectory;
        if(postBuildDir != null && this.Context.DirectoryExists(postBuildDir))
        {
          this.Context.Debug("Deleting PostBuild Artifact Directory: {0}", postBuildDir.FullPath);
          this.Context.DeleteDirectory(postBuildDir, true);
        }
      });
  }

  private void RegisterPreBuildTasks(ProjectConfiguration config)
  {
    this.BuildHelper.AddToPreBuild(config.ProjectAlias);

    this.BuildHelper.AddToPreBuild("DotNetCoreRestore", true, config.ProjectAlias)
      .Does(() =>
      {
        var restoreSettings = new DotNetCoreRestoreSettings
        {
          Sources = this.NugetHelper.SourceFeeds.ToList()
        };        

        this.Context.DotNetCoreRestore(config.RelativeProjectFile.FullPath, restoreSettings);
      });
  }

  private void RegisterBuildTasks(ProjectConfiguration config)
  {
    this.BuildHelper.AddToBuild(config.ProjectAlias);
    
    this.BuildHelper.AddToBuild("DotNetCoreBuild", true, config.ProjectAlias)
      .Does(() =>
      {
        var buildSettings = new DotNetCoreBuildSettings();
        buildSettings.Configuration = config.Configuration;  
        if(!string.IsNullOrWhiteSpace(config.Framework))  
          buildSettings.Framework = config.Framework;

        this.Context.Information("Building {0}...", config.RelativeProjectFile.FullPath);
        this.Context.DotNetCoreBuild(config.RelativeProjectFile.FullPath, buildSettings);
      });
  }

  private void RegisterPostBuildTasks(ProjectConfiguration config)
  {
    this.BuildHelper.AddToPostBuild(config.ProjectAlias);

    this.BuildHelper.AddToPostBuild("MoveToArtifacts", true, config.ProjectAlias)
      .Does(() =>
      {
        if(config.ArtifactDirectory == null)
          throw new ArgumentNullException("ArtifactDirectory", "ArtifactDirectory was not set on ProjectConfig");

        var artifacts = config.Artifacts;
        if(!(artifacts ?? Enumerable.Empty<FilePath>()).Any())
          throw new Exception(string.Format("No Build Artifacts Found. Run \"build.(ps1/sh) -r=Build-{0}\"", config.ProjectAlias));

        var outputDirectory = config.ArtifactDirectory;
        this.Context.EnsureDirectoryExists(outputDirectory);

        foreach(var artifact in artifacts)
        {
          this.Context.CopyFileToDirectory(artifact, outputDirectory);
        }
      });
  }

  private void RegisterTestTasks(ProjectConfiguration config, TestConfiguration testConfig)
  {
    foreach(var testCategory in testConfig.TestCategories)
    {
      this.TestHelper.AddToTest(config.ProjectAlias, testCategory);

      this.TestHelper.AddToTest("RunTestCategory", testCategory, true, config.ProjectAlias)
        .Does(() =>
        {
          var settings = new DotNetCoreTestSettings();
          settings.Filter = string.Format("{0}={1}", testConfig.IsXUnit ? "Category" : "TestCategory", testCategory);
          settings.Configuration = config.Configuration;
          settings.Framework = config.Framework;
          settings.NoBuild = testConfig.NoBuild;

          var targetDir = this.Context.MakeAbsolute(config.ArtifactDirectory);
          this.Context.Information("ProjectFileDir 2: {0}", targetDir.FullPath);
          targetDir = targetDir.Combine("TestResults");
          this.Context.Information("ProjectFileDir 3: {0}", targetDir.FullPath);

          foreach(var testProj in testConfig.GetTestProjects(config))
          {
            var testResultsTarget = targetDir.CombineWithFilePath(
              string.Format("{0}-{1}-{2}-{3}.trx", testProj.Name, testCategory, config.Configuration, config.Framework));

            settings.Logger = string.Format("trx;LogFileName={0}", testResultsTarget.FullPath);

            this.Context.DotNetCoreTest(this.Context.MakeAbsolute(testProj.Path).FullPath, settings);  
          }
        });
    }
  }
}

var DotNetCoreHelper = DotNetCoreHelperModel.CreateCoreHelper(
  Context, 
  BuildHelper,
  TestHelper, 
  CommandHelper, 
  NugetHelper);