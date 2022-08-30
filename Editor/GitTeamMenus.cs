using System;
using System.Collections.Generic;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace GitTeam.Editor
{
    public static class GitTeamMenus
    {
        private static List<string> _outputs;

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
        public static void Pull()
        {
            BeginOutputsLogging();
            Log("--- PULL ---");
            Log("");
            try
            {
                bool success = CommitCurrentChanges();
            }
            finally
            {
                EndOutputsLogging();
                
            }
        }

        private static bool CommitCurrentChanges()
        {
            Log("--- CommitCurrentChanges ---");
            // get user
            var userName = GitUtils.GetUserName();
            var existsUser = GitTeamConfig.Instance.TryFindByName(userName, out var userData);

            if (!existsUser)
            {
                // mostrar error
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
            var gitRoot = GitTeamConfig.Instance.GitProjectRoot;
            var commitOutput = GitUtils.Commit(message, gitRoot);
            Log(commitOutput);
            
            // restore everything else
            Log("--- RESTORE everything else ---");
            var restoreOutput = GitUtils.Restore(".", gitRoot);
            Log(restoreOutput);

            return true;
        }

        private static void AddAllWorkPaths(UserData userData)
        {
            Log("--- AddAllWorkPaths ---");
            var gitRoot = GitTeamConfig.Instance.GitProjectRoot;
            foreach (var workPath in userData.WorkPaths)
            {
                var addOutput = GitUtils.Add(workPath, gitRoot);
                Log(addOutput);
            }
        }

        private static bool ThereAreAnyChangeInPaths(List<string> paths)
        {
            var gitRoot = GitTeamConfig.Instance.GitProjectRoot;
            
            GitUtils.RunGitCommandThrowException("update-index --refresh", gitRoot);

            foreach (var path in paths)
            {
                var fixedPath = path.Replace(gitRoot, "");
                fixedPath = fixedPath.Trim('\\', '/');
                var output = GitUtils.RunGitCommandThrowException($"diff-index HEAD {fixedPath}", gitRoot);
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
            var gitRoot = GitTeamConfig.Instance.GitProjectRoot;
            try
            {
                var output = GitUtils.Switch(branch, gitRoot);
                Log($"Switch output: {output}");
            }
            catch
            {
                var output = GitUtils.Switch($"-c {branch}", gitRoot); // create
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