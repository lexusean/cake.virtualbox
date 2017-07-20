// ---
// Usage example:
//
// #l "local:?path=TaskHelper.cake"
//
// var taskHelper = TaskHelper;

public class BuildTask
{
  public static BuildTask CreateTask(
    CakeTaskBuilder<ActionTask> task, 
    bool isTarget)
  {
    if(task == null)
      throw new ArgumentNullException("task", "Task cannot be empty");

    return new BuildTask(task, isTarget);
  }

  public CakeTaskBuilder<ActionTask> Task { get; set; }
  public bool IsTarget { get; set; }

  public string Name
  {
    get
    {
      if(this.Task == null)
        return string.Empty;
      
      return this.Task.Task.Name;
    }
  }

  private BuildTask(
    CakeTaskBuilder<ActionTask> task, 
    bool isTarget)
  {
    this.Task = task;
    this.IsTarget = isTarget;
  }
}

public class BuildTasks : List<BuildTask>
{
  public bool ContainsTask(string name)
  {
    return this.Any(t => t.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
  }
}

public class TaskHelperModel
{
  public static TaskHelperModel CreateTaskHelper(
    ICakeContext context)
  {
    return new TaskHelperModel(context);
  }

  public BuildTasks Tasks { get; set; }

  public IEnumerable<BuildTask> Targets 
  {
    get
    {
      return Tasks.Where(t => t.IsTarget);
    }
  }

  private ICakeContext Context;

  private TaskHelperModel(ICakeContext context)
  {
    if(context == null)
			throw new ArgumentNullException("context", "context cannot be null.");

    this.Tasks = new BuildTasks();
  }

  public void AddBuildTask(Func<CakeTaskBuilder<ActionTask>> taskFunc, bool isTarget = false)
  {
    if(taskFunc != null)
    {
      var task = taskFunc();
      if(task == null)
        return;

      if(this.Tasks.ContainsTask(task.Task.Name))
      {
        var message = string.Format("Another task with the name \'{0}\' has already been added.", task.Task.Name);
        this.Context.Debug(message);
        throw new Exception(message);
      }

      Tasks.Add(BuildTask.CreateTask(task, isTarget));
    }
  }
}

var TaskHelper = TaskHelperModel.CreateTaskHelper(Context);