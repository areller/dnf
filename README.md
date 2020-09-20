# dnf

**Disclaimer: This tool is meant to be used for development scenarios. It's not advised to use it to host services/websites in a production environment**

- [dnf](#dnf)
- [dnf-iis](#dnf-iis)

[![Actions Status](https://github.com/areller/dnf/workflows/ci/badge.svg)](https://github.com/areller/dnf/actions)

## dnf

[![dnf nuget](https://img.shields.io/nuget/v/dnf.svg)](https://www.nuget.org/packages/dnf/)

Use the `dnf` CLI tool to run .NET Framework projects.  

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

### `--no-restart`

By default, `dnf` will detect if you project was rebuilt/built and will restart the project in such case.

To disable that functionality, run `dnf` with the `--no-restart` flag.

For example,

```
dnf .\MySolution\MyProject .\MySolution --no-restart -- arguments go here
```

## dnf-iis

[![dnf nuget](https://img.shields.io/nuget/v/dnf-iis.svg)](https://www.nuget.org/packages/dnf-iis/)

Use the `dnf-iis` CLI tool to run .NET Framework ASP.NET website on IIS.

The syntax is similar to that of `dnf`, but requires two additional options: `--name` and `--port`

For example,

```
dnf-iis .\Projects\MySolution\MyWebsite --name mywebiste --port 8080
```

Or if the build process depends on the solution path,

```
dnf-iis .\Projects\MySolution\MyWebsite .\Projects\MySolution --name mywebsite --port 8080
```

### `--no-build`

By default, `dnf-iis` will build the website's project before running it.

To disable that functionality, run `dnf-iis` with the `--no-build` flag.

For example,

```
dnf-iis .\Projects\MySolution\MyWebsite --name mywebsite --port 8080
```

### Tye Integration

You can run `dnf-iis` using [Project Tye](https://github.com/dotnet/tye)'s local orchestrator and `dnf-iis` will automatically the name and port. 

For example, 

```
...
services:
    ...
    - name: mywebsite
      executable: dnf-iis
      args: .\Projects\MySolution\MyWebsite
      bindings:
        - protocol: http
```