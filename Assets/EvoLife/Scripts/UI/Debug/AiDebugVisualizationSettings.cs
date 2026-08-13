namespace EvoLife.UI
{
    /// <summary>
    /// Global AI overlay switches. Visualization is selected-creature only by default
    /// and does not change simulation behavior.
    /// </summary>
    public static class AiDebugVisualizationSettings
    {
        public static bool GlobalEnabled;

        public static bool SelectedCreatureOnly = true;

        public static void ToggleGlobal() => GlobalEnabled = !GlobalEnabled;

        public static bool ShouldDraw(bool isSelected)
        {
            if (!GlobalEnabled)
            {
                return false;
            }

            return !SelectedCreatureOnly || isSelected;
        }
    }
}
