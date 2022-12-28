module Build
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.DotNet

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

let Clean _ =
    DotNet.exec id "clean" Projects.sln |> ignore
    // TODO: `dotnet fable clean`

let Restore _ =
    // Can't get Paket.restore to work because it fails with:
    //      An error occurred trying to start process 'paket' with working directory '.'. No such file or directory
    DotNet.exec id "paket" "restore" |> ignore
    DotNet.restore id Projects.sln

let BuildDotNet _ =
    DotNet.build (fun bo -> { bo with Configuration = buildCfg; NoRestore = true }) Projects.sln

let BuildFable _ =
    ()

let Build _ = ()

let TestDotNet _ =
    DotNet.test (fun ``to`` -> { ``to`` with NoRestore = true; NoBuild = true }) Projects.test

let TestFable _ = ()

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
