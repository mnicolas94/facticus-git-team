using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

namespace GitTeam.Editor
{
    [CreateAssetMenu(fileName = "GitTeamConfig", menuName = "Facticus/GitTeam/GitTeamConfig")]
    public class GitTeamConfig : ScriptableObjectSingleton<GitTeamConfig>
    {
        [SerializeField] private string _gitProjectRoot;
        [SerializeField] private string _defaultBranch;
        [SerializeField] private List<UserData> _usersData;

        public string GitProjectRoot
        {
            get
            {
                var root = _gitProjectRoot.Replace("\\", "/");
                var pathIsFine = root == "" || root.EndsWith("/");
                return pathIsFine ? root : $"{root}/";
            }
        }

        public string DefaultBranch => _defaultBranch;

        public List<UserData> UsersData => _usersData;

        public bool TryFindByName(string userName, out UserData userData)
        {
            userData = _usersData.Find(user => user.UserName == userName);
            return userData != null;
        }
    }

    [Serializable]
    public class UserData
    {
        [SerializeField] private string _userName;
        [SerializeField] private string _defaultBranch;
        [FormerlySerializedAs("_workPaths")] [SerializeField] private List<string> _includePaths;
        [SerializeField] private List<string> _excludePaths;

        public string UserName => _userName;

        public string DefaultBranch => _defaultBranch;

        public List<string> IncludePaths => _includePaths;

        public List<string> ExcludePaths => _excludePaths;
        
        public void Deconstruct(out string userName, out string defaultBranch,
            out List<string> includePaths, out List<string> excludePaths)
        {
            userName = _userName;
            defaultBranch = _defaultBranch;
            includePaths = _includePaths;
            excludePaths = _excludePaths;
        }

        public void IncludePath(string path)
        {
            if (!_includePaths.Contains(path))
            {
                _includePaths.Add(path);
            }
        }
        
        public void ExcludePath(string path)
        {
            if (!_excludePaths.Contains(path))
            {
                _excludePaths.Add(path);
            }
        }
    }
}