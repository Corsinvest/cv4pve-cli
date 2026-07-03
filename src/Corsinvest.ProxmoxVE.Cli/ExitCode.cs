/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

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
    public static int Fail(Exception ex) => Fail(ex.Message, Classify(ex));

    /// <summary>
    /// Best-effort mapping of an exception to a semantic exit code.
    /// </summary>
    public static ExitCode Classify(Exception ex)
    {
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
            || msg.Contains("unreachable")) { return ExitCode.Server; }

        return ExitCode.Generic;
    }
}
