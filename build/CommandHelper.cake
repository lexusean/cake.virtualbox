// ---
// Usage example:
//
// #l "local:?path=CommandHelper.cake"
//
// var target = "default";
// CommandHelper.RunCommandHandler(
// 	// Cake Context
// 	Context,
// 	// Target handling
// 	targetArg => 
// 	{
// 		target = targetArg;
// 	},
// 	// Function to return available targets
// 	() =>
// 	{
// 		return new string[] 
// 		{
// 			"dummy"
// 		};
// 	});
// 
// Task("Dummy")
// 	.Does(() =>
// 	{
// 		Information("Running Dummy");
// 	});
// 
// if(target != "default")
// 	RunTarget(target);

public class CommandHelper
{
	public static List<string> AvailableTargets = new List<string>();
	
	public static void RunCommandHandler(
		ICakeContext context, 
		Action<string> targetSetAction,
		Func<IEnumerable<string>> availableTargetsFunc)
  {
		if(availableTargetsFunc != null)
		{
			var availTargets = availableTargetsFunc();
			if(availTargets != null)
			{
				AvailableTargets.AddRange(availTargets);
			}
		}
		
    var helper = new CommandHelper(context);
		helper.RunArgument.ArgumentAction = arg =>
		{
			if(targetSetAction != null)
			{
				var target = arg.ArgumentValue();
				if(!string.IsNullOrWhiteSpace(target))
				{
					targetSetAction(target);
				}
			}
		};
		
		helper.Run();
  }
	
  public static CommandHelper CreateCommandHandler(ICakeContext context)
  {
    return new CommandHelper(context);
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

    public bool CommandLineHasArgument()
    {
			var exists = this.Context.HasArgument(this.ActionName);
      exists |= this.Context.HasArgument(this.ShortName);

      return exists;
    }

    public string ArgumentValue()
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

	private ICakeContext Context;
	
  private CommandHelper(ICakeContext context)
  {
		if(context == null)
			throw new ArgumentNullException("context", "context cannot be null. Type: " + this.GetType().ToString());
		
		this.Context = context;
		
		this.ScriptDescription = "This is a cake build script";
    this.AddDefaultArguments();
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
    var isHelp = this.HelpArgument.CommandLineHasArgument();
    this.Context.Debug("Help Set: {0}", isHelp);

    bool hadArguments = false;
    foreach(var arg in this.AllowedArguments.Where(t => t != this.HelpArgument))
    {
      if(!arg.CommandLineHasArgument())
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
          this.Context.Debug("No Action Defined for Argument: {0}", arg.ActionName);
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
			this.Context.Information("");
			this.Context.Information("{0} Targets Avaiable:\n", CommandHelper.AvailableTargets.Count);
			
			foreach(var targ in CommandHelper.AvailableTargets)
			{
				this.Context.Information("  - {0}", targ);
			}
			
			this.Context.Information("");
		};
  }
}