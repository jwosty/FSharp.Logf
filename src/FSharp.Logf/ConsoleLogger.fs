namespace Fable.Microsoft.Extensions.Logging
open System
open Fable.Microsoft.Extensions.Logging
open Fable.Core.JS

/// <summary>
///     A logging provider which outputs to the Javascript console object for Fable environments.
/// </summary>
type ConsoleLogger(?prependLevelToEntries, ?logLevel: LogLevel) =
    let prependLevelToEntries = defaultArg prependLevelToEntries true
    member val LogLevel = defaultArg logLevel LogLevel.Debug
    
    interface ILogger with
        member this.BeginScope state = raise (NotImplementedException())
        member this.IsEnabled l = l >= this.LogLevel
        member this.Log (level, _, state, err, formatter) =
            if (this :> ILogger).IsEnabled level then
                let log m =
                    match level with
                    | LogLevel.None -> ()
                    | LogLevel.Critical | LogLevel.Error -> console.error m
                    | LogLevel.Warning -> console.warn m
                    | LogLevel.Information -> console.info m
                    | LogLevel.Trace -> console.debug (string m)
                    | _ -> console.log m
                let m =
                    if prependLevelToEntries then
                        sprintf "[%s] " (Enum.GetName(typeof<LogLevel>, level).ToUpper()) + formatter.Invoke (state, err)
                    else formatter.Invoke (state, err)
                log m
                if not (isNull err) then
                    // Fable doesn't support getting exception types at runtime
                    log ("Exception: " + err.Message)
    