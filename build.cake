#tool "GitVersion.CommandLine"
#addin "Cake.DocFx"
#tool "docfx.console"

#l "local:?path=build/CommandHelper.cake"

var target = "default";
CommandHelper.RunCommandHandler(
	// Cake Context
	Context,
	// Target handling
	targetArg => 
	{
		target = targetArg;
	},
	// Function to return available targets
	() =>
	{
		return new string[] 
		{
			"Clean",
			"Clean-All",
			"Restore",
			"Build",
			"Build-Docs"
		};
	});

//////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var configuration = Argument<string>("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var solutionPath = File("./Cake.Virtualbox.sln");
var solution = ParseSolution(solutionPath);
var projects = solution.Projects;
var projectPaths = projects.Select(p => p.Path.GetDirectory());
var deployProjects = projects
	.Where(p => !p.Name.Contains(".Test."));
var testAssemblies = projects
	.Where(p => p.Name.Contains(".Test."))
	.Select(p => p.Path.GetDirectory() + "/bin/" + configuration + "/" + p.Name + ".dll");
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
	
	Information("Branch: {0}", versionInfo.BranchName);
	Information("Metadata {0}", versionInfo.FullBuildMetaData);
	Information("Full SemVer {0}", versionInfo.FullSemVer);
	Information("SemVer {0}", versionInfo.SemVer);
	Information("Patch {0}", versionInfo.CommitsSinceVersionSourcePadded);
	Information("Info Ver {0}", versionInfo.InformationalVersion);
	Information("Assem Ver {0}", versionInfo.AssemblySemVer);
	
	
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
		DotNetCoreClean(solutionPath, new DotNetCoreCleanSettings
		{
			 Configuration = configuration
		});
	});

Task("Clean-All")
	.Does(() => 
	{
			Information("Cleaning bin/obj/artifact/docs folders");
		
			CleanDirectories("./**/bin");
			CleanDirectories("./**/obj");
			CleanDirectories("./docs/_site");
			CleanDirectories(artifacts);
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

Task("Build-Docs")
	.Does(() => 
	{
		var docsPath = Directory("./docs");
		
		DocFxMetadata(new DocFxMetadataSettings()
		{
				WorkingDirectory = docsPath
		});
		
		DocFxBuild(new DocFxBuildSettings()
		{
				WorkingDirectory = docsPath
		});
	});

Task("Post-Build")
	.IsDependentOn("Build")
	.IsDependentOn("Build-Docs")
	.Does(() =>
{
	CreateDirectory(artifacts + "build");
	
	foreach (var project in deployProjects) 
	{
		CreateDirectory(artifacts + "build/" + project.Name);
		CopyFiles(GetFiles(project.Path.GetDirectory() + "/" + project.Name + ".xml"), artifacts + "build/" + project.Name);
		var files = GetFiles(project.Path.GetDirectory() +"/bin/" + configuration +"/" + project.Name +".*");
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

Task("Dummy")
	.Does(() =>
	{
		Information("Running Dummy");
	});

if(target != "default")
	RunTarget(target);