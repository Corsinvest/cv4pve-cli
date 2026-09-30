/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Extension.Shell;

namespace Corsinvest.ProxmoxVE.Cli;

/// <summary>
/// Semantic process exit codes, so scripts and CI can distinguish failure kinds.
/// </summary>
internal enum ExitCode
{
    /// <summary>Success.</summary>
    Ok = 0,

    /// <summary>Generic/unclassified error.</summary>
    Generic = 1,

    /// <summary>Authentication or configuration error (bad credentials, missing context).</summary>
    Auth = 2,

    /// <summary>Requested resource not found.</summary>
    NotFound = 3,

    /// <summary>API/server error (server unreachable, HTTP 5xx).</summary>
    Server = 4,

    /// <summary>Async task failed.</summary>
    TaskFailed = 5,

    /// <summary>Input validation error (bad argument, missing required value).</summary>
    Validation = 6,
}

/// <summary>
/// Error raised by cv4pve-cli itself, with the exit code it maps to.
/// </summary>
internal sealed class CliException(string message, ExitCode code) : Exception(message)
{
    /// <summary>Exit code of the error.</summary>
    public ExitCode Code { get; } = code;
}

/// <summary>
/// Helpers to report errors consistently: message to stderr, semantic exit code returned.
/// </summary>
internal static class ExitCodeHelper
{
    /// <summary>
    /// Print an error to stderr and return the given exit code as int.
    /// </summary>
    public static int Fail(string message, ExitCode code = ExitCode.Generic)
    {
        Console.Error.WriteLine($"Error: {message}");
        return (int)code;
    }

    /// <summary>
    /// Print an exception to stderr and return an exit code inferred from the exception.
    /// </summary>
    public static int Fail(Exception ex)
    {
        var code = Fail(ex.Message, Classify(ex));
        if (IsDebug) { Console.Error.WriteLine(ex); }
        return code;
    }

    /// <summary>
    /// Print the error answered by the Proxmox VE API to stderr and return the exit code of its HTTP status.
    /// </summary>
    /// <param name="statusCode">HTTP status of the answer.</param>
    /// <param name="errorText">Error returned by Proxmox VE, used to choose the exit code.</param>
    /// <param name="message">Full message to print.</param>
    public static int FailHttp(int statusCode, string errorText, string message)
    {
        Console.Error.Write($"Error: {message}");
        return (int)FromHttpStatus(statusCode, errorText);
    }

    /// <summary>
    /// Exit code of an HTTP status returned by the Proxmox VE API.
    /// </summary>
    public static ExitCode FromHttpStatus(int statusCode, string message)
        => statusCode switch
        {
            400 => ExitCode.Validation,
            401 or 403 => ExitCode.Auth,
            404 or 501 => ExitCode.NotFound,
            _ => Classify(new Exception(message)) is var code && code != ExitCode.Generic
                    ? code
                    : ExitCode.Server,
        };

    /// <summary>Exit code of an alias that could not be expanded.</summary>
    public static ExitCode FromCommandError(ApiCommandError error)
        => error == ApiCommandError.GuestNotFound ? ExitCode.NotFound : ExitCode.Validation;

    /// <summary>
    /// True when --debug or --log-level Debug/Trace is on the command line: exceptions are printed in full.
    /// </summary>
    public static bool IsDebug { get; set; }

    /// <summary>
    /// Best-effort mapping of an exception to a semantic exit code.
    /// </summary>
    public static ExitCode Classify(Exception ex)
    {
        if (ex is CliException cli) { return cli.Code; }

        var msg = ex.Message.ToLowerInvariant();

        if (ex is UnauthorizedAccessException
            || msg.Contains("401")
            || msg.Contains("403")
            || msg.Contains("unauthorized")
            || msg.Contains("forbidden")
            || msg.Contains("authentication")
            || msg.Contains("permission")) { return ExitCode.Auth; }

        if (ex is KeyNotFoundException
            || msg.Contains("404")
            || msg.Contains("not found")
            || msg.Contains("does not exist")) { return ExitCode.NotFound; }

        if (ex is ArgumentException
            || msg.Contains("400")
            || msg.Contains("invalid")
            || msg.Contains("required")) { return ExitCode.Validation; }

        if (ex is HttpRequestException
            || msg.Contains("500")
            || msg.Contains("502")
            || msg.Contains("503")
            || msg.Contains("connection")
            || msg.Contains("timeout")
            || msg.Contains("reachable")) { return ExitCode.Server; }

        return ExitCode.Generic;
    }
}
