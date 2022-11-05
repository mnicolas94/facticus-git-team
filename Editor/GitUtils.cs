﻿using System;
using System.Diagnostics;
 using System.IO;
 using System.Linq;
 using Debug = UnityEngine.Debug;

namespace GitTeam.Editor
{
    public static class GitUtils
    {
        public static string RunGitCommandMergeOutputs(string gitCommand, string workingDir)
        {
            var (output, errorOutput) = RunGitCommand(gitCommand, workingDir);
            var allOutput = $"{output}\n{errorOutput}";
            return allOutput;
        }
        
        public static (string, string) RunGitCommand(string gitCommand)
        {
            return RunGitCommand(gitCommand, "");
        }

        public static (string, string) RunGitCommand(string gitCommand, string workingDir) {
            // Strings that will catch the output from our process.
            string output = "no-git";
            string errorOutput = "no-git";

            // Set up our processInfo to run the git command and log to output and errorOutput.
            ProcessStartInfo processInfo = new ProcessStartInfo("git", @gitCommand) {
                WorkingDirectory = workingDir,
                CreateNoWindow = true,          // We want no visible pop-ups
                UseShellExecute = false,        // Allows us to redirect input, output and error streams
                RedirectStandardOutput = true,  // Allows us to read the output stream
                RedirectStandardError = true    // Allows us to read the error stream
            };

            // Set up the Process
            Process process = new Process {
                StartInfo = processInfo
            };
            try {
                process.Start();  // Try to start it, catching any exceptions if it fails
            } catch (Exception) {
                // For now just assume its failed cause it can't find git.
                Debug.LogError("Git is not set-up correctly, required to be on PATH, and to be a git project.");
                throw;
            }

            // Read the results back from the process so we can get the output and check for errors
            output = process.StandardOutput.ReadToEnd();
            errorOutput = process.StandardError.ReadToEnd();

            process.WaitForExit();  // Make sure we wait till the process has fully finished.
            int exitCode = process.ExitCode;
            bool hadErrors = process.ExitCode != 0;
            process.Close();        // Close the process ensuring it frees it resources.

            if (hadErrors)
            {
                Debug.Log($"Exit code: {exitCode}");    
                throw new Exception($"{output}\n{errorOutput}");
            }

            return (output, errorOutput);  // Return the output from git.
        }

        public static string Add(string whatToAdd, string gitRoot = "")
        {
            string gitCommand = $"add {whatToAdd}";
            return RunGitCommandMergeOutputs(gitCommand, gitRoot);
        }
        
        public static string Reset(string whatToReset, string gitRoot = "")
        {
            string gitCommand = $"reset {whatToReset}";
            return RunGitCommandMergeOutputs(gitCommand, gitRoot);
        }
        
        public static string Commit(string message, string gitRoot = "")
        {
            string gitCommand = $"commit -m \"{message}\"";
            try
            {
                var commitOutput = RunGitCommandMergeOutputs(gitCommand, gitRoot);
                return commitOutput;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("nothing to commit"))
                {
                    // it's not an error
                    return "nothing to commit";
                }
                else
                {
                    throw e;
                }
            }
        }
        
        public static string Push(string gitRoot = "")
        {
            string gitCommand = "push";
            return RunGitCommandMergeOutputs(gitCommand, gitRoot);
        }
        
        public static string Pull(string gitRoot = "")
        {
            string gitCommand = "pull";
            return RunGitCommandMergeOutputs(gitCommand, gitRoot);
        }
        
        public static string Restore(string whatToRestore, string gitRoot = "")
        {
            string gitCommand = $"restore {whatToRestore}";
            return RunGitCommandMergeOutputs(gitCommand, gitRoot);
        }

        public static bool ExistsBranch(string branch, string gitRoot = "")
        {
            string gitCommand = $"branch";
            var branchesString = RunGitCommandMergeOutputs(gitCommand, gitRoot);
            var branches = branchesString.Split('\n');
            var trimmed = branches.ToList().ConvertAll(b => b.Trim(' ', '*'));
            return trimmed.Contains(branch);
        }
        
        public static string GetCurrentBranch(string gitRoot = "")
        {
            string gitCommand = $"branch --show-current";
            return RunGitCommandMergeOutputs(gitCommand, gitRoot);
        }
        
        public static string Switch(string switchTo, string gitRoot = "")
        {
            string gitCommand = $"switch {switchTo}";
            return RunGitCommandMergeOutputs(gitCommand, gitRoot);
        }
        
        public static bool CanMerge(string toMerge, string gitRoot = "")
        {
            bool existConflicts = false;
            try
            {
                RunGitCommandMergeOutputs($"merge --no-commit --no-ff {toMerge}", gitRoot);
            }
            catch
            {
                existConflicts = true;
            }
            finally
            {
                var existsMergeInProcess = ExistsMergeInProcess(gitRoot);
                if (existsMergeInProcess)
                {
                    RunGitCommandMergeOutputs($"merge --abort", gitRoot);
                }
            }

            return existConflicts;
        }

        public static bool ExistsMergeInProcess(string gitRoot = "")
        {
            var mergeFile = Path.Combine(gitRoot, ".git", "MERGE_HEAD");
            bool existsMergeInProcess = File.Exists(mergeFile);
            return existsMergeInProcess;
        }
        
        public static string GetGitCommitHash(string gitRoot = "")
        {
            string gitCommand = "rev-parse --short HEAD";
            var stdout = RunGitCommandMergeOutputs(gitCommand, gitRoot);
            stdout = stdout.Trim();
            return stdout;
        }
        
        public static string GetLastTag(string gitRoot = "")
        {
            string gitCommand = "describe --tags --abbrev=0 --match v[0-9]*";
            var stdout = RunGitCommandMergeOutputs(gitCommand, gitRoot);
            stdout = stdout.Trim();
            return stdout;
        }

        public static string GetUserName(string gitRoot = "")
        {
            string gitCommand = "config --get user.name";
            var stdout = RunGitCommandMergeOutputs(gitCommand, gitRoot);
            stdout = stdout.Trim();
            return stdout;
        }
    }
}