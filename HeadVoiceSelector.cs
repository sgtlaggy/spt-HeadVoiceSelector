#if !UNITY_EDITOR
using BepInEx;
using HeadVoiceSelector.Patches;
using System;
using System.IO;

namespace HeadVoiceSelector
{
    [BepInPlugin("com.HeadVoiceSelector.Core", "HeadVoiceSelector Core", "1.0.8")]

    internal class HeadVoiceSelector : BaseUnityPlugin
    {

        public static HeadVoiceSelector instance;

        public static string modPath = Path.Combine(Environment.CurrentDirectory, "user", "mods", "WTT-HeadVoiceSelector");
        public static string pluginPath = Path.Combine(Environment.CurrentDirectory, "BepInEx", "plugins", "WTT-HeadVoiceSelector");


        internal void Awake()
        {
            instance = this;

            new OverallScreenPatch().Enable();

        }
    }
}
#endif
