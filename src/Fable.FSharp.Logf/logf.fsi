/// <summary>
///     <a href="https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-printfmodule.html">Printf</a> style
///     formatting for <see cref="T:Microsoft.Extensions.Logging.ILogger"/> objects, with optional structured logging
///     support. 
/// </summary>
/// <remarks>
///     All functions in this module support format specifiers implemented by FSharp.Core printf functions. Named
///     parameters for structured loggers are specified in curly braces immediately after the format specifier. For
///     example, the format string "Hello, %s{user}!" would give the string argument "user" in a structured logger.
///     Parameter name specifiers are simply ignored by non-structured loggers.
/// </remarks>
/// <example>
///     <code>
///let greet logger person (miles: float) =
///    logf logger LogLevel.Information "Hello, %s{user}! You walked %.1f{distance} miles today." person miles
///greet logger "Jim" 1.7
///     </code>
///     <para>Output when using <a href="https://learn.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter">console logging</a>: <c>"Hello, Jim! You walked 1.7 miles today."</c></para>
///     <para>Output when using <a href="https://github.com/serilog/serilog-sinks-file#json-event-formatting">Serilog JSON file logging</a>: <c>{"@t":"2022-01-01T03:44:57.8532799Z","@mt":"Hello, {user}! You walked {distance} miles today.","user":"Jim","distance":"1.7"}</c></para>
/// </example>
#if DOTNET_LIB
module FSharp.Logf
#else
module Fable.FSharp.Logf
#endif
open FSharp.Core.Printf
open System
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions
#if DOTNET_LIB
open Microsoft.Extensions.Logging
#else
open Fable.Microsoft.Extensions.Logging
#endif

type StringFormat<'T, 'Result, 'Tuple> = Format<'T, unit, string, 'Result, 'Tuple>

/// <summary>
///     Formatted <see cref="T:Microsoft.Extensions.Logging.ILogger"/> compatible printing, using a given 'final'
///     function perform the log call and generate the result.
/// </summary>
/// <param name="continuation">The function called after formatting translation to generate the formatted log result.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val klogf : continuation: (string -> obj[] -> 'Result) -> format: StringFormat<'T, 'Result, 'Tuple> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at the specified
///     <see cref="T:Microsoft.Extensions.Logging.LogLevel"/>.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="logLevel">The LogLevel to use.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logf : logger: ILogger -> logLevel: LogLevel -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Like <see cref="logf" />, but with extra arguments for event id and exception.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="logLevel">The LogLevel to use.</param>
/// <param name="eventId">The event id associated with the log.</param>
/// <param name="exn">The exception to include in the message.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val vlogf : logger: ILogger -> logLevel: LogLevel -> eventId: EventId -> exn: Exception -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Formatted error printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at the specified
///     <see cref="T:Microsoft.Extensions.Logging.LogLevel"/> 
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="logLevel">The LogLevel to use.</param>
/// <param name="exn">The exception to include in the message.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val elogf : logger: ILogger -> logLevel: LogLevel -> exn: Exception -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Trace level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logft : logger: ILogger -> format: Format<'T, unit, string, unit, 'Tuple> -> 'T

/// <summary>
///     Like <see cref="logft" />, but with extra arguments for event id and exception.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="eventId">The event id associated with the log.</param>
/// <param name="exn">The exception to include in the message.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val vlogft : logger: ILogger -> eventId: EventId -> exn: Exception -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Debug level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logfd : logger: ILogger -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Like <see cref="logfd" />, but with extra arguments for event id and exception.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="eventId">The event id associated with the log.</param>
/// <param name="exn">The exception to include in the message.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val vlogfd : logger: ILogger -> eventId: EventId -> exn: Exception -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Information level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logfi : logger: ILogger -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Like <see cref="logfi" />, but with extra arguments for event id and exception.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="eventId">The event id associated with the log.</param>
/// <param name="exn">The exception to include in the message.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val vlogfi : logger: ILogger -> eventId: EventId -> exn: Exception -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Warning level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logfw : logger: ILogger -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Like <see cref="logfw" />, but with extra arguments for event id and exception.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="eventId">The event id associated with the log.</param>
/// <param name="exn">The exception to include in the message.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val vlogfw : logger: ILogger -> eventId: EventId -> exn: Exception -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Formatted error printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Warning level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="exn">The exception to log.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val elogfw : logger: ILogger -> exn: Exception -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Error level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logfe : logger: ILogger -> format: Format<'T, unit, string, unit, 'Tuple> -> 'T

/// <summary>
///     Like <see cref="logfe" />, but with extra arguments for event id and exception.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="eventId">The event id associated with the log.</param>
/// <param name="exn">The exception to include in the message.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val vlogfe : logger: ILogger -> eventId: EventId -> exn: Exception -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Like <see cref="logfe" />, but with extra arguments for event id and exception.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="exn">The exception to log.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val elogfe : logger: ILogger -> exn: Exception -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Critical level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logfc : logger: ILogger -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Like <see cref="logfc" />, but with extra arguments for event id and exception.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="eventId">The event id associated with the log.</param>
/// <param name="exn">The exception to include in the message.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val vlogfc : logger: ILogger -> eventId: EventId -> exn: Exception -> format: StringFormat<'T, unit, 'Tuple> -> 'T

/// <summary>
///     Formatted error printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Critical level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="exn">The exception to log.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val elogfc : logger: ILogger -> exn: Exception -> format: StringFormat<'T, unit, 'Tuple> -> 'T
