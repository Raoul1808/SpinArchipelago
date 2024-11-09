using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using BepInEx.Configuration;
using Newtonsoft.Json;
using SpinCore.UI;
using SpinCore.Utility;
using UnityEngine;

namespace SpinArchipelago
{
    internal static class ArchipelagoManager
    {
        private enum ClearCondition
        {
            Default,
            Medal,
            //Accuracy,
            FullCombo,
            PerfectFullCombo,
        }

        private const string DefaultArchipelagoURL = "archipelago.gg:38281";

        private static string _password;

        private static ConfigEntry<string> _playerName;
        private static ConfigEntry<string> _serverAddress;

        private static string PlayerName
        {
            get => _playerName.Value;
            set => _playerName.Value = value;
        }

        private static string ServerAddress
        {
            get => _serverAddress.Value;
            set => _serverAddress.Value = value;
        }

        private static ArchipelagoSession _session;
        private static DeathLinkService _deathLink;
        private static bool _postConnecting;
        private static bool _justTriggeredDeathLink = false;
        private static CustomGroup _connectUiGroup;
        private static CustomGroup _disconnectUiGroup;
        private static CustomMultiChoice _deathLinkToggle;
        
#if DEBUG
        private static CustomGroup _debugGroup;
        private static CustomMultiChoice _clearConditionMultiChoice;
        private static CustomMultiChoice _medalRequirementMultiChoice;
        private static CustomMultiChoice _targetAccuracyMultiChoice;
        private static CustomMultiChoice _clearsRequiredMultiChoice;
#endif

        private static readonly List<int> UnlockedSongs = new List<int>();
        private static readonly Queue<DeathLink> DeathLinkBuffer = new Queue<DeathLink>();
        private static Dictionary<string, int> _songNameToIdTable;

        private static bool _deathLinkEnabled;
        private static ClearCondition _clearCondition;
        private static int _medalIndex;
        private static float _targetAccuracy;

        private static int _clearsRequiredForGoal;
        public static int BossSong { get; private set; }

        public static bool CanPlayBossSong => _clearsRequiredForGoal != 0 &&
                                              _session.Locations.AllLocationsChecked.Count >= _clearsRequiredForGoal;

        private static bool DeathLinkEnabled
        {
            get => _deathLinkEnabled;
            set
            {
                if (value)
                    _deathLink.EnableDeathLink();
                else
                    _deathLink.DisableDeathLink();
                _deathLinkEnabled = value;
            }
        }

        public static bool IsConnected => _session?.Socket?.Connected ?? false;

        public static void ParseSongList()
        {
            var dict = new Dictionary<string, int>();
            var songListRawStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SpinArchipelago.Resources.SongList.txt");
            if (songListRawStream == null)
                throw new InvalidDataException("No song list");
            string songListRaw;
            using (var reader = new StreamReader(songListRawStream))
            {
                songListRaw = reader.ReadToEnd();
            }

            foreach (string line in songListRaw.Split('\n'))
            {
                string[] elems = line.Trim().Split('|');
                if (elems.Length < 2) continue;
                string songName = elems[1];
                if (dict.ContainsKey(songName)) continue;
                if (!int.TryParse(elems[0], out int songId)) continue;
                dict.Add(songName, songId);
            }

            if (dict.Count == 0)
                throw new InvalidDataException("There are no songs in the list");
            _songNameToIdTable = dict;
        }

        public static void InitUI()
        {
            var page = UIHelper.CreateCustomPage("SpinArchipelago");
            page.OnPageLoad += pageParent =>
            {
                var group = UIHelper.CreateGroup(pageParent, "Server");
                UIHelper.CreateSectionHeader(
                    group.Transform,
                    "Header",
                    "SpinArchipelago_ServerSettings",
                    false
                );
                _connectUiGroup = UIHelper.CreateGroup(pageParent, "Connect");
                {
                    var g = UIHelper.CreateGroup(_connectUiGroup.Transform, "PlayerName", Axis.Horizontal);
                    UIHelper.CreateLabel(
                        g.Transform,
                        "Label",
                        "SpinArchipelago_PlayerName"
                    );
                    var field = UIHelper.CreateInputField(
                        g.Transform,
                        "Input Field",
                        (_, newValue) => PlayerName = newValue
                    );
                    field.InputField.tmpInputField.text = PlayerName;
                }
                {
                    var g = UIHelper.CreateGroup(_connectUiGroup.Transform, "ServerAddress", Axis.Horizontal);
                    UIHelper.CreateLabel(
                        g.Transform,
                        "Label",
                        "SpinArchipelago_ServerAddress"
                    );
                    var field = UIHelper.CreateInputField(
                        g.Transform,
                        "Input Field",
                        (_, newValue) => ServerAddress = newValue
                    );
                    field.InputField.tmpInputField.text = ServerAddress;
                }
                {
                    var g = UIHelper.CreateGroup(_connectUiGroup.Transform, "Password", Axis.Horizontal);
                    UIHelper.CreateLabel(
                        g.Transform,
                        "Label",
                        "SpinArchipelago_Password"
                    );
                    UIHelper.CreateInputField(
                        g.Transform,
                        "Input Field",
                        (_, newValue) => _password = newValue
                    );
                }
                UIHelper.CreateButton(
                    _connectUiGroup.Transform,
                    "Connect",
                    "SpinArchipelago_Connect",
                    ConnectToServer
                );
                _disconnectUiGroup = UIHelper.CreateGroup(group.Transform, "Disconnect");
                UIHelper.CreateButton(
                    _disconnectUiGroup.Transform,
                    "Disconnect",
                    "SpinArchipelago_Disconnect",
                    DisconnectFromServer
                );
                _deathLinkToggle = UIHelper.CreateLargeToggle(
                    _disconnectUiGroup.Transform,
                    "DeathLink",
                    "SpinArchipelago_DeathLink",
                    false,
                    val => DeathLinkEnabled = val
                );
                _disconnectUiGroup.Active = false;
                _connectUiGroup.Active = true;

#if DEBUG
                _debugGroup = UIHelper.CreateGroup(pageParent, "Debug");
                UIHelper.CreateSectionHeader(
                    _debugGroup.Transform,
                    "Header",
                    "SpinArchipelago_DebugSection",
                    true
                );
                _clearConditionMultiChoice = UIHelper.CreateLargeMultiChoiceButton(
                    _debugGroup.Transform,
                    "Clear Condition",
                    "SpinArchipelago_ClearCondition",
                    ClearCondition.Default,
                    val => _clearCondition = val
                );
                _medalRequirementMultiChoice = UIHelper.CreateLargeMultiChoiceButton(
                    _debugGroup.Transform,
                    "Medal Requirement",
                    "SpinArchipelago_MedalRequirement",
                    0,
                    val => _medalIndex = val,
                    () => new IntRange(1, MedalValue.RanksPerDifficulty + 1),
                    val => TrackDataMetadata.RankConversion[val].rankString
                );
                _targetAccuracyMultiChoice = UIHelper.CreateLargeMultiChoiceButton(
                    _debugGroup.Transform,
                    "Target Accuracy",
                    "SpinArchipelago_TargetAccuracy",
                    80,
                    val => _targetAccuracy = val / 100f,
                    () => new IntRange(0, 101),
                    val => val + "%"
                );
                _clearsRequiredMultiChoice = UIHelper.CreateLargeMultiChoiceButton(
                    _debugGroup,
                    "Clears Required",
                    "SpinArchipelago_ClearsRequired",
                    20,
                    val => _clearsRequiredForGoal = val,
                    () => new IntRange(2, 68),
                    val => val.ToString()
                );
                UIHelper.AddTooltip(_clearsRequiredMultiChoice, "SpinArchipelago_ClearsRequired_Tooltip");
                UIHelper.CreateButton(
                    _debugGroup,
                    "Manually Trigger Goal",
                    "SpinArchipelago_ManuallyTriggerGoal",
                    () => _session.SetGoalAchieved()
                );
#endif
            };
            UIHelper.RegisterMenuInModSettingsRoot("SpinArchipelago_Name", page);

            var archipelagoIconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SpinArchipelago.Resources.archipelago_white.png");
            var archipelagoIconTex = RuntimeAssetLoader.Texture2DFromStream(archipelagoIconStream);
            var archipelagoSprite = Sprite.Create(archipelagoIconTex, new Rect(1, 1, 510, 510), Vector2.zero);
            var archipelagoSidePanel = UIHelper.CreateSidePanel("SpinArchipelagoTab", "SpinArchipelago_Name", archipelagoSprite);
            archipelagoSidePanel.OnSidePanelLoaded += panelParent =>
            {
                UIHelper.CreateLabel(
                    panelParent,
                    "UnderConstruction",
                    "SpinArchipelago_UnderConstruction"
                );
                UIHelper.CreateButton(
                    panelParent,
                    "FocusBossSong",
                    "SpinArchipelago_FocusBossSong",
                    () =>
                    {
                        if (!CanPlayBossSong)
                        {
                            Util.Notify("Boss song not available");
                            return;
                        }

                        string bossSongName = _songNameToIdTable.FirstOrDefault(kvp => kvp.Value == BossSong).Key;
                        var item = XDSelectionListMenu.Instance.ActiveList.items.Find(i => i.Title == bossSongName);
                        if (item != null)
                            XDSelectionListMenu.Instance.ScrollToItem(item);
                        else
                            Util.Notify("Unable to find boss song. Make sure your filters are cleared.");
                    }
                );
                UIHelper.CreateButton(
                    panelParent,
                    "GenerateSongList",
                    "SpinArchipelago_GenerateSongList",
                    () =>
                    {
                        string path = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "GeneratedSongList.txt");
                        // var songs = new List<ArchipelagoSongId>();
                        var songs = new StringBuilder();
                        var songSelectItems = XDSelectionListMenu.Instance.ActiveList.items;
                        for (int i = 0; i < songSelectItems.Count; i++)
                        {
                            var songItem = songSelectItems[i];
                            if (songItem is MetadataHandle handle)
                                // songs.Add(new ArchipelagoSongId(i + 1, handle));
                                songs.AppendLine($"{i + 1}|{handle.TrackInfoMetadata.title}");
                        }

                        // string serializedSongs = JsonConvert.SerializeObject(songs);
                        File.WriteAllText(path, songs.ToString());
                        Util.Notify("Generated song list from current song selection. Check your plugins directory");
                    }
                );
            };
        }

        public static void GrabConfigEntries(ConfigFile config)
        {
            _playerName = config.Bind(
                "Server",
                "PlayerName",
                "",
                "The player name in the multiworld."
            );
            _serverAddress = config.Bind(
                "Server",
                "ServerAddress",
                DefaultArchipelagoURL,
                "The Archipelago server address."
            );
        }

        public static bool IsSongUnlocked(string title)
        {
            return _songNameToIdTable != null &&
                   _songNameToIdTable.TryGetValue(title, out int id) &&
                   UnlockedSongs.Contains(id);
        }

        public static void PlayingTrack()
        {
            _session.SetClientState(ArchipelagoClientState.ClientPlaying);
        }

        public static void StopPlaying()
        {
            _session.SetClientState(ArchipelagoClientState.ClientReady);
        }

        public static void ClearSong(string name, MedalValue medal, FullComboState fcState)
        {
            if (!IsConnected ||
                _songNameToIdTable == null ||
                !_songNameToIdTable.TryGetValue(name, out int id) ||
                _session.Locations.AllLocationsChecked.Contains(id))
                return;
            switch (_clearCondition)
            {
                case ClearCondition.Medal:
                    if (medal.Rank <= _medalIndex)
                        return;
                    break;

                // case ClearCondition.Accuracy:
                //     if (accuracy < _targetAccuracy)
                //         return;
                //     break;

                case ClearCondition.FullCombo:
                    if (fcState == FullComboState.None)
                        return;
                    break;

                case ClearCondition.PerfectFullCombo:
                    if (fcState < FullComboState.Perfect)
                        return;
                    break;

                case ClearCondition.Default:
                default:
                    break;
            }
            _session.Locations.CompleteLocationChecks(id);
            Util.Notify($"Completed song {id}. Location has been checked.");
            if (CanPlayBossSong && !UnlockedSongs.Contains(BossSong))
            {
                UnlockedSongs.Add(BossSong);
                Util.Notify("Boss Song unlocked. Clear it to finish your run!");
            }
            if (id == BossSong)
            {
                _session.SetGoalAchieved();
                Util.Notify("Congratulations, you have achieved your goal!");
            }
        }

        public static void SendDeathLink(string title, TrackData.DifficultyType difficulty)
        {
            if (_justTriggeredDeathLink) return;
            _deathLink?.SendDeathLink(new DeathLink(_session.Players.ActivePlayer.Name, $"Failed song {title} on difficulty {difficulty}"));
        }

        public static void ApplyDeathLink()
        {
            if (DeathLinkBuffer.Count == 0) return;
            var deathLink = DeathLinkBuffer.Dequeue();
            _justTriggeredDeathLink = true;
            Track.FailSong();
            _justTriggeredDeathLink = false;
            Util.Notify($"DEATH LINK: {deathLink.Source} died. Cause: {deathLink.Cause}");
        }

        private static void ConnectToServer()
        {
            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                Util.Notify("Player Name is empty");
                return;
            }
            var addressParts = ServerAddress?.Split(':') ?? Array.Empty<string>();
            if (addressParts.Length != 2)
            {
                Util.Notify("Invalid address");
                return;
            }

            string address = addressParts[0];
            if (!ushort.TryParse(addressParts[1], out ushort port))
            {
                Util.Notify("Invalid port");
                return;
            }
            _session = ArchipelagoSessionFactory.CreateSession(address, port);
            Util.Notify("Connecting to Archipelago server");
            Login();
        }

        private static void DisconnectFromServer()
        {
            _deathLink.DisableDeathLink();
            _deathLink.OnDeathLinkReceived -= DeathLinkHandler;
            _session.Items.ItemReceived -= ReceivedItemHandler;
            _session.Socket.DisconnectAsync();
            _deathLink = null;
            _session = null;
            _disconnectUiGroup.Active = false;
            _connectUiGroup.Active = true;
        }

        private static void Login()
        {
            LoginResult result;

            try
            {
                result = _session.TryConnectAndLogin(
                    "Spin Rhythm XD",
                    PlayerName,
                    ItemsHandlingFlags.AllItems,
                    password: _password
                );
            }
            catch (Exception e)
            {
                result = new LoginFailure(e.GetBaseException().Message);
            }

            if (!result.Successful)
            {
                var failure = (LoginFailure)result;
                Util.Notify("Failed to connect to server: check console logs for details");
                string failureMessage = $"Failed to connect to {ServerAddress} as {PlayerName}:";
                foreach (string error in failure.Errors)
                {
                    failureMessage += $"\n    {error}";
                }
                foreach (var error in failure.ErrorCodes)
                {
                    failureMessage += $"\n    {error}";
                }
                Log.Error(failureMessage);
                return;
            }

            var success = (LoginSuccessful)result;
            _postConnecting = true;
            _session.Items.ItemReceived += ReceivedItemHandler;
            _session.SetClientState(ArchipelagoClientState.ClientConnected);
            Util.Notify("Connected!");
            Log.Info("Connected!");
            _session.SetClientState(ArchipelagoClientState.ClientReady);
            _postConnecting = false;

            _deathLink = _session.CreateDeathLinkService();
            _deathLink.OnDeathLinkReceived += DeathLinkHandler;
            DeathLinkEnabled = (long)success.SlotData["deathLink"] == 1;
            _clearCondition = (ClearCondition)(int)(long)success.SlotData["clearCondition"];
            _medalIndex = (int)(long)success.SlotData["medalRequirement"] + 1;
            _targetAccuracy = (int)(long)success.SlotData["targetAccuracy"] / 100f;
            BossSong = (int)(long)success.SlotData["bossSong"] + 1;
            _clearsRequiredForGoal = (int)(long)success.SlotData["clearsRequiredForGoal"];
            _deathLinkToggle.SetCurrentValue(DeathLinkEnabled ? 1 : 0);
            _connectUiGroup.Active = false;
            _disconnectUiGroup.Active = true;

#if DEBUG
            _clearConditionMultiChoice.SetCurrentValue((int)_clearCondition);
            _medalRequirementMultiChoice.SetCurrentValue(_medalIndex);
            _targetAccuracyMultiChoice.SetCurrentValue((int)(_targetAccuracy * 100));
            _clearsRequiredMultiChoice.SetCurrentValue(_clearsRequiredForGoal);
#endif
        }

        private static void DeathLinkHandler(DeathLink deathLink)
        {
            Log.Info($"DEATH LINK: Adding to buffer. {deathLink.Source} died. Cause: {deathLink.Cause}");
            DeathLinkBuffer.Enqueue(deathLink);
        }

        private static void ReceivedItemHandler(ReceivedItemsHelper helper)
        {
            var itemInfo = helper.DequeueItem();
            UnlockedSongs.Add((int)itemInfo.ItemId);
            Log.Info($"NEW ITEM: Obtained Item {itemInfo.ItemId} {itemInfo.ItemName} from {itemInfo.Player.Name} in {itemInfo.Player.Game}");
            if (_postConnecting) return;
            Util.Notify($"Song {itemInfo.ItemName} unlocked (courtesy of {itemInfo.Player.Name})");
            if (XDSelectionListMenu.Instance?.isActiveAndEnabled ?? false)
            {
                XDSelectionListMenu.Instance?.UpdateListDisplays();
                XDSelectionListMenu.Instance?.ActiveList?.SnapToIndex((int)itemInfo.ItemId);
            }
        }
    }
}
