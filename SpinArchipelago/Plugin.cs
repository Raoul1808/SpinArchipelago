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
