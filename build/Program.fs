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

module Folder =
    let src = "src"
    let test = "test"

type NupkgMetadata = {
    ``type``: string
    description: string option
    licenseExpression: string
    tags: string
    projectUrl: string
    repositoryUrl: string
    repositoryType: string
    repositoryCommit: string
    files: string list
}

let repoUrl = "https://github.com/jwosty/FSharp.Logf"
let repoSshUrl = "git@github.com:jwosty/FSharp.Logf.git"

let baseMetadata = lazy {
    ``type`` = "project"
    description = None
    licenseExpression = "MIT"
    tags = "f# fsharp logging log structured printf"
    repositoryType = "git"
    repositoryUrl = repoUrl
    repositoryCommit = Git.Information.getCurrentHash ()
    projectUrl = repoUrl
    files = []
}

type Project = { path: string; metadata: NupkgMetadata }

let mkProject path description additionalTags =
    {
        path = path
        metadata = {
            baseMetadata.Value with
              description = Some description
              tags = $"{baseMetadata.Value.tags} {additionalTags}"
    } }

// ensures that fsproj, fs, and fsi files are included in the output so that Fable will recognize the nupkg as
// Fable-compatible
let packFableFiles proj =
    { proj with
        metadata = {
            proj.metadata with
                files =
                    List.append (
                        ["*.fsproj";"**\*.fs"; "**\*.fsi"]
                        |> List.map (fun p -> $"%s{p} ==> fable%c{Path.DirectorySeparatorChar}")
                    )
                        ["!**\obj\**\*"]
    } }

module Projects =
    open Folder
    let sln = "FSharp.Logf.sln"
    let logfLib = mkProject (src </> "FSharp.Logf" </> "FSharp.Logf.fsproj") "Printf-style logging for structured loggers." "fable fable-library fable-javascript" |> packFableFiles
    let fableLogfLib = mkProject (src </> "Fable.FSharp.Logf" </> "Fable.FSharp.Logf.fsproj") "ConsoleLogger support for Fable FSharp.Logf." "fable fable-library fable-dotnet fable-javascript" |> packFableFiles
    let expectoAdapterLib = mkProject (src </> "FSharp.Logf.ExpectoAdapter" </> "FSharp.Logf.ExpectoAdapter.fsproj") "Expecto.Logging.ILogger -> Microsoft.Extensions.Logging.ILogger adapter" "expecto"
    let suaveAdapterLib = mkProject (src </> "FSharp.Logf.SuaveAdapter" </> "FSharp.Logf.SuaveAdapter.fsproj") "Microsoft.Extensions.Logging.ILogger -> Suave.Logging.ILogger adapter" "suave"
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

let GeneratePackageTemplates _ =
    Trace.log " -- Generating paket.template files --"
    for proj in Projects.allLibs do
        let templateFilePath = Path.GetDirectoryName proj.path </> "paket.template"
        Trace.logfn "  Writing: %s" templateFilePath
        let renderedTemplate =
            let fields = FSharp.Reflection.FSharpType.GetRecordFields (proj.metadata.GetType())
            [
                for field in fields do
                    let value =
                        match field.GetValue proj.metadata with
                        | :? string as sV -> Some sV
                        | :? option<string> as soV -> soV
                        | :? list<string> as [] -> None
                        | :? list<string> as slV ->
                            "" :: slV
                            |> String.concat (Environment.NewLine + "  ")
                            |> Some
                        | v -> failwithf $"Type not supported: %s{v.GetType().Name}"
                    match value with
                    | Some v -> yield (field.Name, v)
                    | None -> ()
            ]
            |> Seq.map (fun (k,v) -> $"%s{k} %s{v}")
        File.WriteAllLines (templateFilePath, renderedTemplate)

let Pack _ =
    Trace.log " -- Creating nuget packages --"
    
    DotNet.exec id "paket" $"pack ./artifacts --version {currentVersionInfo.versionName} --release-notes \"{currentVersionInfo.versionChanges}\"" |> ignore
    // for proj in [Projects.logfLib] do
        // Paket.pack (fun p -> { p with Version = currentVersionInfo.versionName; ReleaseNotes = currentVersionInfo.versionChanges })
    // for proj in [Projects.logfLib; (*Projects.fableLogfLib;*) Projects.expectoAdapterLib; Projects.suaveAdapterLib] do
    //     DotNet.pack (fun po -> { po with MSBuildParams = addVersionInfo currentVersionInfo po.MSBuildParams; Configuration = buildCfg; NoRestore = true; NoBuild = true }) proj

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
    Target.create (nameof GeneratePackageTemplates) GeneratePackageTemplates
    Target.create (nameof Pack) Pack
    Target.create (nameof BuildDocs) BuildDocs
    Target.create (nameof WatchDocs) WatchDocs
    Target.create (nameof ReleaseDocs) ReleaseDocs
    
    nameof Restore ==> nameof BuildDotNet ==> nameof TestDotNet
    nameof Restore ==> nameof BuildFable ==> nameof TestFable
    nameof Build <== [nameof BuildDotNet; nameof BuildFable]
    nameof Test <== [nameof TestDotNet; nameof TestFable]
    nameof Restore ==> nameof BuildDocs ==> nameof ReleaseDocs
    nameof BuildDotNet ==> nameof GeneratePackageTemplates ==> nameof Pack

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
