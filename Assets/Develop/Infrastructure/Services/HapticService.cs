using UnityEngine;

namespace NeonSeven.Infrastructure.Services
{
    public sealed class HapticService
    {
        public void Light()
        {
            Vibrate(6);
        }

        public void Drop() => Vibrate(14);
        public void Pop(int wave)
        {
            if (wave > 1)
                Vibrate(18, 30, 24);
            else
                Vibrate(16);
        }
        public void Rise() => Vibrate(10, 20, 10);
        public void Clear() => Vibrate(30, 40, 30, 60);

        private static void Vibrate(params long[] pattern)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (pattern.Length == 1)
                    {
                        vibrator.Call("vibrate", pattern[0]);
                    }
                    else
                    {
                        var fullPattern = new long[pattern.Length + 1];
                        System.Array.Copy(pattern, 0, fullPattern, 1, pattern.Length);
                        vibrator.Call("vibrate", fullPattern, -1);
                    }
                }
            }
            catch
            {
                // Haptics are optional on unsupported Android devices.
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
