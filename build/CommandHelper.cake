// ---
// Usage example:
//
// #l "local:?path=CommandHelper.cake"
//
// var cmdHelper = CmdHelper;
// cmdHelper.ScriptDescription = "Build Script Description";
// cmdHelper.TaskHelper.AddBuildTask(() => Task
//   .Does(() => ...),
//   isTarget: true);
//
// cmdHelper.Run();

#l "local:?path=TaskHelper.cake"

public class CommandHelperModel
{
	public static CommandHelperModel CreateCommandHandler(
    ICakeContext context,
    TaskHelperModel taskHelper,
    Action<string> runTargetAction)
  {
    return new CommandHelperModel(context, taskHelper, runTargetAction);
  }

  public class AllowedArgument
  {
    public static AllowedArgument CanAddArgument(
      IEnumerable<AllowedArgument> args, 
			ICakeContext context,
      string actionName,
      string shortName,
      string description)
    {
      if(args == null)
        return null;
      
      if(string.IsNullOrWhiteSpace(actionName) ||
        string.IsNullOrWhiteSpace(shortName) ||
        string.IsNullOrWhiteSpace(description))
        return null;

      var newArg = new AllowedArgument(context, actionName, shortName, description);

      var actionExists = args.Any(t => t.ActionName.Equals(newArg.ActionName));
      actionExists |= args.Any(t => t.ShortName.Equals(newArg.ShortName));

      if(actionExists)
        return null;

      return newArg;
    }

    public string ActionName;
    public string ShortName;
    public string Description;
    public Action<AllowedArgument> ArgumentAction; 
    public List<AllowedArgument> AllowedArguments; 

    public string ArgumentValue
    {
      get
      {
        return this.GetArgumentValue();
      }
    }

    public bool CommandLineHasArgument
    {
      get
      {
        return this.GetCommandLineHasArgument();
      }
    }

		private ICakeContext Context;
		
    private AllowedArgument(
			ICakeContext context,
      string actionName,
      string shortName,
      string description)
    {
			if(context == null)
				throw new ArgumentNullException("context", "context cannot be null " + this.GetType().ToString());
			
			this.Context = context;
      this.ActionName = actionName;
      this.ShortName = shortName;
      this.Description = description;
			
			this.AllowedArguments = new List<AllowedArgument>();
    }

    public AllowedArgument AddArgument(
      string actionName,
      string shortName,
      string description)
    {
      var arg = CanAddArgument(this.AllowedArguments, this.Context, actionName, shortName, description);
      if(arg != null)
        this.AllowedArguments.Add(arg);

      return arg;
    }

    private bool GetCommandLineHasArgument()
    {
			var exists = this.Context.HasArgument(this.ActionName);
      exists |= this.Context.HasArgument(this.ShortName);

      return exists;
    }

    private string GetArgumentValue()
    {
      var longArgValue = this.Context.Argument(this.ActionName, string.Empty);
      var shortArgValue = this.Context.Argument(this.ShortName, string.Empty);

      if(!string.IsNullOrEmpty(longArgValue))
        return longArgValue;

      if(!string.IsNullOrEmpty(shortArgValue))
        return shortArgValue;

      return string.Empty;
    }
  }

  public string ScriptDescription;

  private List<AllowedArgument> _AllowedArguments = new List<AllowedArgument>();
  public IEnumerable<AllowedArgument> AllowedArguments 
  {
    get { return this._AllowedArguments; }
  }

  public AllowedArgument HelpArgument;
  public AllowedArgument RunArgument;
	public AllowedArgument AvailableTargetsArgument;

  public TaskHelperModel TaskHelper;
  public string DefaultTarget = string.Empty;

	private ICakeContext Context;
  private Action<string> _RunTargetAction;
	
  private CommandHelperModel(
    ICakeContext context,
    TaskHelperModel taskHelper,
    Action<string> runTargetAction)
  {
		if(context == null)
			throw new ArgumentNullException("context", "context cannot be null.");
		
    if(taskHelper == null)
      throw new ArgumentNullException("taskHelper", "taskHelper cannot be null.");
    
    if(runTargetAction == null)
      throw new ArgumentNullException("runTargetAction", "runTargetAction cannot be null.");

		this.Context = context;
    this.TaskHelper = taskHelper;
    this._RunTargetAction = runTargetAction;
		
		this.ScriptDescription = "This is a cake build script";
    this.AddDefaultArguments();
  }

  public string GetTarget()
  {
    if(this.RunArgument == null)
      return string.Empty;

    return this.RunArgument.ArgumentValue;
  }

  public AllowedArgument AddArgument(
      string actionName,
      string shortName,
      string description)
  {
    var arg = AllowedArgument.CanAddArgument(this.AllowedArguments, this.Context, actionName, shortName, description);
    if(arg != null)
      this._AllowedArguments.Add(arg);

    return arg;
  }

  public void Run()
  {
    var isHelp = this.HelpArgument.CommandLineHasArgument;
    this.Context.Debug("Help Set: {0}", isHelp);

    bool hadArguments = false;
    foreach(var arg in this.AllowedArguments.Where(t => t != this.HelpArgument))
    {
      if(!arg.CommandLineHasArgument)
        continue;
			
      hadArguments = true;
      if(isHelp)
      {
        this.RunHelp(arg);
				return;
      }
      else
      {
        if(arg.ArgumentAction == null)
        {
          this.Context.Information("No Action Defined for Argument: {0}", arg.ActionName);
        }
        else
        {
          arg.ArgumentAction(arg);
        }
      }

      break;
    }

		if(isHelp || !hadArguments)
      this.RunHelp();
  }

  private void RunHelp(AllowedArgument arg = null)
  {
		this.Context.Information("\n");
		
		if(arg != null)
    {
      this.Context.Information(arg.Description);
    }
    else
    {
      var desc = this.ScriptDescription + "\n";
      desc += "Typical Usage:\n";
      desc += "  Windows:\n";
      desc += "    build.ps1 <--longAction | -shortAction>=<action option>\n";
      desc += "  Linux:\n";
      desc += "    build.sh <--longAction | -shortAction>=<action option>\n";
      
			desc += "\n";
      desc += "Available Actions:\n";
      foreach(var a in this.AllowedArguments)
      {
        desc += "  --" + a.ActionName + " | -" + a.ShortName + "\n";
      }

			desc += "\n";
      desc += this.HelpArgument.Description;
			this.Context.Information(desc);
    }
		
		this.Context.Information("\n");
  }

  private void AddDefaultArguments()
  {
    this.AddHelpArgument();
    this.AddRunTargetArgument();
		this.AddAvailTargetsArgument();
  }

  private void AddHelpArgument()
  {
    var desc = "Action: --use | -h\n";
		desc += "Description: Shows help for arguments\n";
    desc += "Typical Usage:\n";
    desc += "  Windows:\n";
    desc += "    build.ps1 -h(--use)\n";
    desc += "    build.ps1 <action> -h(--use)\n";
    desc += "  Linux:\n";
    desc += "    build.sh -h(--use)\n";
    desc += "    build.sh <action> -h(--use)\n";

    this.HelpArgument = this.AddArgument("use", "h", desc);
  }

  private void AddRunTargetArgument()
  {
    var desc = "Action: -run | -r\n";
		desc += "Description: Runs a build target\n";
    desc += "Typical Usage:\n";
    desc += "  Windows:\n";
    desc += "    build.ps1 -r(-run)=<target>\n";
		desc += "  	   Run to get available targets: build.ps1 -at(--available-targets)\n";
    desc += "  Linux:\n";
    desc += "    build.sh -r(-run)=<target>\n";
		desc += "  	   Run to get available targets: build.sh -at(--available-targets)\n";

    this.RunArgument = this.AddArgument("run", "r", desc);
    this.RunArgument.ArgumentAction = arg =>
    {
      var target = arg.ArgumentValue;
      target = string.IsNullOrWhiteSpace(target) ? this.DefaultTarget : target;

      if(string.IsNullOrWhiteSpace(target))
      {
        var message = "No target or Default Target defined for --run | -r";
        this.Context.Error(message);
        throw new ArgumentNullException("target", message);
      }

      this._RunTargetAction(target);
    };
  }
	
	private void AddAvailTargetsArgument()
  {
    var desc = "Action: -available-targets | -at\n";
		desc += "Description: Lists all targets defined for use in script\n";
    desc += "Typical Usage:\n";
    desc += "  Windows:\n";
    desc += "    build.ps1 -at(-available-targets)\n";
    desc += "  Linux:\n";
    desc += "    build.sh -at(-available-targets)\n";

    this.AvailableTargetsArgument = this.AddArgument("available-targets", "at", desc);
		this.AvailableTargetsArgument.ArgumentAction = arg =>
		{
      var targets = this.TaskHelper.Targets.ToList();

			this.Context.Information("");
			this.Context.Information("{0} Targets Avaiable:\n", targets.Count);
			
			foreach(var targ in targets)
			{
				this.Context.Information("  - {0}", targ.Name);
			}
			
			this.Context.Information("");
		};
  }
}

Action<string> targetAction = target =>
{
  RunTarget(target);
};
var CommandHelper = CommandHelperModel.CreateCommandHandler(Context, TaskHelper, targetAction);