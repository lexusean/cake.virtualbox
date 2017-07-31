// ---
// Usage example:
//
// #l "local:?path=CleanHelper.cake"
//
// var cleanHelper = CleanHelper;

#l "local:?path=TaskHelper.cake"

public class CleanHelperModel
{
  private const string TargetCategory = "Clean";
  private const string TargetConstraintFormat = "{0}-{1}";
  private const string SubTargetConstraintFormat = "{0}-{1}";

  public static CleanHelperModel CreateCleanHelper(
    ICakeContext context,
    TaskHelperModel taskHelper)
  {
    return new CleanHelperModel(context, taskHelper);
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

  private static CakeTaskBuilder<ActionTask> GetTask(
    TaskHelperModel taskHelper, 
    string cleanCategory = "",
    string name = "All", 
    bool isTarget = true)
  {
    if(taskHelper == null)
      return null;

    if(string.IsNullOrWhiteSpace(name))
      name = "All";

    if(string.IsNullOrWhiteSpace(cleanCategory))
      cleanCategory = "Generic";

    cleanCategory = string.Format("{0}-{1}", TargetCategory, cleanCategory);
    var taskName = GetTaskName(cleanCategory, name);

    return taskHelper.GetTask(taskName, isTarget, TargetCategory, cleanCategory);
  }

  private ICakeContext Context;
  private TaskHelperModel TaskHelper { get; set; }

  private CleanHelperModel(
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

  public CakeTaskBuilder<ActionTask> CleanTask(string cleanCategory = "", string target = "All", bool isSubTask = false)
  {
    return GetTask(this.TaskHelper, cleanCategory, target, !isSubTask);
  }

  public CakeTaskBuilder<ActionTask> AddToClean(string taskName, string cleanCategory = "", bool isSubTask = false, string parentTaskName = "")
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    if(isSubTask && string.IsNullOrWhiteSpace(parentTaskName))
      throw new ArgumentNullException("parentTaskName", "Need a specific Parent Task name if adding a sub task at this point.");

    var newTaskName = isSubTask ? string.Format(SubTargetConstraintFormat, parentTaskName, taskName) : taskName;

    var parentTask = isSubTask ? this.CleanTask(cleanCategory, parentTaskName) : this.CleanTask(cleanCategory);
    var newTask = this.CleanTask(cleanCategory, newTaskName, isSubTask);

    parentTask
      .IsDependentOn(newTask);

    return newTask;
  }

  private void SetDefaults()
  {
  }
}

var CleanHelper = CleanHelperModel.CreateCleanHelper(Context, TaskHelper);