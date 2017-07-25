// ---
// Usage example:
//
// #l "local:?path=TaskHelper.cake"
//
// var taskHelper = TaskHelper;

public class TaskHelperModel
{
  private const string DefaultTaskCategory = "Generic";
  public const string DefaultTaskType = "Unknown";

  public class ActionTaskDataModel
  {
    public ActionTask Task { get; set; }
    public bool IsTarget { get; set; }

    private string _Category = DefaultTaskCategory;
    public string Category 
    { 
      get
      {
        if(string.IsNullOrWhiteSpace(this._Category))
        {
          this._Category = DefaultTaskCategory;
        }

        return this._Category;
      } 
      set
      {
        this._Category = value;
      }
    }

    private string _TaskType = DefaultTaskType;
    public string TaskType 
    { 
      get
      {
        if(string.IsNullOrWhiteSpace(this._TaskType))
        {
          this._TaskType = DefaultTaskType;
        }

        return this._TaskType;
      } 
      set
      {
        this._TaskType = value;
      }
    }

    public ActionTaskDataModel(ActionTask task)
    {
      if (task == null)
        throw new ArgumentNullException("task");

      this.Task = task;
    }
  }

  private class ActionTaskDataCache
  {
    public Dictionary<ActionTask, ActionTaskDataModel> Cache = new Dictionary<ActionTask, ActionTaskDataModel>();

    public ActionTaskDataModel AddTask(ActionTask task)
    {
      return this.AddBuildTask(task, false);
    }

    public ActionTaskDataModel AddBuildTask(ActionTask task, bool isTarget = true)
    {
      ActionTaskDataModel model = null;
      if(this.Cache.ContainsKey(task))
        model = this.Cache[task];

      if(model == null)
      {
        model = new ActionTaskDataModel(task);
        this.Cache.Add(task, model);
      }

      model.IsTarget = isTarget;
      return model;
    }

    public ActionTaskDataModel GetTaskData(ActionTask task)
    {
      if(!this.Cache.ContainsKey(task))
        return this.Cache[task];

      return this.AddTask(task);
    }

    public void RemoveTask(ActionTask task)
    {
      if(!this.Cache.ContainsKey(task))
        return;
      
      this.Cache.Remove(task);
    }

    public void RemoveAll()
    {
      this.Cache.Clear();
    }
  }

  public static TaskHelperModel CreateTaskHelper(
    ICakeContext context)
  {
    return new TaskHelperModel(context);
  }

  public IEnumerable<ActionTaskDataModel> TaskData
  {
    get
    {
      return this.TaskCache.Cache
        .Select(kvp => kvp.Value);
    }
  }
  
  public IEnumerable<CakeTaskBuilder<ActionTask>> Tasks
  {
    get
    {
      return this.TaskCache.Cache
        .Select(kvp => new CakeTaskBuilder<ActionTask>(kvp.Key));
    }
  }

  public IEnumerable<CakeTaskBuilder<ActionTask>> Targets
  {
    get
    {
      return this.TaskCache.Cache
        .Where(kvp => kvp.Value.IsTarget)
        .Select(kvp => new CakeTaskBuilder<ActionTask>(kvp.Key));
    }
  }

  public IEnumerable<string> Categories
  {
    get
    {
      return this.TaskCache.Cache
        .Select(kvp => kvp.Value.Category)
        .Distinct();
    }
  }

  public Func<string, CakeTaskBuilder<ActionTask>> ScriptHostTaskFunc { get; set; }

  private ActionTaskDataCache TaskCache { get; set; }
  private ICakeContext Context;

  private TaskHelperModel(ICakeContext context)
  {
    this.TaskCache = new ActionTaskDataCache();

    if(context == null)
			throw new ArgumentNullException("context", "context cannot be null.");

    this.Context = context;
  }

  public string GetCategoryForTask(string taskName)
  {
    var category = this.TaskCache.Cache
      .Where(t => t.Key.Name == taskName)
      .Select(t => t.Value.Category)
      .FirstOrDefault();

    return string.IsNullOrWhiteSpace(category) ? DefaultTaskCategory : category;
  }

  public IEnumerable<CakeTaskBuilder<ActionTask>> GetTasksForCategory(string category)
  {
    return this.TaskCache.Cache
        .Where(kvp => kvp.Value.Category == category)
        .Select(kvp => new CakeTaskBuilder<ActionTask>(kvp.Key));
  }

  public CakeTaskBuilder<ActionTask> GetTask(
    string taskName, 
    bool isTarget = false, 
    string category = "",
    string taskType = "")
  {
    if(this.ScriptHostTaskFunc == null)
      return null;

    var task = this.TaskCache.Cache
      .Where(t => t.Key.Name == taskName)
      .Select(t => t.Key)
      .FirstOrDefault();

    if(task != null)
      return new CakeTaskBuilder<ActionTask>(task);
    
    if(isTarget)
      return this.AddBuildTask(taskName, category, taskType);
    
    return this.AddTask(taskName, category, taskType);
  }

  public CakeTaskBuilder<ActionTask> AddTask(
    string taskName, 
    string category = "", 
    string taskType = "")
  {
    if(this.ScriptHostTaskFunc == null)
      return null;

    if(string.IsNullOrWhiteSpace(taskName))
      return null;
    
    var task = this.ScriptHostTaskFunc(taskName);

    var model = this.TaskCache.AddTask(task.Task);
    model.Category = category;
    model.TaskType = taskType;

    return task;
  }

  public CakeTaskBuilder<ActionTask> AddTask(
    CakeTaskBuilder<ActionTask> task, 
    string category = "",
    string taskType = "")
  {
    if(task == null)
      return null;
    
    var model = this.TaskCache.AddTask(task.Task);
    model.Category = category;
    model.TaskType = taskType;

    return task;
  }

  public CakeTaskBuilder<ActionTask> AddTask(
    ActionTask task, 
    string category = "",
    string taskType = "")
  {
    if(task == null)
      return null;
    
    var model = this.TaskCache.AddTask(task);
    model.Category = category;
    model.TaskType = taskType;

    return new CakeTaskBuilder<ActionTask>(task);
  }

  public CakeTaskBuilder<ActionTask> AddBuildTask(
    string taskName, 
    string category = "",
    string taskType = "")
  {
    if(this.ScriptHostTaskFunc == null)
      return null;

    if(string.IsNullOrWhiteSpace(taskName))
      return null;
    
    var task = this.ScriptHostTaskFunc(taskName);

    var model = this.TaskCache.AddBuildTask(task.Task);
    model.Category = category;
    model.TaskType = taskType;

    return task;
  }

  public CakeTaskBuilder<ActionTask> AddBuildTask(
    CakeTaskBuilder<ActionTask> task, 
    string category = "",
    string taskType = "")
  {
    if(task == null)
      return null;
    
    var model = this.TaskCache.AddBuildTask(task.Task, true);
    model.Category = category;
    model.TaskType = taskType;

    return task;
  }

  public CakeTaskBuilder<ActionTask> AddBuildTask(
    ActionTask task, 
    string category = "",
    string taskType = "")
  {
    if(task == null)
      return null;
    
    var model = this.TaskCache.AddBuildTask(task, true);
    model.Category = category;
    model.TaskType = taskType;

    return new CakeTaskBuilder<ActionTask>(task);
  }

  public void RemoveTask(string taskName)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      return;

    var task = this.TaskCache.Cache
      .Where(t => t.Key.Name == taskName)
      .Select(t => t.Key)
      .FirstOrDefault();

    this.RemoveTask(task);
  }

  public void RemoveTask(ActionTask task)
  {
    if(task == null)
      return;
    
    this.TaskCache.RemoveTask(task);
  }

  public void AddTaskDependency(CakeTaskBuilder<ActionTask> originatingTask, string taskName)
  {
    if(originatingTask == null)
      return;

    if(originatingTask.Task.Dependencies.Any(t => t == taskName))
      return;

    originatingTask.IsDependentOn(taskName);
  }
}

var TaskHelper = TaskHelperModel.CreateTaskHelper(Context);
TaskHelper.ScriptHostTaskFunc = Task;