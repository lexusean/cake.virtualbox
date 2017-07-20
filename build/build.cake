#l "local:?path=CommandHelper.cake"

var cmdHelper = CommandHelper;
cmdHelper.ScriptDescription = "Build Script Description Test";

// --
// Setup Build Tasks
// -- 
cmdHelper.TaskHelper.AddBuildTask(() => 
	Task("Dummy")
		.Does(() =>
		{
			Information("Targets defined: ");
			foreach(var targ in TaskHelper.Targets)
			{
				Information("  - {0}", targ.Name);
			}

			Information("Tasks defined: ");
			foreach(var t in TaskHelper.Tasks)
			{
				Information("  - {0}", t.Name);
			}
		}),
	isTarget: true);

cmdHelper.TaskHelper.AddBuildTask(() =>
	Task("Test")
		.Does(() =>
		{
			Information("Targets defined: ");
			foreach(var targ in TaskHelper.Targets)
			{
				Information("  - {0}", targ.Name);
			}
		}));

// --
// Execution
// --
cmdHelper.Run();
