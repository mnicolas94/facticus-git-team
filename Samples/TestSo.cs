using UnityEngine;

namespace GitTeam.Samples
{
    [CreateAssetMenu(fileName = "TestSo", menuName = "TestSo", order = 0)]
    public class TestSo : ScriptableObject
    {
        [SerializeField] private string _test;
    }
}