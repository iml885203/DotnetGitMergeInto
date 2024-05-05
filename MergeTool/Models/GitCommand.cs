using System.Diagnostics;

namespace MergeTool.Models;

public static class GitCommand
{
    public static async Task<GitProcess> Run(params string?[] args)
    {
        var startInfo = new ProcessStartInfo("git", string.Join(" ", args))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync();


        var gitProcess = new GitProcess()
        {
            ExitCode = process.ExitCode,
            StandardOutput = await process.StandardOutput.ReadToEndAsync(),
            StandardError = await process.StandardError.ReadToEndAsync()
        };
        Console.WriteLine($"> git {string.Join(" ", args)}");
        Console.WriteLine(gitProcess.GetOutput());
        return gitProcess;
    }

    public static async Task CheckUncommitted()
    {
        var process = await Run("status", "--porcelain");
        var output = process.GetTrimStandardOutput();
        if (!string.IsNullOrEmpty(output))
            throw new GitCommandFailed("There are uncommitted changes in the current branch.");
    }

    public static async Task<string> Checkout(string targetBranch)
    {
        return (await Run("checkout", targetBranch)).GetOutput();
    }

    public static async Task<string> Fetch(string targetBranch)
    {
        var process = await Run("fetch", "origin", targetBranch);
        if (process.IsFailed())
            throw new GitCommandFailed($"Failed to fetch the '{targetBranch}' branch.");
        return process.GetOutput();
    }

    public static async Task<string> ResetHard(string targetBranch)
    {
        var process = await Run("reset", "--hard", $"origin/{targetBranch}");
        if (process.IsFailed())
            throw new GitCommandFailed($"Failed to reset the '{targetBranch}' branch.");
        return process.GetOutput();
    }

    public static async Task<string> Merge(string originalBranch, string targetBranch)
    {
        var process = await Run("merge", originalBranch);
        if (process.IsFailed())
        {
            await Run("merge", "--abort");
            if (process.GetTrimStandardOutput().Contains("CONFLICT"))
            {
                throw new GitCommandFailed($"Merge conflict detected for branch '{targetBranch}'.");
            }
            else
            {
                throw new GitCommandFailed($"Merge failed for branch '{targetBranch}'.");
            }
        }

        return process.GetOutput();
    }

    public static async Task<string> GetOriginalBranch()
    {
        var originBranchProcess = await Run("branch", "--show-current");
        return originBranchProcess.GetTrimStandardOutput();
    }

    public static async Task<string> Push(string targetBranch)
    {
        var process = await Run("push", "origin", targetBranch);
        if (process.IsFailed())
            throw new GitCommandFailed($"Failed to push the '{targetBranch}' branch.");

        return process.GetOutput();
    }
}