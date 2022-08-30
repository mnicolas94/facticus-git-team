using System;
using System.Collections.Generic;
using UnityEditor;
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

        private static void EndOutputsLogging()
        {
            string bigLog = String.Join("\n", _outputs);
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
                Pull(out _);
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
                bool commitSuccess = Pull(out var userData);
                if (commitSuccess)
                {
                    var pushOutput = GitUtils.RunGitCommandMergeOutputs($"push -u origin {userData.DefaultBranch}", GitRoot);
                    Log(pushOutput);
                }
            }
            finally
            {
                Log("");
                EndOutputsLogging();
            }
        }
        
        private static bool Pull(out UserData userData)
        {
            bool commitSuccess = CommitCurrentChanges(out userData);
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
                var mergeMessage = $"Automatic merge: {defaultBranch} -> {userBranch} from GitTeam";
                var mergeOutput = GitUtils.RunGitCommandMergeOutputs(
                    $"merge {defaultBranch} --no-edit -m \"{mergeMessage}\"", GitRoot);
                Log(mergeOutput);
            }

            return commitSuccess;
        }

        private static bool CommitCurrentChanges(out UserData userData)
        {
            Log("--- CommitCurrentChanges ---");
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
                var fixedWorkPath = workPath.Replace(GitRoot, "");
                var addOutput = GitUtils.Add(fixedWorkPath, GitRoot);
                Log(addOutput);
            }
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
                var fixedPath = path.Replace(GitRoot, "");
                fixedPath = fixedPath.Trim('\\', '/');
                var output = GitUtils.RunGitCommandMergeOutputs($"diff-index HEAD -- {fixedPath}", GitRoot);
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
            try
            {
                var output = GitUtils.Switch(branch, GitRoot);
                Log($"Switch output: {output}");
            }
            catch
            {
                var output = GitUtils.Switch($"-c {branch}", GitRoot); // create
                Log($"Switch output: {output}");
            }
        }

        private static string GetCommitMessage()
        {
            var message = EditorInputDialog.Show(
                "Commit message",
                "Enter a commit message for the current changes",
                ""
            );
            return message;
        }
    }
}