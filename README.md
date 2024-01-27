# dotnet-bsp
Build Server for .NET C#. BSP is designed to allow your editor to communicate with different build tools to compile, run, test, debug your code and more. For more info see the [Official BSP specification](https://build-server-protocol.github.io/docs/specification).

This is currently in active development and is not stable. You are welcome to contibute and make PRs.

# Build

```sh
dotnet build && dotnet publish
```

# Setup for project
Build the project before you run the install script.

```sh
./install.sh ../my-sample-project/
```

# Credits
Thanks to the following projects that helped me to build this project.

- https://github.com/dotnet/roslyn Took some parts of the implmentation of LSP from here
