using BepInEx;

namespace SpinArchipelago
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        private const string Guid = "srxd.raoul1808.spinarchipelago";
        private const string Name = "Spin Archipelago";
        private const string Version = "0.1.0";

        private void Awake()
        {
            Log.Init(Logger);
            Log.Info($"Hello from {Name}");
        }
    }
}
