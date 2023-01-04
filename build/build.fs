module Build

open System
open System.IO
open System.Text.RegularExpressions
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.JavaScript
open Fake.Tools

let repoUrl = "https://github.com/jwosty/FSharp.Logf"
let repoSshUrl = "git@github.com:jwosty/FSharp.Logf.git"

module Folder =
    let src = "src"
    let test = "test"
    let artifacts = "artifacts"

module Projects =
    open Folder
    let sln = "FSharp.Logf.sln"
    let logfLib = (src </> "FSharp.Logf" </> "FSharp.Logf.fsproj")
    let fableLogfLib = (src </> "Fable.FSharp.Logf" </> "Fable.FSharp.Logf.fsproj")
    let expectoAdapterLib = (src </> "FSharp.Logf.ExpectoAdapter" </> "FSharp.Logf.ExpectoAdapter.fsproj")
    let suaveAdapterLib = (src </> "FSharp.Logf.SuaveAdapter" </> "FSharp.Logf.SuaveAdapter.fsproj")
    let allLibs = [logfLib; fableLogfLib; expectoAdapterLib; suaveAdapterLib]
    let test = test </> "FSharp.Logf.Tests" </> "FSharp.Logf.Tests.fsproj"

type PackageVersionInfo = { versionName: string; versionChanges: string }

let scrapeChangelog () =
    let changelog = File.ReadAllText "CHANGELOG.md"
    let regex = Regex("""## (?<Version>.*)\n+(?<Changes>(.|\n)*?)##""")
    let result = seq {
        for m in regex.Matches changelog ->
            {   versionName = m.Groups.["Version"].Value.Trim()
                versionChanges =
                    m.Groups.["Changes"].Value.Trim()
                        .Replace("    *", "    ◦")
                        .Replace("*", "•")
                        .Replace("    ", "\u00A0\u00A0\u00A0\u00A0") }
    }
    result

let changelog = scrapeChangelog () |> Seq.toList

let currentVersionInfo =
    List.tryHead changelog
    |> Option.defaultWith (fun () -> failwithf "Version info not found!")
Trace.logfn "currentVersionInfo: %O" currentVersionInfo

let addProperties props (defaults) =
    { defaults with MSBuild.CliArguments.Properties = [yield! defaults.Properties; yield! props]}

let addVersionInfo (versionInfo: PackageVersionInfo) options =
    let versionPrefix, versionSuffix =
        match String.splitStr "-" versionInfo.versionName with
        | [hd] -> hd, None
        | hd::tl -> hd, Some (String.Join ("-", tl))
        | [] -> failwith "Version name is missing"
    addProperties [
        "VersionPrefix", versionPrefix
        match versionSuffix with Some versionSuffix -> "VersionSuffix", versionSuffix | _ -> ()
        "PackageReleaseNotes", versionInfo.versionChanges
    ] options

let buildCfg = DotNet.BuildConfiguration.fromEnvironVarOrDefault "CONFIGURATION" DotNet.BuildConfiguration.Release
let nugetPushSource = Environment.environVarOrDefault "NUGET_PUSH_SOURCE" "https://api.nuget.org/v3/index.json"
let nugetPushApiKey = Environment.environVarOrNone "NUGET_PUSH_API_KEY"
    
let Clean (args: TargetParameter) =
    let isDryRun = args.Context.Arguments |> List.contains "--dry-run"
    Trace.logfn " -- Cleaning%s --" (if isDryRun then " (DRY RUN)" else "")
    let deleteDir =
        if isDryRun then (fun p -> Trace.logfn "Would remove: %s" p)
        else (fun p -> Trace.logfn "Removing: %s" p; Directory.delete p)
    // if not isDryRun then DotNet.exec (fun o -> ) "clean" Projects.sln |> ignore
    let outputDirs =
        !!("**" </> "bin")
        ++("**" </> "obj")
        ++"packages"
        --("build" </> "**")
    outputDirs
    |> Seq.iter deleteDir
    if isDryRun then
        Trace.logfn "Would execute: dotnet fable clean --yes"
    else
        DotNet.exec id "fable" "clean --yes" |> ignore
        
    deleteDir ".fsdocs"
    deleteDir Folder.artifacts

let Restore _ =
    Trace.log " -- Restoring --"
    // Can't get Paket.restore to work because it fails with:
    //      An error occurred trying to start process 'paket' with working directory '.'. No such file or directory
    // So instead just use this way to invoke it
    DotNet.exec id "paket" "restore" |> ignore
    DotNet.restore id Projects.sln

let BuildDotNet _ =
    Trace.log " -- Building dotnet projects --"
    DotNet.build (fun bo -> { bo with MSBuildParams = addVersionInfo currentVersionInfo bo.MSBuildParams; Configuration = buildCfg; NoRestore = true }) Projects.sln

let BuildFable _ =
    Trace.log " -- Building Fable projects --"
    ()

let Build _ = ()

let TestDotNet _ =
    Trace.log " -- Running dotnet tests --"
    DotNet.test (fun ``to`` -> { ``to`` with NoRestore = true; NoBuild = true; Configuration = buildCfg }) Projects.sln

let TestFable _ =
    Trace.log " -- Running Fable tests --"
    Yarn.exec "test" id

let Test _ = ()

let Pack _ =
    Trace.log " -- Creating nuget packages --"
    
    // TODO: would be nice to depend on the Build target and avoid a rebuild (so that CI can test and pack without
    // building twice), but for some reason Fable.FSharp.Logf.fsproj blows up when you try to build it after build with
    // NoBuild set. This is a minor issue.
    DotNet.pack (fun po -> { po with MSBuildParams = addVersionInfo currentVersionInfo po.MSBuildParams; Configuration = buildCfg; NoRestore = true; OutputPath = Some Folder.artifacts }) Projects.sln
    // let anp = "artifacts-nopost"
    // Shell.cp_r Folder.artifacts anp
    for nupkg in !!(Folder.artifacts </> "*.nupkg") ++(Folder.artifacts </> "*.snupkg") do
        PostProcessNupkg.processNupkgAtPath nupkg
    
    // for pkg in !!(Folder.artifacts </> "*.nupkg") ++(Folder.artifacts </> "*.snupkg") ++(anp </> "*.nupkg") ++(anp </> "*.snupkg") do
    //     let dst = Path.GetDirectoryName pkg </> Path.GetFileNameWithoutExtension pkg + "." + Path.GetExtension pkg
    //     Trace.logfn $"Expanding %s{pkg} to %s{dst}"
    //     Directory.delete dst
    //     System.IO.Compression.ZipFile.ExtractToDirectory(pkg, dst)

let PublishNugetPackages _ =
    Trace.log " -- Publishing NuGet packages --"
    // Trace.logfn $"NUGET_SOURCE_URL = %s{nugetPushSource}"
    
    for pkg in !!(Folder.artifacts </> "*.nupkg") ++(Folder.artifacts </> "*.snupkg") do
        Trace.logfn $"Pushing %s{pkg} to %s{nugetPushSource}"
        DotNet.nugetPush (fun npo -> { npo with PushParams = { npo.PushParams with Source = Some nugetPushSource; ApiKey = nugetPushApiKey } }) pkg
        
    
    ()

let BuildDocs _ =
    // Can't use .NET 6.0.4xx due to: https://github.com/fsprojects/FSharp.Formatting/issues/616
    // And can't use .NET 7 due to: https://github.com/fable-compiler/Fable/issues/3294
    // So, in global.json, force .NET 6.0.3xx
    Trace.log " --- Building documentation --- "
    if buildCfg <> DotNet.BuildConfiguration.Release then
        failwithf "Build configuration must be set to 'Release' when building docs. Try again with `CONFIGURATION=Release ./build.sh -t BuildDocs`"
    DotNet.build (fun bo -> { bo with Configuration = buildCfg; NoRestore = true; MSBuildParams = { bo.MSBuildParams with Properties = ("WORKAROUND_MISSING_DOCS", "True") :: bo.MSBuildParams.Properties } }) Projects.sln
    let result = DotNet.exec id "fsdocs" ("build --clean --properties Configuration=Release WORKAROUND_MISSING_DOCS=true")
    Trace.logfn "%s" (result.ToString())
    
let WatchDocs _ =
    Trace.log " --- Building and watching documentation --- "
    if buildCfg <> DotNet.BuildConfiguration.Release then
        failwithf "Build configuration must be set to 'Release' when building docs. Try again with `CONFIGURATION=Release ./build.sh -t WatchDocs`"
    let result = DotNet.exec id "fsdocs" ("watch --clean --properties Configuration=Release WORKAROUND_MISSING_DOCS=true")
    Trace.logfn "%s" (result.ToString())

let ReleaseDocs _ =
    Trace.log "--- Releasing documentation --- "
    Git.CommandHelper.runSimpleGitCommand "." (sprintf "clone %s tmp/gh-pages --depth 1 -b gh-pages" repoSshUrl) |> ignore
    Shell.copyRecursive "output" "tmp/gh-pages" true |> printfn "%A"
    Git.CommandHelper.runSimpleGitCommand "tmp/gh-pages" "add ." |> printfn "%s"
    let commit = Git.Information.getCurrentHash ()
    Git.CommandHelper.runSimpleGitCommand "tmp/gh-pages"
        (sprintf """commit -a -m "Update generated docs from %s" """ commit)
    |> printfn "%s"
    Git.Branches.pushBranch "tmp/gh-pages" "origin" "gh-pages"

open Fake.Core.TargetOperators

// FS0020: The result of this expression has type 'string' and is explicitly ignored. ...
#nowarn "0020"

let initTargets () =
    Target.create (nameof Clean) Clean
    Target.create (nameof Restore) Restore
    Target.create (nameof BuildDotNet) BuildDotNet
    Target.create (nameof BuildFable) BuildFable
    Target.create (nameof Build) Build
    Target.create (nameof TestDotNet) TestDotNet
    Target.create (nameof TestFable) TestFable
    Target.create (nameof Test) Test
    Target.create (nameof Pack) Pack
    Target.create (nameof PublishNugetPackages) PublishNugetPackages
    Target.create (nameof BuildDocs) BuildDocs
    Target.create (nameof WatchDocs) WatchDocs
    Target.create (nameof ReleaseDocs) ReleaseDocs
    
    nameof Restore ==> nameof BuildDotNet ==> nameof TestDotNet
    nameof Restore ==> nameof BuildFable ==> nameof TestFable
    nameof Build <== [nameof BuildDotNet; nameof BuildFable]
    nameof Test <== [nameof TestDotNet; nameof TestFable]
    nameof Restore ==> nameof BuildDocs ==> nameof ReleaseDocs
    nameof Restore ==> nameof Pack ==> nameof PublishNugetPackages

[<EntryPoint>]
let main argv =
    Trace.logfn "CurrentDirectory: %s" System.Environment.CurrentDirectory
    
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext
    initTargets ()
    Target.runOrDefaultWithArguments "Build"

    0
