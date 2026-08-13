using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Species or role-specific scripted baseline configuration.
    /// Assign on a creature prefab / <see cref="CreatureBrain"/>; do not store genetic traits here.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ScriptedBaselineProfile",
        menuName = "EvoLife/AI/Scripted Baseline Profile")]
    public sealed class ScriptedBaselineProfile : ScriptableObject
    {
        [SerializeField] ScriptedBaselineSettings settings = new ScriptedBaselineSettings();

        public ScriptedBaselineSettings Settings => settings;
    }
}
