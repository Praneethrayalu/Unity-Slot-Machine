using UnityEngine;

namespace SlotGame.Utilities
{
    /// <summary>Global, one-time runtime settings applied before the first frame.</summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            // Smooth reel motion needs a steady frame rate; 60 is the sweet spot for WebGL/mobile.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            // Recording mode (?capture=30): game time advances a fixed step per rendered frame, so a
            // screen recording on a slow machine can be retimed into perfectly smooth footage.
            if (TryReadIntParameter(Application.absoluteURL, "capture", out int captureFps) && captureFps > 0)
            {
                Time.captureFramerate = captureFps;
                Debug.Log($"[Slot] Capture mode: {captureFps} fps fixed timestep.");
            }
        }

        /// <summary>Reads an integer query-string parameter (<c>?name=123</c>) from a URL.</summary>
        public static bool TryReadIntParameter(string url, string name, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            int queryStart = url.IndexOf('?');
            if (queryStart < 0)
            {
                return false;
            }

            string prefix = name + "=";
            foreach (string pair in url.Substring(queryStart + 1).Split('&', '#'))
            {
                if (pair.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(pair.Substring(prefix.Length), out value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
