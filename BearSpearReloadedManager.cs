#define DEV_BUILD
#define DEV_BUILD_LOG
//#define DEV_BUILD_SPAWNONE
//#define DEV_BUILD_LOG_VERBOSE

using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using System.Collections;
using UnityEngine;

namespace MonsieurMeh.Mods.TLD.BearSpearReloaded
{
    public class BearSpearReloadedManager : MonoBehaviour //just so I can get coroutines working and avoid constant update method running while spear is out and/or thrown into world... hate update methods
    {
        #region Consts & Enums

        const float MillisecondsPerTick = 0.0001f;
        const float SecondsPerTick = MillisecondsPerTick * 0.001f;
        const long TicksPerUpdate = 10000000;
        const string Null = "null";

        #endregion


        #region Lazy Singleton

        private class Nested
        {
            static Nested()
            {
            }

            internal static readonly BearSpearReloadedManager instance = new BearSpearReloadedManager();
        }

        private BearSpearReloadedManager() { }
        public static BearSpearReloadedManager Instance { get { return Nested.instance; } }

        #endregion


        private Action<string> mLogMessageAction;
        private Action<string> mLogErrorAction;
        private Settings mSettings;
        private bool mInitialized = false;
        private bool mEnabled = false;
        private long mStartTime = System.DateTime.Now.Ticks;

        private long TicksSinceStart { get { return System.DateTime.Now.Ticks - mStartTime; } }


        public bool Initialize(Settings settings, Action<string> logMessageAction, Action<string> logErrorAction)
        {
            if (mInitialized)
            {
                return false;
            }
            mInitialized = true;
            mStartTime = System.DateTime.Now.Ticks;
            mSettings = settings;
            mLogMessageAction = logMessageAction;
            mLogErrorAction = logErrorAction;
            return true;
        }


        public bool Shutdown()
        {
            if (!mInitialized)
            {
                return false;
            }
            mInitialized = false;
            mSettings = null;
            mLogMessageAction = null;
            mLogErrorAction = null;
            return true;
        }


        public void Log(string message, bool error = false)
        {
#if DEV_BUILD
            try
            {
#endif
                string logMessage = $"[{TicksSinceStart}t/{TicksSinceStart * MillisecondsPerTick}ms/{TicksSinceStart * SecondsPerTick}s] {message}";
                if (error)
                {
                    mLogErrorAction.Invoke(logMessage);
                }
                else
                {
                    mLogMessageAction.Invoke(logMessage);
                }
#if DEV_BUILD
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while trying to log a message: {e}");
            }
#endif
        }



        
        public void LogError(string message)
        {
            Log(message, true);
        }


        public void Log(BaseAi baseAi, string msg, bool error = false)
        {
            Log($"{BaseAiInfo(baseAi)} {msg}", error);
        }


        public void LogError(BaseAi baseAi, string msg)
        {
            Log(baseAi, msg, true);
        }


        public static string BaseAiInfo(BaseAi baseAi)
        {
            return $"{baseAi?.gameObject?.name ?? Null} ({baseAi?.GetType()}) [{baseAi?.GetHashCode()}] at {baseAi?.gameObject?.transform?.position ?? Vector3.zero}";
        }
    }


    public static class Helpers
    {
        public static BearSpearReloadedManager Manager { get { return BearSpearReloadedManager.Instance; } }
        public static void Log(string msg, bool error = false) => BearSpearReloadedManager.Instance.Log(msg, error);
        public static void Log(BaseAi baseAi, string msg, bool error = false) => BearSpearReloadedManager.Instance.Log(baseAi, msg, error);
        public static void LogError(string msg) => Log(msg, true);
        public static void LogError(BaseAi baseAi, string msg) => Log(baseAi, msg, true);

    }
}