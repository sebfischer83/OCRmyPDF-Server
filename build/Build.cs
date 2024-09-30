using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Nuke.Common.Git;
using Serilog;
using Nuke.Common.Tools.DotNet;

class Build : NukeBuild
{

    [Solution(GenerateProjects = false)]
    readonly Solution Solution;

    [GitRepository] readonly GitRepository Repository;

    [GitVersion]
    readonly GitVersion GitVersion;

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            Log.Information("Commit = {Value}", Repository.Commit);
            Log.Information("Branch = {Value}", Repository.Branch);
            Log.Information("Tags = {Value}", Repository.Tags);

            Log.Information("main/master branch = {Value}", Repository.IsOnMainOrMasterBranch());
            Log.Information("release/* branch = {Value}", Repository.IsOnReleaseBranch());
            Log.Information("hotfix/* branch = {Value}", Repository.IsOnHotfixBranch());

            Log.Information("Https URL = {Value}", Repository.HttpsUrl);
            Log.Information("SSH URL = {Value}", Repository.SshUrl);
            Log.Information("GitVersion = {Value}", GitVersion.FullSemVer);
            DotNetRestore(s => s                                                                
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s                                                                  // DOTNET
               .SetProjectFile(Solution)                                                       // DOTNET
               .SetConfiguration(Configuration)                                                // DOTNET
               .SetAssemblyVersion(GitVersion.AssemblySemVer)                                  // DOTNET && GITVERSION
               .SetFileVersion(GitVersion.AssemblySemFileVer)                                  // DOTNET && GITVERSION
               .SetInformationalVersion(GitVersion.InformationalVersion)                       // DOTNET && GITVERSION
               .EnableNoRestore());

        });

}
