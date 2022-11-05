using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Utils.Editor;
using Debug = UnityEngine.Debug;

namespace GitTeam.Editor
{
    public static class GitTeamMenus
    {
        private static List<string> _outputs;

        private static string GitRoot => GitTeamConfig.Instance.GitProjectRoot;

        private static void BeginOutputsLogging()
        {
            if (_outputs == null)
            {
                _outputs = new List<string>();
            }
            
            _outputs.Clear();
        }

        private static void Log(string log)
        {
            _outputs.Add(log);
        }

        private static string GetLog()
        {
            string log = String.Join("\n", _outputs);
            return log;
        }

        private static void EndOutputsLogging()
        {
            var bigLog = GetLog();
            Debug.Log(bigLog);
        }

        [MenuItem("Tools/Facticus/GitTeam/Pull")]
        public static void PullMenu()
        {
            BeginOutputsLogging();
            Log("--- PULL ---");
            Log("");
            try
            {
                bool commitSuccess = CommitCurrentChanges(out var userData);
                if (commitSuccess)
                {
                    var defaultBranch = GitTeamConfig.Instance.DefaultBranch;
                    var userBranch = userData.DefaultBranch;
                    // switch to project's default branch
                    Log(GitUtils.Switch(defaultBranch, GitRoot));

                    // pull
                    Log(GitUtils.Pull(GitRoot));

                    // switch back to the user branch
                    Log(GitUtils.Switch(userBranch, GitRoot));

                    // merge
                    bool success = Merge(userBranch, defaultBranch);
                    if (!success)
                    {
                        var title = "Merge conflicts";
                        var message =
                            $"Can't pull. Conflicts would happen if {defaultBranch} merges into {userBranch}. " +
                            "Please notify it to someone in your team that could fix this.";
                        ShowErrorMessage(title, message);
                    }
                    else
                    {
                        ShowSuccessMessage("Success", "Pull successful!");
                    }
                }
            }
            catch (Exception e)
            {
                ShowErrorMessage("Error!", e.Message);
            }
            finally
            {
                Log("");
                EndOutputsLogging();
            }
        }

        [MenuItem("Tools/Facticus/GitTeam/Push")]
        public static void PushMenu()
        {
            BeginOutputsLogging();
            Log("--- PUSH ---");
            Log("");
            try
            {
                bool commitSuccess = CommitCurrentChanges(out var userData);
                if (commitSuccess)
                {
                    var pushOutput =
                        GitUtils.RunGitCommandMergeOutputs($"push -u origin {userData.DefaultBranch}", GitRoot);
                    Log(pushOutput);
                }

                ShowSuccessMessage("Success", "Push successful!");
            }
            catch (Exception e)
            {
                ShowErrorMessage("Error!", e.Message);
            }
            finally
            {
                Log("");
                EndOutputsLogging();
            }
        }

        private static void ShowErrorMessage(string title, string message)
        {
            EditorInputDialog.Show(
                title,
                message,
                new List<(string, Action<ScriptableObject>)>
                {
                    ("Ok", null),
                    ("Copy log", _ => EditorGUIUtility.systemCopyBuffer = $"{message}\n{GetLog()}")
                }
            );
        }
        
        private static void ShowSuccessMessage(string title, string message)
        {
            EditorInputDialog.ShowMessage(
                title,
                message
            );
        }

        private static bool Merge(string current, string toMerge)
        {
            // check conflicts
            bool existConflicts = false;
            try
            {
                GitUtils.RunGitCommandMergeOutputs($"merge --no-commit --no-ff {toMerge}", GitRoot);
            }
            catch
            {
                existConflicts = true;
            }
            finally
            {
                GitUtils.RunGitCommandMergeOutputs($"merge --abort", GitRoot);
            }

            if (!existConflicts)
            {
                var mergeMessage = $"Automatic merge: {toMerge} -> {current} from GitTeam";
                var mergeOutput = GitUtils.RunGitCommandMergeOutputs(
                    $"merge {toMerge} --no-edit -m \"{mergeMessage}\"", GitRoot);
                Log(mergeOutput);
            }

            bool success = !existConflicts;
            return success;
        }

        private static bool CommitCurrentChanges(out UserData userData)
        {
            Log("--- CommitCurrentChanges ---");
            
            Log("Saving assets before commit...");
            AssetDatabase.SaveAssets();
            
            // get user
            var userName = GitUtils.GetUserName();
            var existsUser = GitTeamConfig.Instance.TryFindByName(userName, out userData);

            if (!existsUser)
            {
                Log($"User {userName} does not exist in GitTeam settings.");
                return false;
            }
            
            // check if there are changes
            bool existChanges = ThereAreAnyChangeInPaths(userData.WorkPaths);
            if (!existChanges)
            {
                Log("Not commiting because there are no changes.");
                return true;
            }
            
            // get commit message
            var message = GetCommitMessage();
            if (message == null)
            {
                Log("Cancelling because a commit message wasn't provided.");
                return false;
            }
            
            // get user branch
            var branch = userData.DefaultBranch;

            // switch to or create that branch
            CreateOrSwitchToBranch(branch);
            
            // git add all user's paths
            AddAllWorkPaths(userData);
            
            // git commit
            Log("--- COMMIT changes ---");
            var commitOutput = GitUtils.Commit(message, GitRoot);
            Log(commitOutput);
            
            // restore everything else
            Log("--- RESTORE everything else ---");
            var restoreOutput = GitUtils.Restore(".", GitRoot);
            Log(restoreOutput);

            return true;
        }

        private static void AddAllWorkPaths(UserData userData)
        {
            Log("--- AddAllWorkPaths ---");
            foreach (var workPath in userData.WorkPaths)
            {
                var relativeWorkPath = MakePathRelativeToRoot(workPath);
                var addOutput = GitUtils.Add(relativeWorkPath, GitRoot);
                Log(addOutput);
            }
        }

        private static string MakePathRelativeToRoot(string workPath)
        {
            if (GitRoot == "")
            {
                return workPath;
            }
            return workPath.Replace(GitRoot, "");
        }

        private static bool ThereAreAnyChangeInPaths(List<string> paths)
        {
            try
            {
                // this is needed in some cases to allow "diff-index" command bellow to work properly
                GitUtils.RunGitCommandMergeOutputs("update-index --refresh", GitRoot);
            }
            catch
            {
                // ignored
            }

            foreach (var path in paths)
            {
                var relativePath = MakePathRelativeToRoot(path);
                relativePath = relativePath.Trim('\\', '/');
                var output = GitUtils.RunGitCommandMergeOutputs($"diff-index HEAD -- {relativePath}", GitRoot);
                output = output.Trim('\n');
                if (!String.IsNullOrEmpty(output))
                {
                    // there is a change
                    return true;
                }
            }
            
            return false;
        }

        private static void CreateOrSwitchToBranch(string branch)
        {
            Log("--- CreateOrSwitchToBranch ---");
            bool alreadyOnBranch = GitUtils.GetCurrentBranch(GitRoot) == branch;
            if (alreadyOnBranch)
            {
                Log($"Already on branch {branch}");
            }
            else
            {
                bool exists = GitUtils.ExistsBranch(branch, GitRoot);
                if (exists)
                {
                    var output = GitUtils.Switch(branch, GitRoot);
                    Log($"Switch output: {output}");
                }
                else
                {
                    var output = GitUtils.Switch($"-c {branch}", GitRoot); // create
                    Log($"Switch output: {output}");
                }
            }
        }
        

        private static string GetCommitMessage()
        {
            string message = null;
            
            EditorInputDialog.Show<StringContainer>(
                "Commit message",
                "Enter a commit message for the current changes",
                msg => message = msg.Value,
                modal: true
            );

            message = message == "" ? "---" : message;
            
            return message;
        }
    }
}