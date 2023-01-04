# logf - printf-style functions for ILogger

[![CI (Ubuntu)](https://github.com/jwosty/FSharp.Logf/actions/workflows/ci-ubuntu.yml/badge.svg)](https://github.com/jwosty/FSharp.Logf/actions/workflows/ci-ubuntu.yml)

This library implements printf-style logging functions for any Microsoft.Extensions.Logging.ILogger, allowing you to log in an F# style with the full power of structured logging.
Here's an example, adding logging to the snippet from https://learn.microsoft.com/en-us/dotnet/fsharp/tutorials/async#combine-asynchronous-computations:

```fsharp
// Put this in a project and reference these packages: FSharp.Logf, Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Console
open System
open System.IO
open Microsoft.Extensions.Logging
open FSharp.Logf

// Type annotation would be inferred if omitted (included here for clarity)
let printTotalFileBytes (ml: ILogger) path =
    async {
        try
            let! bytes = File.ReadAllBytesAsync(path) |> Async.AwaitTask
            let fileName = Path.GetFileName(path)
            // Log at information level, with fileName and bytesLength as the parameter names for any logging sinks
            // supporting structured logging
            logfi ml "File %s{fileName} has %d{bytesLength} bytes" fileName bytes.Length
        with e -> 
            // Log at error level, setting an exception
            elogfe ml e "Exception accessing file: '%s{path}'" path
    }

[<EntryPoint>]
let main argv =
    // Create a Microsoft-provided logger. Choose your favorite Logger provider (for example: Serilog, NLog, log4net)
    let logger = LoggerFactory.Create(fun builder -> builder.AddConsole().SetMinimumLevel(LogLevel.Debug) |> ignore).CreateLogger()
    
    // Log at debug level. Since the NewLine argument doesn't have a parameter name right after it, it will be baked
    // directly into the string. The argv argument, however, will be parameterized like the others.
    logfd logger "ARGV:%s%s{argv}" Environment.NewLine ("[|" + (argv |> String.concat ";") + "|]")

    argv
    |> Seq.map (printTotalFileBytes logger)
    |> Async.Parallel
    |> Async.Ignore
    |> Async.RunSynchronously
    
    0
```

## Fable compatability

This library is Fable-compatible. You can take advantage of this like so:

```fsharp
#if FABLE_COMPILER
open Fable.Microsoft.Extensions.Logging
#else
open Microsoft.Extensions.Logging
#endif

let ml =
#if !FABLE_COMPILER
    LoggerFactory.Create(fun builder -> builder.AddConsole().SetMinimumLevel(LogLevel.Debug) |> ignore)
#else
    ConsoleLogger()
#endif

logfi ml "Hello, world"
```
