module PostProcessNupkg
open System
open System.IO
open System.IO.Compression
open System.Text
open System.Xml
open System.Xml.Linq
open System.Xml.Schema
open Fake.Core
open Fake.IO
open FSharp.Data
open Paket
open Paket.Core
open Paket.Domain

// I would use the xsd schema, but it's not playing nice with the XmlProvider. So just use a sample for now.
[<Literal>]
let sample = """<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>FSharp.Logf</id>
    <version>1.0.0</version>
    <authors>FSharp.Logf</authors>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Printf-style logging for structured loggers.</description>
    <releaseNotes>• Initial release</releaseNotes>
    <copyright>Copyright (c) John Wostenberg 2022</copyright>
    <tags>f# fsharp logging log structured printf fable fable-library fable-dotnet fable-javascript</tags>
    <repository type="git" url="https://github.com/jwosty/FSharp.Logf" branch="master" commit="515b6270e985ce62856a232b3becc34ed259397a" />
    <dependencies>
      <group targetFramework=".NETStandard2.0">
        <dependency id="BlackFox.MasterOfFoo" version="1.0.6" exclude="Build,Analyzers" />
        <dependency id="FSharp.Core" version="7.0.0" exclude="Build,Analyzers" />
        <dependency id="Microsoft.Extensions.Logging" version="7.0.0" exclude="Build,Analyzers" />
      </group>
      <group targetFramework=".NETStandard2.1">
      </group>
    </dependencies>
  </metadata>
</package>"""
type NuspecFile = XmlProvider<sample>

let paketDepencencies = lazy(DependenciesFile.ReadFromFile "paket.dependencies")

// Reach into the XML and change the <dependency> version constraints
let changeVersionConstraints (getDependencyVersionConstraint: string -> string option) (nuspec: NuspecFile.Package) =
    for group in nuspec.Metadata.Dependencies.Groups do
        for dep in group.Dependencies do
            match getDependencyVersionConstraint dep.Id with
            | Some v ->
                let versionAttr = dep.XElement.Attribute(nameof(dep.Version).ToLower())
                versionAttr.Value <- v
            | None -> ()
    nuspec

let processNupkgFromStream (_: string) (stream: Stream) =
    use archive = new ZipArchive(stream, ZipArchiveMode.Update)
    let entry = archive.Entries |> Seq.find (fun e -> e.Name.EndsWith ".nuspec")
    let mainGroup = paketDepencencies.Value.Groups.[GroupName("Main")]
    use streamReader = new StreamReader(entry.Open(), Encoding.UTF8) in
        let nuspecFile = NuspecFile.Load(streamReader)
        let output =
            changeVersionConstraints
                (fun pkgId ->
                    let pkgName = PackageName(pkgId)
                    let p =
                        if pkgId = "FSharp.Logf" then None
                        else
                            try mainGroup.Packages |> List.find (fun p -> p.Name = pkgName) |> Some
                            with e ->
                                // If this error is throwing, it's because the package in question is a transitive
                                // dependency. Just add it as a direct dependency to make it work.
                                raise (Exception($"Package %s{pkgId} not found in dependency list. Please add it to paket.dependencies.", e))
                    p |> Option.map (fun p ->
                        match p.VersionRequirement.Range with
                        | VersionRange.Minimum v -> string v
                        | _ -> raise (NotImplementedException())))
                nuspecFile
    let entryStream = entry.Open()
    entryStream.Seek (0L, SeekOrigin.Begin) |> ignore
    entryStream.SetLength 0
    let nuspecWriter = new StreamWriter(entryStream, Encoding.UTF8)
    output.XElement.Save(nuspecWriter)

/// Cracks open a nupkg and changes all dependency versions to match those from paket.dependencies 
let processNupkgAtPath (path: string) =
    Trace.log ("Fixing nupkg dependency versions: " + path)
    try
        use file = File.Open (path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
        processNupkgFromStream path file
    with e ->
        raise (Exception($"Exception processing nupkg: %s{path}", e))
