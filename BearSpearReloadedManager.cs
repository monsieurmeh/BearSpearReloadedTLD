#define DEV_BUILD
#define DEV_BUILD_LOG
//#define DEV_BUILD_SPAWNONE
//#define DEV_BUILD_LOG_VERBOSE

using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using System.Collections;
using UnityEngine;
using static MonsieurMeh.Mods.TLD.BearSpearReloaded.BearSpearReloadedManager;

namespace MonsieurMeh.Mods.TLD.BearSpearReloaded
{
    public class BearSpearReloadedManager : MonoBehaviour //just so I can get coroutines working and avoid constant update method running while spear is out and/or thrown into world... hate update methods
    {
        #region Consts & Enums

        const float MillisecondsPerTick = 0.0001f;
        const float SecondsPerTick = MillisecondsPerTick * 0.001f;

        const long TicksPerMillisecond = 10000;
        const long TicksPerSecond = TicksPerMillisecond * 1000;

        const string Null = "null";

        public enum Readouts : int
        {
            vp_FPSPLayer_HandleBearSpearInput = 0,
            BearSpearItem_Update = 1,
            COUNT
        }

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
        private long[] mLastReadout = new long[(int)Readouts.COUNT];
        private BaseAi mNearestAi;

        private long TicksSinceStart { get { return System.DateTime.Now.Ticks - mStartTime; } }
        public BaseAi NearestAi { get { return mNearestAi; } set { mNearestAi = value; } }


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


        public bool ReadyForReadout(Readouts readoutType, int msFrequency)
        {
            return System.DateTime.Now.Ticks - mLastReadout[(int)readoutType] >= msFrequency * TicksPerMillisecond;
        }


        public void TryReadout(string message, Readouts readoutType, int msFrequency)
        {
            if (ReadyForReadout(readoutType, msFrequency))
            {
                Readout(message, readoutType);
            }
        }


        public void Readout(string message, Readouts readoutType)
        {
            mLastReadout[(int)readoutType] = System.DateTime.Now.Ticks;
            Log($"[{readoutType.ToString().PadRight(48)}]: {message}");
        }


        public void Log(string message, bool error = false)
        {
#if DEV_BUILD
            try
            {
#endif
                string logMessage = $"[{TicksSinceStart.ToString().PadRight(12)}t/{(TicksSinceStart * MillisecondsPerTick).ToString().PadRight(12)}ms/{(TicksSinceStart * SecondsPerTick).ToString().PadRight(12)}s] {message}";
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
        public static BearSpearReloadedManager Manager { get { return Instance; } }
        public static void Log(string msg, bool error = false) => Instance.Log(msg, error);
        public static void Log(BaseAi baseAi, string msg, bool error = false) => Instance.Log(baseAi, msg, error);
        public static void LogError(string msg) => Log(msg, true);
        public static void LogError(BaseAi baseAi, string msg) => Log(baseAi, msg, true);
        public static bool ReadyForReadout(Readouts readoutType, int msFrequency) => Instance.ReadyForReadout(readoutType, msFrequency); 
        public static void TryReadout(string message, Readouts readoutType, int msFrequency) => Instance.TryReadout(message, readoutType, msFrequency); 
        public static void Readout(string message, Readouts readoutType) => Instance.Readout(message, readoutType);
    }
}