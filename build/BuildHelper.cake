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
  private const string SubTargetConstraintFormat = "{0}-{1}";
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

  private static CakeTaskBuilder<ActionTask> GetTask(TaskHelperModel taskHelper, string taskType, string name = "All", bool isTarget = true)
  {
    if(taskHelper == null)
      return null;

    if(string.IsNullOrWhiteSpace(name))
      name = "All";

    var taskName = GetTaskName(taskType, name);

    return taskHelper.GetTask(taskName, isTarget, TargetCategory, taskType);
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

  public CakeTaskBuilder<ActionTask> CleanTask(string target = "All", bool isSubTask = false)
  {
    return GetTask(this.TaskHelper, CleanTaskName, target, !isSubTask);
  }

  public CakeTaskBuilder<ActionTask> PreBuildTask(string target = "All", bool isSubTask = false)
  {
    var preBuildTask = GetTask(this.TaskHelper, PreBuildTaskName, target, !isSubTask);

    if(!isSubTask)
    {
      var clnTask = GetTask(this.TaskHelper, CleanTaskName, target);
      this.TaskHelper.AddTaskDependency(preBuildTask, clnTask.Task.Name);
    }

    return preBuildTask;
  }

  public CakeTaskBuilder<ActionTask> BuildTask(string target = "All", bool isSubTask = false)
  {
    var buildTask = GetTask(this.TaskHelper, BuildTaskName, target, !isSubTask);

    if(!isSubTask)
    {
      var preBuildTask = GetTask(this.TaskHelper, PreBuildTaskName, target);
      this.TaskHelper.AddTaskDependency(buildTask, preBuildTask.Task.Name);
    }

    return buildTask;
  }

  public CakeTaskBuilder<ActionTask> PostBuildTask(string target = "All", bool isSubTask = false)
  {
    var postBuildTask = GetTask(this.TaskHelper, PostBuildTaskName, target, !isSubTask);

    return postBuildTask;
  }

  public CakeTaskBuilder<ActionTask> PackageBuildTask(string target = "All", bool isSubTask = false)
  {
    var packageTask = GetTask(this.TaskHelper, PackageBuildTaskName, target, !isSubTask);

    if(!isSubTask)
    {
      var postBuildTask = GetTask(this.TaskHelper, PostBuildTaskName, target);
      this.TaskHelper.AddTaskDependency(packageTask, postBuildTask.Task.Name);
    }

    return packageTask;
  }

  public CakeTaskBuilder<ActionTask> AddToClean(string taskName, bool isSubTask = false, string parentTaskName = "")
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    if(isSubTask && string.IsNullOrWhiteSpace(parentTaskName))
      throw new ArgumentNullException("parentTaskName", "Need a specific Parent Task name if adding a sub task at this point.");

    var newTaskName = isSubTask ? string.Format(SubTargetConstraintFormat, parentTaskName, taskName) : taskName;

    var parentTask = isSubTask ? this.CleanTask(parentTaskName) : this.CleanTask();
    var newTask = this.CleanTask(newTaskName, isSubTask);

    parentTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToPreBuild(string taskName, bool isSubTask = false, string parentTaskName = "")
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    if(isSubTask && string.IsNullOrWhiteSpace(parentTaskName))
      throw new ArgumentNullException("parentTaskName", "Need a specific Parent Task name if adding a sub task at this point.");

    var newTaskName = isSubTask ? string.Format(SubTargetConstraintFormat, parentTaskName, taskName) : taskName;

    var parentTask = isSubTask ? this.PreBuildTask(parentTaskName) : this.PreBuildTask();
    var newTask = this.PreBuildTask(newTaskName, isSubTask);

    parentTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToBuild(string taskName, bool isSubTask = false, string parentTaskName = "")
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    if(isSubTask && string.IsNullOrWhiteSpace(parentTaskName))
      throw new ArgumentNullException("parentTaskName", "Need a specific Parent Task name if adding a sub task at this point.");

    var newTaskName = isSubTask ? string.Format(SubTargetConstraintFormat, parentTaskName, taskName) : taskName;

    var parentTask = isSubTask ? this.BuildTask(parentTaskName) : this.BuildTask();
    var newTask = this.BuildTask(newTaskName, isSubTask);

    parentTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToPostBuild(string taskName, bool isSubTask = false, string parentTaskName = "")
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    if(isSubTask && string.IsNullOrWhiteSpace(parentTaskName))
      throw new ArgumentNullException("parentTaskName", "Need a specific Parent Task name if adding a sub task at this point.");

    var newTaskName = isSubTask ? string.Format(SubTargetConstraintFormat, parentTaskName, taskName) : taskName;

    var parentTask = isSubTask ? this.PostBuildTask(parentTaskName) : this.PostBuildTask();
    var newTask = this.PostBuildTask(newTaskName, isSubTask);

    parentTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToPackage(string taskName, bool isSubTask = false, string parentTaskName = "")
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    if(isSubTask && string.IsNullOrWhiteSpace(parentTaskName))
      throw new ArgumentNullException("parentTaskName", "Need a specific Parent Task name if adding a sub task at this point.");

    var newTaskName = isSubTask ? string.Format(SubTargetConstraintFormat, parentTaskName, taskName) : taskName;

    var parentTask = isSubTask ? this.PackageBuildTask(parentTaskName) : this.PackageBuildTask();
    var newTask = this.PackageBuildTask(newTaskName, isSubTask);

    parentTask
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

    var packageTask = this.PackageBuildTask()
      .Does(() => this.Context.Debug("In Package-All Task"));
  }
}

var BuildHelper = BuildHelperModel.CreateBuildHelper(Context, TaskHelper);