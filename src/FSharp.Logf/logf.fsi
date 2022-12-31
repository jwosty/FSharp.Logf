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
module FSharp.Logf
open System
open System.Text
open System.Text.RegularExpressions
#if FABLE_COMPILER
open Fable.Microsoft.Extensions.Logging
#else
open Microsoft.Extensions.Logging
#endif

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at the specified
///     <see cref="T:Microsoft.Extensions.Logging.LogLevel"/>.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="logLevel">The LogLevel to use.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logf : logger: ILogger -> logLevel: LogLevel -> format: Format<'T, unit, string, unit> -> 'T

/// <summary>
///     Formatted error printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at the specified
///     <see cref="T:Microsoft.Extensions.Logging.LogLevel"/>.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="logLevel">The LogLevel to use.</param>
/// <param name="exn">The exception to include in the message.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val elogf : logger: ILogger -> logLevel: LogLevel -> exn: Exception -> format: Format<'T, unit, string, unit> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Trace level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logft : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Debug level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logfd : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Information level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logfi : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Warning level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logfw : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T

/// <summary>
///     Formatted error printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Warning level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val elogfw : logger: ILogger -> exn: Exception -> format: Format<'T, unit, string, unit> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Error level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logfe : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T

/// <summary>
///     Formatted error printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Error level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val elogfe : logger: ILogger -> exn: Exception -> format: Format<'T, unit, string, unit> -> 'T

/// <summary>
///     Formatted printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Critical level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val logfc : logger: ILogger -> format: Format<'T, unit, string, unit> -> 'T

/// <summary>
///     Formatted error printing to an <see cref="T:Microsoft.Extensions.Logging.ILogger"/> at Critical level.
/// </summary>
/// <param name="logger">The logger to output to.</param>
/// <param name="format">The input formatter.</param>
/// <returns>The return type and arguments of the formatter.</returns>
val elogfc : logger: ILogger -> exn: Exception -> format: Format<'T, unit, string, unit> -> 'T
