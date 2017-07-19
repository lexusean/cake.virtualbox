# Getting Started

Make sure you have Virtualbox installed *first*, as the addin doesn't include it.

## Including the addin

At the top of your script, just add the following to install the addin:

```
#addin nuget:?package=Cake.Virtualbox
```

## Usage

The addin exposes a single property alias `Virtualbox` with some of the normal Vboxmanage operations (create, remove, list details) available as methods. Subcommands are included as properties with their own methods. This keeps things as clean and readable as possible while retaining it's syntactic similarity to the real Vagrant CLI.

|CLI|Cake|
|---|----|
|`vboxmanage createvm`|`Virtualbox.CreateVm()`|
|`vboxmanage unregistervm`|`Virtualbox.RemoveVm()`|
|`vboxmanage list vms`|`Virtualbox.Vms`|

## Settings

Command arguments are generally included as an `Action` object, so you can use the familiar lambda syntax from the `MSBuild` etc aliases. So a more complex example might be:

```
Virtualbox.CreateVm(settings =>
             settings.Name = "TestVm"));
```
which is equivalent to:
```
vboxmanage createvm blah blah
```

Each settings class has full documentation as well as extension methods for complete control with the fluent API.

## Directory switching

To run Virtualbox commands in a directory other than the current one, use the `FromPath()` method chain to switch directory:

```
Virtualbox.FromPath("./path/to/directory").CreateVm();
Virtualbox.FromPath("./path/to/other/dir").RemoveVm();
```

This method will switch directory only for the current command, and will not affect subsequent commands.