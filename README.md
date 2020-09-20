# dnf

[![Actions Status](https://github.com/areller/dnf/workflows/ci/badge.svg)](https://github.com/areller/dnf/actions)

## dnf
Use the `dnf` CLI tool run .NET Framework projects.  

`dnf` requires Visual Studio and MSBuild to be installed on the machine, since it uses MSBuild to build the given project.

### Installation

Run

```
dotnet tool install --global dnf
```

to install the latest version of `dnf`, or run

```
dotnet tool install --global dnf --version VERSION_NUMBER
```

to install a specific version

### Usage

To run `dnf`, from a command line, type

```
dnf RELATIVE_OR_ABSOLUTE_PATH_TO_YOUR_PROJECT_DIRECTORY
```

For example,

```
dnf .\MySolution\MyProject
```

or

```
dnf C:\Projects\MySolution\MyProject
```

The project directory is the directory that contains the `.csproj` file.

In some cases, the build process of your project might depend on the solution directory. In that case, your can specify the relative or absolute path of the solution in the second argument.

For example,

```
dnf .\MySolution\MyProject .\MySolution
```

You can also pass arguments to your project, using

```
dnf .\MySolution\MyProject -- arguments go here
```

### Restart after Build

By default, `dnf` will detect if you project was rebuilt/built and will restart the project in such case.

To disable that functionality, run `dnf` with the `--no-restart` flag.

For example,

```
dnf .\MySolution\MyProject .\MySolution --no-restart -- arguments go here
```