// ---
// Usage example:
//
// #l "local:?path=BuildHelper.cake"
//
// var buildHelper = BuildHelper;

#l "local:?path=TaskHelper.cake"

public class BuildHelperModel
{
  private const string TargetCategory = "Build";
  private const string TargetConstraintFormat = "{0}-{1}";
  private const string CleanTaskName = "Clean";
  private const string PreBuildTaskName = "PreBuild";
  private const string BuildTaskName = "Build";
  private const string TestTaskName = "Test";
  private const string PostBuildTaskName = "PostBuild";
  private const string PackageBuildTaskName = "Package";

  public static BuildHelperModel CreateBuildHelper(
    ICakeContext context,
    TaskHelperModel taskHelper)
  {
    return new BuildHelperModel(context, taskHelper);
  }

  private static string GetTaskName(string taskType, string name = "All")
  {
    if(string.IsNullOrWhiteSpace(taskType))
      return string.Empty;

    var taskTarget = name;
    if(string.IsNullOrWhiteSpace(taskTarget))
      taskTarget = "All";

    return string.Format(TargetConstraintFormat, taskType, taskTarget);
  }

  private static CakeTaskBuilder<ActionTask> GetTask(TaskHelperModel taskHelper, string taskType, string name = "All")
  {
    if(taskHelper == null)
      return null;

    if(string.IsNullOrWhiteSpace(name))
      name = "All";

    var taskName = GetTaskName(taskType, name);

    return taskHelper.GetTask(taskName, true, TargetCategory, taskType);
  }

  private ICakeContext Context;
  private TaskHelperModel TaskHelper { get; set; }

  private BuildHelperModel(
    ICakeContext context,
    TaskHelperModel taskHelper)
  {
    if(context == null)
			throw new ArgumentNullException("context", "context cannot be null.");

    if(taskHelper == null)
			throw new ArgumentNullException("taskHelper", "taskHelper cannot be null.");

    this.Context = context;
    this.TaskHelper = taskHelper;

    this.SetDefaults();
  }

  public CakeTaskBuilder<ActionTask> CleanTask(string target = "All")
  {
    return GetTask(this.TaskHelper, CleanTaskName, target);
  }

  public CakeTaskBuilder<ActionTask> PreBuildTask(string target = "All")
  {
    var clnTask = GetTask(this.TaskHelper, CleanTaskName, target);
    var preBuildTask = GetTask(this.TaskHelper, PreBuildTaskName, target);
    this.TaskHelper.AddTaskDependency(preBuildTask, clnTask.Task.Name);

    return preBuildTask;
  }

  public CakeTaskBuilder<ActionTask> BuildTask(string target = "All")
  {
    var preBuildTask = GetTask(this.TaskHelper, PreBuildTaskName, target);
    var buildTask = GetTask(this.TaskHelper, BuildTaskName, target);
    this.TaskHelper.AddTaskDependency(buildTask, preBuildTask.Task.Name);

    return buildTask;
  }

  public CakeTaskBuilder<ActionTask> TestTask(string target = "All")
  {
    var buildTask = GetTask(this.TaskHelper, BuildTaskName, target);
    var testTask = GetTask(this.TaskHelper, TestTaskName, target);
    this.TaskHelper.AddTaskDependency(testTask, buildTask.Task.Name);

    return testTask;
  }

  public CakeTaskBuilder<ActionTask> PostBuildTask(string target = "All")
  {
    var buildTask = GetTask(this.TaskHelper, BuildTaskName, target);
    var postBuildTask = GetTask(this.TaskHelper, PostBuildTaskName, target);
    this.TaskHelper.AddTaskDependency(postBuildTask, buildTask.Task.Name);

    return postBuildTask;
  }

  public CakeTaskBuilder<ActionTask> PackageBuildTask(string target = "All")
  {
    var postBuildTask = GetTask(this.TaskHelper, PostBuildTaskName, target);
    var packageTask = GetTask(this.TaskHelper, PackageBuildTaskName, target);
    this.TaskHelper.AddTaskDependency(packageTask, postBuildTask.Task.Name);

    return packageTask;
  }

  public CakeTaskBuilder<ActionTask> AddToClean(string taskName)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    var allTask = this.CleanTask();
    var newTask = this.CleanTask(taskName);

    allTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToPreBuild(string taskName)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    var allTask = this.PreBuildTask();
    var newTask = this.PreBuildTask(taskName);

    allTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToBuild(string taskName)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    var allTask = this.BuildTask();
    var newTask = this.BuildTask(taskName);

    allTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToPostBuild(string taskName)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    var allTask = this.PostBuildTask();
    var newTask = this.PostBuildTask(taskName);

    allTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToPackage(string taskName)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");;

    var allTask = this.PackageBuildTask();
    var newTask = this.PackageBuildTask(taskName);

    allTask
      .IsDependentOn(newTask);

    return newTask;
  }

  private void SetDefaults()
  {
    var clnTask = this.CleanTask()
      .Does(() => this.Context.Debug("In Clean-All Task"));

    var preBuildTask = this.PreBuildTask()
      .Does(() => this.Context.Debug("In PreBuild-All Task"));

    var buildTask = this.BuildTask()
      .Does(() => this.Context.Debug("In Build-All Task"));

    var postBuildTask = this.PostBuildTask()
      .Does(() => this.Context.Debug("In PostBuild-All Task"));

    var testTask = this.TestTask()
      .Does(() => this.Context.Debug("In Test-All Task"));

    var packageTask = this.PackageBuildTask()
      .Does(() => this.Context.Debug("In Package-All Task"));
  }
}

var BuildHelper = BuildHelperModel.CreateBuildHelper(Context, TaskHelper);