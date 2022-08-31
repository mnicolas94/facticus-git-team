using UnityEditor;
using UnityEngine;
using Utils.Editor.EditorGUIUtils;

namespace GitTeam.Editor
{
    public static class EditorSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider GetSettings()
        {
            bool existsSettings = GitTeamConfig.Instance != null;
            var so = existsSettings ? new SerializedObject(GitTeamConfig.Instance) : null;
            var keywords = existsSettings ? SettingsProvider.GetSearchKeywordsFromSerializedObject(so) : new string[0];
            var provider = new SettingsProvider("Project/Facticus/GitTeam", SettingsScope.Project)
            {
                guiHandler = searchContext =>
                {
                    EditorGUILayout.Space(12);
                    
                    if (existsSettings)
                        GUIUtils.DrawSerializedObject(so);
                    else
                    {
                        var r = EditorGUILayout.GetControlRect();
                        if (GUI.Button(r, "Create settings"))
                        {
                            var settings = ScriptableObject.CreateInstance<GitTeamConfig>();
                            AssetDatabase.CreateAsset(settings, "Assets/GitTeamConfig.asset");
                        }
                    }
                },
                
                keywords = keywords
            };

            return provider;
        }
    }
}