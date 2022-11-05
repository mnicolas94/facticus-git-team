using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Utils.Editor;
using Debug = UnityEngine.Debug;

namespace GitTeam.Editor
{
    public static class GitTeamPathsMenus
    {
        [MenuItem("Assets/Facticus/GitTeam/Include", false, 1000)]
        public static void IncludeAsset()
        {
            IncludeOrExcludeAsset(
                user => user.IncludePaths,
                (user, path) => user.IncludePath(path),
                true
            );
        }
        
        [MenuItem("Assets/Facticus/GitTeam/Exclude", false, 1000)]
        public static void ExcludeAsset()
        {
            IncludeOrExcludeAsset(
                user => user.ExcludePaths,
                (user, path) => user.ExcludePath(path),
                true
                );
        }
        
        [MenuItem("Assets/Facticus/GitTeam/Remove include", false, 1000)]
        public static void RemoveIncludeAsset()
        {
            IncludeOrExcludeAsset(
                user => user.IncludePaths,
                (user, path) => user.RemoveIncludePath(path),
                false
            );
        }
        
        [MenuItem("Assets/Facticus/GitTeam/Remove exclude", false, 1000)]
        public static void RemoveExcludeAsset()
        {
            IncludeOrExcludeAsset(
                user => user.ExcludePaths,
                (user, path) => user.RemoveExcludePath(path),
                false
            );
        }
        
        [MenuItem("Assets/Facticus/GitTeam/Include", true, 1000)]
        public static bool IncludeAssetMenuValidation()
        {
            return IncludeOrExcludeAssetMenuValidation(user => user.IncludePaths, true);
        }
        
        [MenuItem("Assets/Facticus/GitTeam/Exclude", true, 1000)]
        public static bool ExcludeAssetMenuValidation()
        {
            return IncludeOrExcludeAssetMenuValidation(user => user.ExcludePaths, true);
        }
        
        [MenuItem("Assets/Facticus/GitTeam/Remove include", true, 1000)]
        public static bool RemoveIncludeAssetMenuValidation()
        {
            return IncludeOrExcludeAssetMenuValidation(user => user.IncludePaths, false);
        }
        
        [MenuItem("Assets/Facticus/GitTeam/Remove exclude", true, 1000)]
        public static bool RemoveExcludeAssetMenuValidation()
        {
            return IncludeOrExcludeAssetMenuValidation(user => user.ExcludePaths, false);
        }
        
        public static void IncludeOrExcludeAsset(
            Func<UserData, List<string>> getPathsFunction,
            Action<UserData, string> includeOrExcludeAction,
            bool wantToAdd
            )
        {
            var guids = Selection.assetGUIDs;
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList();

            // users that don't contain at least one of the paths if wantToAdd == true,
            // otherwise users that contain at least one of the paths
            var users = GitTeamConfig.Instance.UsersData
                .Where(user => paths.Any(path => getPathsFunction(user).Contains(path) ^ wantToAdd))
                .ToList();

            var genericMenu = new GenericMenu();
            foreach (var userData in users)
            {
                genericMenu.AddItem(
                    new GUIContent(userData.UserName),
                    false,
                    () =>
                    {
                        Undo.RecordObject(GitTeamConfig.Instance, "Change include or exclude settings");
                        foreach (var path in paths)
                        {
                            includeOrExcludeAction(userData, path);
                        }
                    });
            }
            genericMenu.AddSeparator("");
            genericMenu.AddItem(
                new GUIContent("All"),
                false,
                () =>
                {
                    Undo.RecordObject(GitTeamConfig.Instance, "Change include or exclude settings");
                    foreach (var userData in users)
                    {
                        foreach (var path in paths)
                        {
                            includeOrExcludeAction(userData, path);
                        }
                    }
                });

                var pos = GetMousePosition();
                genericMenu.DropDown(new Rect(pos.x, pos.y, 0, 0));
                genericMenu.ShowAsContext();
        }
        
        private static bool IncludeOrExcludeAssetMenuValidation(
            Func<UserData, List<string>> getPathsFunction,
            bool wantToAdd)
        {
            var guids = Selection.assetGUIDs;
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath);
            var anyNotIncluded = paths.Any(path => 
                GitTeamConfig.Instance.UsersData.Any(user => 
                    getPathsFunction(user).Contains(path) ^ wantToAdd));
            return anyNotIncluded;
        }

        private static Vector2 GetMousePosition()
        {
            var field = typeof ( Event ).GetField ( "s_Current", BindingFlags.Static | BindingFlags.NonPublic );
            if ( field != null )
            {
                Event current = field.GetValue ( null ) as Event;
                if ( current != null )
                {
                    return current.mousePosition;
                }
            }
            
            return Vector2.zero;
        }
    }
}