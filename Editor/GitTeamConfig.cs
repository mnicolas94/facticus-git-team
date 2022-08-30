using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace GitTeam.Editor
{
    [CreateAssetMenu(fileName = "GitTeamConfig", menuName = "Facticus/GitTeam/GitTeamConfig")]
    public class GitTeamConfig : ScriptableObjectSingleton<GitTeamConfig>
    {
        [SerializeField] private string _gitProjectRoot;
        [SerializeField] private List<UserData> _usersData;

        public string GitProjectRoot => _gitProjectRoot;

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
        [SerializeField] private List<string> _workPaths;

        public string UserName => _userName;

        public string DefaultBranch => _defaultBranch;

        public List<string> WorkPaths => _workPaths;

        public void Deconstruct(out string userName, out string defaultBranch, out List<string> mutablePaths)
        {
            userName = _userName;
            defaultBranch = _defaultBranch;
            mutablePaths = _workPaths;
        }
    }
}