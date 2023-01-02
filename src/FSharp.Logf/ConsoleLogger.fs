namespace Fable.Microsoft.Extensions.Logging
open System
open Fable.Microsoft.Extensions.Logging


// NOTE: this type is actually defined in Fable.FSharp.Logf/ConsoleLogger.fs. Due to an fsdocs bug
// (https://github.com/fsprojects/FSharp.Formatting/issues/680), "fake" that type here to at least make the docs
// generate, albeit from the wrong project.

/// <summary>
///     A logging provider which outputs to the Javascript console object for Fable environments.
/// </summary>
type ConsoleLogger(?prependLevelToEntries: bool, ?logLevel: LogLevel) =
    member val LogLevel = Unchecked.defaultof<LogLevel>
    
    interface ILogger with
        member this.BeginScope state = raise (NotImplementedException())
        member this.IsEnabled l = raise (NotImplementedException())
        member this.Log (level, _, state, err, formatter) = raise (NotImplementedException())
    