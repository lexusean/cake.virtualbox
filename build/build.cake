#l "local:?path=CommandHelper.cake"
#l "local:?path=BuildHelper.cake"
#l "local:?path=DotNetCoreHelper.cake"
//#l "local:?path=DotNetCoreHelper.cake"

var cmdHelper = CommandHelper;
cmdHelper.ScriptDescription = "Build Script Description Test";

TestHelper.TestTempFolder = Directory("../TestTemp");
BuildHelper.BuildTempFolder = Directory("../BuildTemp");

var dotCoreHelper = DotNetCoreHelper;
var testConfig = dotCoreHelper.GetTestConfig("Unit", "System");
var projConfig = dotCoreHelper.GetProjectConfig(File("../Cake.Virtualbox.sln"));
projConfig.Configuration = "Debug";
projConfig.Framework = "net452";

dotCoreHelper.AddProjectConfig(projConfig);
dotCoreHelper.AddTestConfig(projConfig, testConfig);


// --
// Setup Build Tasks
// -- 
cmdHelper.TaskHelper.AddBuildTask("Dummy")
	.Does(() =>
	{
		Information("Targets defined: ");
		foreach(var targ in TaskHelper.Targets)
		{
			Information("  - {0}", targ.Task.Name);
		}

		Information("Tasks defined: ");
		foreach(var t in TaskHelper.Tasks)
		{
			Information("  - {0}", t.Task.Name);
		}
	});

cmdHelper.TaskHelper.AddTask("Test")
	.Does(() =>
	{
		Information("Targets defined: ");
		foreach(var targ in TaskHelper.Targets)
		{
			Information("  - {0}", targ.Task.Name);
		}
	});

// --
// Execution
// --
cmdHelper.Run();
