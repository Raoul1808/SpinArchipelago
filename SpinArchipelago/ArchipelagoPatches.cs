using HarmonyLib;

namespace SpinArchipelago
{
    internal static class ArchipelagoPatches
    {
        [HarmonyPatch(typeof(MetadataHandle), nameof(MetadataHandle.IsUnlocked), MethodType.Getter)]
        [HarmonyPostfix]
        private static void ForceTrackLocked(MetadataHandle __instance, ref bool __result)
        {
            if (!ArchipelagoManager.IsConnected || __instance.Title == "Tutorial")
                return;

            if (__instance.IsCustom)
            {
                __result = false;
                return;
            }

            __result = ArchipelagoManager.IsSongUnlocked(__instance.IndexInList + 1);
        }
    }
}
