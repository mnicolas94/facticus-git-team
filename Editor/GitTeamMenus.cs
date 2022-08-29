using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Utils.Editor;
using Debug = UnityEngine.Debug;

namespace GitTeam.Editor
{
    public static class GitTeamMenus
    {
        [MenuItem("Tools/Facticus/GitTeam/Pull")]
        public static void Pull()
        {
            CommitCurrentChanges();
        }

        private static void CommitCurrentChanges()
        {
            // get user
            var userName = GitUtils.GetUserName();
            var exists = GitTeamConfig.Instance.TryFindByName(userName, out var userData);

            if (!exists)
            {
                // mostrar error
                Debug.Log($"User {userName} does not exists in GitTeam settings");
                return;
            }
            
            // check if there are changes
            bool existChanges = ThereAreAnyChangeInPaths(userData.CanChangePaths);
            Debug.Log($"existChanges: {existChanges}");
            
            // get commit message
            var message = GetCommitMessage();
            if (message == null)
            {
                return;
            }
            Debug.Log(message);
            
            // get user branch
            // switch to or create that branch
            // git add all user's paths
            // restore everything else
        }

        private static string GetCommitMessage()
        {
            var message = EditorInputDialog.Show(
                "Commit message",
                "Enter commit message for the current changes",
                ""
            );
            return message;
        }

        private static bool ThereAreAnyChangeInPaths(List<string> paths)
        {
            var gitRoot = GitTeamConfig.Instance.GitProjectRoot;
            
            GitUtils.RunGitCommand("update-index --refresh", gitRoot);

            foreach (var path in paths)
            {
                var fixedPath = path.Replace(gitRoot, "");
                fixedPath = fixedPath.Trim('\\', '/');
                var output = GitUtils.RunGitCommand($"diff-index HEAD {fixedPath}", gitRoot);
                if (!String.IsNullOrEmpty(output))
                {
                    // there is a change
                    return true;
                }
            }
            
            return false;
        }
    }
}