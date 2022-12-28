namespace Fable.Microsoft.Extensions.Logging
open System
open System.Runtime
open System.Runtime.InteropServices

type LogLevel = | Trace = 0 | Debug = 1 | Information = 2 | Warning = 3 | Error = 4 | Critical = 5 | None = 6
[<Struct>]
type EventId(id: int, name: string) =
    member _.Id = id
    member _.Name = name

// Fable proxy of Microsoft.Extensions.Logging.ILogger:
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger?view=dotnet-plat-ext-6.0
type ILogger =
    abstract BeginScope<'TState> : state:'TState -> IDisposable
    abstract IsEnabled : logLevel:LogLevel -> bool
    abstract Log<'TState> : logLevel:LogLevel * eventId:EventId * state:'TState * [<Optional; DefaultParameterValue(null:exn)>] ``exception``:exn * Func<'TState,exn,string> -> unit

// https://github.com/dotnet/runtime/blob/215b39abf947da7a40b0cb137eab4bceb24ad3e3/src/libraries/Microsoft.Extensions.Logging.Abstractions/src/LoggerExtensions.cs
// module LoggerExtensions =
//     type ILogger with
//         member this.LogDebug (eventId: EventId, ?``exception``: exn, ?message: string, [<ParamArray>] ?arg: obj[]) =
//             this.Log (LogLevel.Debug, eventId, ``exception``, message, args)
