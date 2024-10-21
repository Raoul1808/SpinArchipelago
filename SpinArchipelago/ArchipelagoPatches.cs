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

            __result = __instance.IndexInList == ArchipelagoManager.BossSong
                ? ArchipelagoManager.CanPlayBossSong
                : ArchipelagoManager.IsSongUnlocked(__instance.IndexInList + 1);
        }

        // private static float GetMaxAccuracy(this PlayState playState)
        // {
        //     var stats = playState.playStateStats.allTimed;
        //     float maxScore = stats.maxPossibleScore;
        //     float score = stats.score;
        //     return score / maxScore;
        //     // float accuracy = 0f;
        //     // int sectionCount = playState.trackData.EditorTrackCuePoints.Count - 1;
        //     // for (int i = 0; i < sectionCount; i++)
        //     // {
        //     //     (int currentScore, int maxPotentialScore, int maxScore) = playState.GetCurrentTotalsForPracticeSection(i);
        //     //     accuracy += (float)maxPotentialScore / maxScore;
        //     // }
        //     // return accuracy / sectionCount;
        // }
        
        [HarmonyPatch(typeof(CompleteSequenceGameState), nameof(CompleteSequenceGameState.OnBecameActive))]
        [HarmonyPostfix]
        private static void CompleteLocationCheck()
        {
            if (!ArchipelagoManager.IsConnected)
                return;
            MedalValue medal = default;
            FullComboState bestFcState = default;
            foreach (var playState in Track.PlayStates)
            {
                // If failed, ignore
                if (playState.playStateStatus == PlayStateStatus.Failure)
                    return;
                var m = new MedalValue(playState);
                if (m.Rank > medal.Rank)
                    medal = m;
                var fc = playState.fullComboState;
                if (fc > bestFcState)
                    bestFcState = fc;
            }
            var metadata = Track.PlayHandle.Setup.TrackDataSegmentForSingleTrackDataSetup.metadata;
            if (metadata.IsCustom)
                return;
            int trackId = metadata.IndexInList;
            ArchipelagoManager.ClearSong(trackId + 1, medal, bestFcState);
        }

        // [HarmonyPatch(typeof(LoadIntoPlayingGameState), nameof(LoadIntoPlayingGameState.LoadTrack))]
        // [HarmonyPrefix]
        // private static bool PreventPlay()
        // {
        //     var metadata = Track.PlayHandle.Setup.TrackDataSegmentForSingleTrackDataSetup.metadata;
        //     if (ArchipelagoManager.BossSong == metadata.IndexInList && !ArchipelagoManager.CanPlayBossSong)
        //     {
        //         NotificationSystemGUI.AddMessage("This is the boss song. You cannot play it for now.");
        //         return false;
        //     }
        //     return true;
        // }

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
