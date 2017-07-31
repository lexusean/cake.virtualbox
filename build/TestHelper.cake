// ---
// Usage example:
//
// #l "local:?path=TestHelper.cake"
//
// var testHelper = TestHelper;

#l "local:?path=TaskHelper.cake"
#l "local:?path=CleanHelper.cake"

public class TestHelperModel
{
  private const string TestTempFolderDefault = "TestTemp";
  private const string TargetCategory = "Test";
  private const string TargetConstraintFormat = "{0}-{1}";
  private const string SubTargetConstraintFormat = "{0}-{1}";

  public static TestHelperModel CreateTestHelper(
    ICakeContext context,
    TaskHelperModel taskHelper,
    CleanHelperModel cleanHelper)
  {
    return new TestHelperModel(context, taskHelper, cleanHelper);
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
    string testCategory,
    string name = "All", 
    bool isTarget = true)
  {
    if(taskHelper == null)
      return null;

    if(string.IsNullOrWhiteSpace(name))
      name = "All";

    if(string.IsNullOrWhiteSpace(testCategory))
      testCategory = "Generic";

    testCategory = string.Format("{0}-{1}", TargetCategory, testCategory);
    var taskName = GetTaskName(testCategory, name);

    return taskHelper.GetTask(taskName, isTarget, TargetCategory, testCategory);
  }

  private static CakeTaskBuilder<ActionTask> GetCleanTask(
    CleanHelperModel cleanHelper, 
    string testCategory,
    string name = "All", 
    bool isTarget = true)
  {
    if(cleanHelper == null)
      return null;

    if(string.IsNullOrWhiteSpace(name))
      name = "All";

    if(string.IsNullOrWhiteSpace(testCategory))
      testCategory = "Generic";

    var taskName = GetTaskName(testCategory, name);

    return cleanHelper.CleanTask(TargetCategory, taskName, !isTarget);
  }

  private DirectoryPath _TestTempFolder = null;
  public DirectoryPath TestTempFolder 
  { 
    get
    {
      if(this._TestTempFolder == null)
      {
        this._TestTempFolder = this.Context.Directory(TestTempFolderDefault);
        this._TestTempFolder = this.Context.MakeAbsolute(this._TestTempFolder);
      }

      return this._TestTempFolder;
    } 
    set
    {
      var val = value;
      if(val == null)
      {
        this._TestTempFolder = null;
      }
      else
      {
        this._TestTempFolder = value;
        this._TestTempFolder = this.Context.MakeAbsolute(this._TestTempFolder);
      }
    }
  }

  private ICakeContext Context;
  private TaskHelperModel TaskHelper { get; set; }
  private CleanHelperModel CleanHelper { get; set; }

  private CakeTaskBuilder<ActionTask> TestCleanAllTask { get; set; }

  private TestHelperModel(
    ICakeContext context,
    TaskHelperModel taskHelper,
    CleanHelperModel cleanHelper)
  {
    if(context == null)
			throw new ArgumentNullException("context", "context cannot be null.");

    if(taskHelper == null)
			throw new ArgumentNullException("taskHelper", "taskHelper cannot be null.");

    if(cleanHelper == null)
			throw new ArgumentNullException("cleanHelper", "cleanHelper cannot be null.");

    this.Context = context;
    this.TaskHelper = taskHelper;
    this.CleanHelper = cleanHelper;

    this.SetDefaults();
  }

  public DirectoryPath GetTestCategoryDirectory(string testCategory)
  {
    if(string.IsNullOrWhiteSpace(testCategory))
      throw new ArgumentNullException("testCategory", "Need a specific Test Category at this point (unit|system|etc).");

    var dir = this.TestTempFolder.Combine(this.Context.Directory(testCategory));
    return this.Context.MakeAbsolute(dir);
  }

  public CakeTaskBuilder<ActionTask> CleanTask(string testCategory = "", string target = "All", bool isSubTask = false)
  {
    return GetCleanTask(this.CleanHelper, testCategory, target, !isSubTask);
  }

  public CakeTaskBuilder<ActionTask> TestTask(string testCategory = "", string target = "All", bool isSubTask = false)
  {
    var testTask = GetTask(this.TaskHelper, testCategory, target, !isSubTask);
    if(!isSubTask)
    {
      var clnTask = GetCleanTask(this.CleanHelper, testCategory, target);
      this.TaskHelper.AddTaskDependency(testTask, clnTask.Task.Name);
    }

    return testTask;
  }

  public CakeTaskBuilder<ActionTask> TrxCombinerTask(string testCategory = "", string target = "All")
  {
    var newTarg = string.Format("TrxCombiner-{0}", target);
    var combinerTask = GetTask(this.TaskHelper, testCategory, newTarg, false);

    return combinerTask;
  }

  public CakeTaskBuilder<ActionTask> AddToClean(string taskName, string testCategory, bool isSubTask = false, string parentTaskName = "")
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    if(string.IsNullOrWhiteSpace(testCategory))
      throw new ArgumentNullException("testCategory", "Need a specific Test Category at this point (unit|system|etc).");

    if(isSubTask && string.IsNullOrWhiteSpace(parentTaskName))
      throw new ArgumentNullException("parentTaskName", "Need a specific Parent Task name if adding a sub task at this point.");

    var newTaskName = isSubTask ? string.Format(SubTargetConstraintFormat, parentTaskName, taskName) : taskName;

    var parentTask = isSubTask ? this.CleanTask(testCategory, parentTaskName) : this.CleanTask(testCategory);
    var newTask = this.CleanTask(testCategory, newTaskName, isSubTask);

    parentTask
      .IsDependentOn(newTask);

    if(!isSubTask)
    {
      this.TestCleanAllTask
        .IsDependentOn(parentTask);
    }

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddToTest(string taskName, string testCategory, bool isSubTask = false, string parentTaskName = "")
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    if(string.IsNullOrWhiteSpace(testCategory))
      throw new ArgumentNullException("testCategory", "Need a specific Test Category at this point (unit|system|etc).");

    if(isSubTask && string.IsNullOrWhiteSpace(parentTaskName))
      throw new ArgumentNullException("parentTaskName", "Need a specific Parent Task name if adding a sub task at this point.");

    var newTaskName = isSubTask ? string.Format(SubTargetConstraintFormat, parentTaskName, taskName) : taskName;

    var parentTask = isSubTask ? this.TestTask(testCategory, parentTaskName) : this.TestTask(testCategory);
    var newTask = this.TestTask(testCategory, newTaskName, isSubTask);

    parentTask
      .IsDependentOn(newTask);

    return newTask;
  }

  public CakeTaskBuilder<ActionTask> AddTrxCombiner(string taskName, string testCategory)
  {
    if(string.IsNullOrWhiteSpace(taskName))
      throw new ArgumentNullException("taskName", "Need a specific Task name at this point.");

    if(string.IsNullOrWhiteSpace(testCategory))
      throw new ArgumentNullException("testCategory", "Need a specific Test Category at this point (unit|system|etc).");

    var newTaskName = taskName;

    var parentTask = this.TestTask(testCategory, newTaskName);
    var newTask = this.TrxCombinerTask(testCategory, newTaskName);

    this.Context.Information("ParentTaskName: {0}, SubTaskName: {1}", parentTask.Task.Name, newTask.Task.Name);

    parentTask
      .IsDependentOn(newTask);

    return newTask;
  }

  private void SetDefaults()
  {
    this.TestCleanAllTask = this.CleanHelper.CleanTask(TargetCategory);
  }
}

var TestHelper = TestHelperModel.CreateTestHelper(Context, TaskHelper, CleanHelper);