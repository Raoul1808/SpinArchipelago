using System.Reflection;
using BepInEx;
using HarmonyLib;
using SpinCore;
using SpinCore.Translation;

namespace SpinArchipelago
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(SpinCorePlugin.Guid, SpinCorePlugin.Version)]
    public class Plugin : BaseUnityPlugin
    {
        // TODO: Add buffer for notifications to prevent game crashing (note: should probably hijack this on SpinCore)
        // TODO: Add target accuracy back once I figured out how to get a REAL accuracy number
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
            ArchipelagoManager.ParseSongList();

            var harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(ArchipelagoPatches));

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SpinArchipelago.locale.json");
            TranslationHelper.LoadTranslationsFromStream(stream);
#if DEBUG
            TranslationHelper.AddTranslation("SpinArchipelago_DebugSection", "Debug");
            TranslationHelper.AddTranslation("SpinArchipelago_ClearCondition", "Clear Condition");
            TranslationHelper.AddTranslation("SpinArchipelago_MedalRequirement", "Medal Requirement");
            TranslationHelper.AddTranslation("SpinArchipelago_TargetAccuracy", "Target Accuracy");
            TranslationHelper.AddTranslation("SpinArchipelago_ClearsRequired", "Clears Required");
            TranslationHelper.AddTranslation("SpinArchipelago_ClearsRequired_Tooltip", "The amount of clears required to unlock the final song of the run. Clearing the final song will count as a goal completion.");
            TranslationHelper.AddTranslation("SpinArchipelago_ManuallyTriggerGoal", "Manually Trigger Goal");
#endif
            ArchipelagoManager.GrabConfigEntries(Config);
            ArchipelagoManager.InitUI();
        }
    }
}
