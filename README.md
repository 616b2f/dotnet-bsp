# dotnet-bsp
Build Server for .NET C#. BSP is designed to allow your editor to communicate with different build tools to compile, run, test, debug your code and more. For more info see the [Official BSP specification](https://build-server-protocol.github.io/docs/specification).

This is currently in active development and is not stable. You are welcome to contibute and make PRs.

Implemented server interface features:

- [x] BuildInitialize: request
- [x] OnBuildInitialized: notification
- [x] BuildShutdown: request
- [x] OnBuildExit: notification
- [x] WorkspaceBuildTargets: request
- [ ] WorkspaceReload: request
- [ ] BuildTargetSources: request
- [ ] BuildTargetInverseSources: request
- [ ] BuildTargetDependencySources: request
- [ ] BuildTargetDependencyModules: request
- [ ] BuildTargetResources: request
- [ ] BuildTargetOutputPaths: request
- [x] BuildTargetCompile: request
- [x] BuildTargetRun: request
- [x] BuildTargetTest: request
- [ ] DebugSessionStart: request
- [x] BuildTargetCleanCache: request
- [ ] OnRunReadStdin: notification

Client remote interface features:

- [x] OnBuildShowMessage: notification
- [x] OnBuildLogMessage: notification
- [x] OnBuildPublishDiagnostics: notification
- [x] OnBuildTargetDidChange: notification
- [x] OnBuildTaskStart: notification
- [x] OnBuildTaskProgress: notification
- [x] OnBuildTaskFinish: notification
- [x] OnRunPrintStdout: notification
- [x] OnRunPrintStderr: notification

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
