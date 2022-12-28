module FSharp.Logf
open System
open System.Text
open System.Text.RegularExpressions
#if FABLE_COMPILER
open Fable.Microsoft.Extensions.Logging
#else
open Microsoft.Extensions.Logging
#endif

val logf : logger: ILogger -> logLevel: LogLevel -> format: Format<'T, unit, string, unit> -> 'T
val elogf : logger: ILogger -> logLevel: LogLevel -> exn: Exception -> format: Format<'T, unit, string, unit> -> 'T
val logft : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T
val logfd : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T
val logfi : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T
val logfw : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T
val elogfw : logger: ILogger -> exn: Exception -> format: Format<'T, unit, string, unit> -> 'T
val logfe : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T
val elogfe : logger: ILogger -> exn: Exception -> format: Format<'T, unit, string, unit> -> 'T
val logfc : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T
val elogfc : logger: ILogger -> exn: Exception -> format: Format<'T, unit, string, unit> -> 'T
val logfn : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T
