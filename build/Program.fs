module Build
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.JavaScript

module Folder =
    let src = "src"
    let test = "test"

module Projects =
    open Folder
    let sln = "FSharp.Logf.sln"
    let logfLib = src </> "FSharp.Logf.ExpectoAdapter" </> "FSharp.Logf.ExpectoAdapter.fsproj"
    let expectoAdapterLib = src </> "FSharp.Logf.ExpectoAdapter" </> "FSharp.Logf.ExpectoAdapter.fsproj"
    let suaveAdapterLib = src </> "FSharp.Logf.SuaveAdapter" </> "FSharp.Logf.SuaveAdapter.fsproj"
    let test = test </> "FSharp.Logf.Tests" </> "FSharp.Logf.Tests.fsproj"

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
    // |> Seq.map (fun x -> Trace.logfn "Removing: '%s'" x; x)
    |> Seq.iter deleteDir
    if isDryRun then
        Trace.logfn "Would execute: dotnet fable clean --yes"
    else
        DotNet.exec id "fable" "clean --yes" |> ignore

let Restore _ =
    Trace.log " -- Restoring --"
    // Can't get Paket.restore to work because it fails with:
    //      An error occurred trying to start process 'paket' with working directory '.'. No such file or directory
    DotNet.exec id "paket" "restore" |> ignore
    DotNet.restore id Projects.sln

let BuildDotNet _ =
    Trace.log " -- Building dotnet projects --"
    DotNet.build (fun bo -> { bo with Configuration = buildCfg; NoRestore = true }) Projects.sln

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
    ()

let Test _ = ()

let Pack _ = ()


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
    
    nameof Restore ==> nameof BuildDotNet ==> nameof TestDotNet
    nameof Restore ==> nameof BuildFable ==> nameof TestFable
    nameof Build <== [nameof BuildDotNet; nameof BuildFable]
    nameof Test <== [nameof TestDotNet; nameof TestFable]

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
