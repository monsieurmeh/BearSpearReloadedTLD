using Il2Cpp;
using Il2CppSuperSplines;
using MelonLoader;
using UnityEngine;


[assembly: MelonInfo(typeof(MonsieurMeh.Mods.TLD.BearSpearReloaded.Main), "BearSpearReloaded", "0.0.1", "MonsieurMeh", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace MonsieurMeh.Mods.TLD.BearSpearReloaded
{
    public class Main : MelonMod
    {
        protected bool mInitialized = false;
        protected BearSpearReloadedManager mManager; 

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg(Initialize() ? "Initialized Successfully!" : "Initialization Errors!");
        }

        public override void OnDeinitializeMelon()
        {
            LoggerInstance.Msg(Shutdown() ? "Shutdown Successfully!" : "Shutdown Errors!");
        }


        protected bool Initialize()
        {
            mManager = BearSpearReloadedManager.Instance;
            mManager.Initialize(new Settings(), (s) => LoggerInstance.Msg(s), (err) => LoggerInstance.Error(err));
            mInitialized = mManager != null;
            return mInitialized;
        }


        protected bool Shutdown()
        {
            mInitialized = mManager.Shutdown();
            return !mInitialized;
        }
    }
}