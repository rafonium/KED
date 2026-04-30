using UnityEngine;

namespace KerbalEngineDynamics
{
    public static class KEDLog
    {
        public static void Debug(string message)
        {
            if (KEDSettings.debugMode)
            {
                UnityEngine.Debug.Log($"[KED-DEBUG] {message}");
            }
        }

        public static void Info(string message)
        {
            UnityEngine.Debug.Log($"[KED] {message}");
        }

        public static void Error(string message)
        {
            UnityEngine.Debug.LogError($"[KED-ERROR] {message}");
        }

        public static void ScreenAndLog(string message, float duration = 5f, ScreenMessageStyle style = ScreenMessageStyle.UPPER_CENTER)
        {
            ScreenMessages.PostScreenMessage(message, duration, style);
            Info(message);
        }
        
        public static void DebugScreenAndLog(string message, float duration = 5f, ScreenMessageStyle style = ScreenMessageStyle.UPPER_CENTER)
        {
            if (KEDSettings.debugMode)
            {
                ScreenMessages.PostScreenMessage($"[DEBUG] {message}", duration, style);
                Debug(message);
            }
        }
    }
}
