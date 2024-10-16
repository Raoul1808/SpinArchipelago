using System.Reflection;
using BepInEx;
using HarmonyLib;
using SpinCore.Translation;

namespace SpinArchipelago
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("srxd.raoul1808.spincore")]
    public class Plugin : BaseUnityPlugin
    {
        // TODO: Fix clearing counted when failing
        // TODO: Add goal
        // TODO: Configure goal
        // TODO: Add clear conditions
        // TODO: Add traps
        // TODO: Trap ideas:
        // TODO: Chroma Traps (all rainbow, all grayscale, random palette changes)
        // TODO: Track Speed changes (SONIC, training bandwidth, nauseating slowdown and speedups)
        // TODO: Distracting camera transforms
        // TODO: Spoon
        // TODO: There is no spoon :(
        // TODO: Changing playback rate (INVALIDATE SCORE!!!!)
        // TODO: Add rank checkign to force combo (unrelated but todoing anyway)
        // TODO: 
        // TODO: 
        // TODO: 
        private const string Guid = "srxd.raoul1808.spinarchipelago";
        private const string Name = "Spin Archipelago";
        private const string Version = "0.1.0";

        private void Awake()
        {
            Log.Init(Logger);
            Log.Info($"Hello from {Name}");

            var harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(ArchipelagoPatches));

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SpinArchipelago.locale.json");
            TranslationHelper.LoadTranslationsFromStream(stream);
            ArchipelagoManager.GrabConfigEntries(Config);
            ArchipelagoManager.InitUI();
        }
    }
}
