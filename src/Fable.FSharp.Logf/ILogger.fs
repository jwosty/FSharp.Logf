namespace Fable.Microsoft.Extensions.Logging
open System
open System.Runtime
open System.Runtime.InteropServices

/// <summary>
///     Fable proxy of <see cref="T:Microsoft.Extensions.Logging.LogLevel" />.
/// </summary>
type LogLevel = | Trace = 0 | Debug = 1 | Information = 2 | Warning = 3 | Error = 4 | Critical = 5 | None = 6
/// <summary>
///     Fable proxy of <see cref="T:Microsoft.Extensions.Logging.EventId" />.
/// </summary>
[<Struct>]
type EventId(id: int, name: string) =
    member _.Id = id
    member _.Name = name
    new(id) = EventId(id, null)

/// <summary>
///     Fable proxy of <see cref="T:Microsoft.Extensions.Logging.ILogger" />.
/// </summary>
type ILogger =
    abstract BeginScope<'TState> : state:'TState -> IDisposable
    abstract IsEnabled : logLevel:LogLevel -> bool
    abstract Log<'TState> : logLevel:LogLevel * eventId:EventId * state:'TState * ``exception``:exn * Func<'TState,exn,string> -> unit
