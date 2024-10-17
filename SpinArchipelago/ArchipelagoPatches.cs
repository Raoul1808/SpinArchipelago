using HarmonyLib;
using XDMenuPlay;

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

        [HarmonyPatch(typeof(CompleteSequenceGameState), nameof(CompleteSequenceGameState.OnBecameActive))]
        [HarmonyPostfix]
        private static void CompleteLocationCheck()
        {
            if (!ArchipelagoManager.IsConnected)
                return;
            // If failed, ignore
            foreach (var playState in Track.PlayStates)
                if (playState.playStateStatus == PlayStateStatus.Failure)
                    return;
            var metadata = Track.PlayHandle.Setup.TrackDataSegmentForSingleTrackDataSetup.metadata;
            if (metadata.IsCustom)
                return;
            int trackId = metadata.IndexInList;
            NotificationSystemGUI.AddMessage($"Completed song {trackId}. Sending to Archipelago server now");
            ArchipelagoManager.ClearSong(trackId + 1);
        }

        [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack))]
        [HarmonyPostfix]
        private static void SetArchipelagoPlayingState()
        {
            if (ArchipelagoManager.IsConnected)
                ArchipelagoManager.PlayingTrack();
        }

        [HarmonyPatch(typeof(SpinMenu), nameof(SpinMenu.OpenMenu))]
        [HarmonyPostfix]
        private static void StopArchipelagoPlayingState(SpinMenu __instance)
        {
            if (ArchipelagoManager.IsConnected && __instance.GetType() == typeof(XDSelectionListMenu))
                ArchipelagoManager.StopPlaying();
        }

        [HarmonyPatch(typeof(Track), nameof(Track.FailSong))]
        [HarmonyPostfix]
        private static void SendDeathLink()
        {
            if (!ArchipelagoManager.IsConnected) return;
            string title = Track.PlayHandle.Setup.TrackDataSegmentForSingleTrackDataSetup.metadata.Title;
            var diff = Track.PlayHandle.data.Difficulty;
            ArchipelagoManager.SendDeathLink(title, diff);
        }

        [HarmonyPatch(typeof(Track), nameof(Track.Update))]
        [HarmonyPostfix]
        private static void ApplyQueuedDeathLinks()
        {
            if (ArchipelagoManager.IsConnected && Track.IsPlaying)
                ArchipelagoManager.ApplyDeathLink();
        }
    }
}
