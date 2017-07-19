#tool "GitVersion.CommandLine"
#addin "Cake.DocFx"
#tool "docfx.console"

//////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var solutionPath = File("./Cake.Virtualbox.sln");
var solution = ParseSolution(solutionPath);
var projects = solution.Projects;
var projectPaths = projects.Select(p => p.Path.GetDirectory());
var testAssemblies = projects.Where(p => p.Name.Contains(".Tests")).Select(p => p.Path.GetDirectory() + "/bin/" + configuration + "/" + p.Name + ".dll");
var artifacts = "./dist/";
var testResultsPath = MakeAbsolute(Directory(artifacts + "./test-results"));
GitVersion versionInfo = null;
DotNetCoreMSBuildSettings msBuildSettings = null;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
	// Executed BEFORE the first task.
	Information("Running tasks...");
	versionInfo = GitVersion();
	Information("Building for version {0}", versionInfo.FullSemVer);
	
	msBuildSettings = new DotNetCoreMSBuildSettings()
		.WithProperty("Version", versionInfo.FullSemVer);
		//.WithProperty("AssemblyVersion", parameters.Version.Version)
		//.WithProperty("FileVersion", parameters.Version.Version);
});

Teardown(ctx =>
{
	// Executed AFTER the last task.
	Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
	.Does(() =>
	{
		// Clean solution directories.
		foreach(var path in projectPaths)
		{
			Information("Cleaning {0}", path);
			CleanDirectories(path + "/**/bin/" + configuration);
			CleanDirectories(path + "/**/obj/" + configuration);
		}
		
		Information("Cleaning common files...");
		CleanDirectory(artifacts);
	});

Task("Restore")
	.Does(() =>
	{
		// Restore all NuGet packages.
		Information("Restoring solution...");
		DotNetCoreRestore(solutionPath, new DotNetCoreRestoreSettings
		{
			Verbosity = DotNetCoreVerbosity.Minimal,
			Sources = new [] {
				"https://api.nuget.org/v3/index.json",
			},
			MSBuildSettings = msBuildSettings
		});
	});

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.Does(() =>
	{
		Information("Building solution...");
		var path = MakeAbsolute(solutionPath);
		DotNetCoreBuild(path.FullPath, new DotNetCoreBuildSettings()
		{
			Configuration = configuration,
			MSBuildSettings = msBuildSettings
		});
	});

Task("Generate-Docs")
	.Does(() => 
	{
		DocFxMetadata("./docs/docfx.json");
		DocFxBuild("./docs/docfx.json");
	});

Task("Post-Build")
	.IsDependentOn("Build")
	.IsDependentOn("Generate-Docs")
	.Does(() =>
{
	CreateDirectory(artifacts + "build");
	
	foreach (var project in projects) 
	{
		CreateDirectory(artifacts + "build/" + project.Name);
		CopyFiles(GetFiles(project.Path.GetDirectory() + "/" + project.Name + ".xml"), artifacts + "build/" + project.Name);
		var files = GetFiles(project.Path.GetDirectory() +"/bin/" +configuration +"/" +project.Name +".*");
		CopyFiles(files, artifacts + "build/" + project.Name);
	}
	//Package docs
	Zip("./docs/_site/", artifacts + "/docfx.zip");
});

Task("Run-Unit-Tests")
	.IsDependentOn("Build")
	.Does(() =>
{
	var projects = GetFiles("./test/**/*.Tests.*.csproj");
	foreach (var project in projects) 
	{
		DotNetCoreTest(project.FullPath, new DotNetCoreTestSettings
		{
			OutputDirectory = testResultsPath,
			Logger = "trx;LogFileName=TestResults.xml",
			Filter = "TestCategory=unit",
			NoBuild = true,
			
		});
	}
});

Task("NuGet")
	.IsDependentOn("Post-Build")
	.IsDependentOn("Run-Unit-Tests")
	.Does(() => 
	{
		CreateDirectory(artifacts + "package/");
		Information("Building NuGet package");
	
		var nuspecFiles = GetFiles("./**/*.nuspec");
		var versionNotes = ParseAllReleaseNotes("./ReleaseNotes.md")
			.FirstOrDefault(v => v.Version.ToString() == versionInfo.MajorMinorPatch);
			
		NuGetPack(nuspecFiles, new NuGetPackSettings() 
		{
			Version = versionInfo.NuGetVersionV2,
			ReleaseNotes = versionNotes != null ? versionNotes.Notes.ToList() : new List<string>(),
			OutputDirectory = artifacts + "/package"
		});
	});
	
///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Default")
	.IsDependentOn("NuGet");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);