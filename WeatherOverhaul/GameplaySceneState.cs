namespace WeatherOverhaul
{
    internal static class GameplaySceneState
    {
        internal static bool IsMainMenuOrBootSceneActive()
        {
            try
            {
                return IsMainMenuOrBootSceneName(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
            catch
            {
                return true;
            }
        }

        internal static bool IsGameplaySceneActive()
        {
            try
            {
                return IsGameplaySceneName(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsGameplaySceneName(string sceneName)
        {
            return !IsSaveBoundarySceneName(sceneName);
        }

        internal static bool IsMainMenuOrBootSceneName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return true;
            if (sceneName.StartsWith("MainMenu", StringComparison.OrdinalIgnoreCase)) return true;
            if (sceneName.StartsWith("Boot", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        internal static bool IsSaveBoundarySceneName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return true;
            if (string.Equals(sceneName, "Empty", StringComparison.OrdinalIgnoreCase)) return true;
            return IsMainMenuOrBootSceneName(sceneName);
        }
    }
}