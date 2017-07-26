// ---
// Usage example:
//
// #l "local:?path=TestHelper.cake"
//
// var testHelper = TestHelper;

#l "local:?path=TaskHelper.cake"

public class TestHelperModel
{
  private const string TargetCategory = "Test";
  private const string TargetConstraintFormat = "{0}-{1}";
  private const string SubTargetConstraintFormat = "{0}-{1}";

  public static TestHelperModel CreateTestHelper(
    ICakeContext context,
    TaskHelperModel taskHelper)
  {
    return new TestHelperModel(context, taskHelper);
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

  private ICakeContext Context;
  private TaskHelperModel TaskHelper { get; set; }

  private TestHelperModel(
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

  public CakeTaskBuilder<ActionTask> TestTask(string testCategory = "", string target = "All", bool isSubTask = false)
  {
    return GetTask(this.TaskHelper, testCategory, target, !isSubTask);
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

  private void SetDefaults()
  {
  }
}

var TestHelper = TestHelperModel.CreateTestHelper(Context, TaskHelper);