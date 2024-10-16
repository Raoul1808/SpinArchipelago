using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using BepInEx.Configuration;
using SpinCore.UI;

namespace SpinArchipelago
{
    internal static class ArchipelagoManager
    {
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

        private static readonly List<int> UnlockedSongs = new List<int>();
        private static readonly Queue<DeathLink> DeathLinkBuffer = new Queue<DeathLink>();

        public static bool IsConnected => _session?.Socket?.Connected ?? false;

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
                {
                    var g = UIHelper.CreateGroup(group.Transform, "PlayerName", Axis.Horizontal);
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
                    var g = UIHelper.CreateGroup(group.Transform, "ServerAddress", Axis.Horizontal);
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
                    var g = UIHelper.CreateGroup(group.Transform, "Password", Axis.Horizontal);
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
                    group.Transform,
                    "Connect",
                    "SpinArchipelago_Connect",
                    ConnectToServer
                );
            };
            UIHelper.RegisterMenuInModSettingsRoot("SpinArchipelago_Name", page);
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

        public static bool IsSongUnlocked(int id)
        {
            return UnlockedSongs.Contains(id);
        }

        public static void PlayingTrack()
        {
            _session.SetClientState(ArchipelagoClientState.ClientPlaying);
        }

        public static void StopPlaying()
        {
            _session.SetClientState(ArchipelagoClientState.ClientReady);
        }

        public static void ClearSong(int id)
        {
            _session.Locations.CompleteLocationChecks(id);
        }

        public static void SendDeathLink()
        {
            if (_justTriggeredDeathLink) return;
            _deathLink?.SendDeathLink(new DeathLink(_session.Players.ActivePlayer.Name, "Failed a song"));
        }

        public static void ApplyDeathLink()
        {
            if (DeathLinkBuffer.Count == 0) return;
            var deathLink = DeathLinkBuffer.Dequeue();
            _justTriggeredDeathLink = true;
            Track.FailSong();
            _justTriggeredDeathLink = false;
            NotificationSystemGUI.AddMessage($"DEATH LINK: {deathLink.Source} died. Cause: {deathLink.Cause}");
        }

        private static void ConnectToServer()
        {
            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                NotificationSystemGUI.AddMessage("Player Name is empty");
                return;
            }
            var addressParts = ServerAddress?.Split(':') ?? Array.Empty<string>();
            if (addressParts.Length != 2)
            {
                NotificationSystemGUI.AddMessage("Invalid address");
                return;
            }

            string address = addressParts[0];
            if (!ushort.TryParse(addressParts[1], out ushort port))
            {
                NotificationSystemGUI.AddMessage("Invalid port");
                return;
            }
            _session = ArchipelagoSessionFactory.CreateSession(address, port);
            NotificationSystemGUI.AddMessage("Connecting to Archipelago server");
            Login();
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
                NotificationSystemGUI.AddMessage("Failed to connect to server: check console logs for details");
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
            NotificationSystemGUI.AddMessage("Connected!");
            Log.Info("Connected!");
            _postConnecting = false;
            _session.SetClientState(ArchipelagoClientState.ClientReady);

            _deathLink = _session.CreateDeathLinkService();
            if ((long)success.SlotData["deathLink"] == 1)
                _deathLink.EnableDeathLink();
            _deathLink.OnDeathLinkReceived += DeathLinkHandler;
        }

        private static void DeathLinkHandler(DeathLink deathLink)
        {
            Log.Info($"DEATH LINK: Adding to buffer. {deathLink.Source} died. Cause: {deathLink.Cause}");
            DeathLinkBuffer.Enqueue(deathLink);
        }

        private static void ReceivedItemHandler(ReceivedItemsHelper helper)
        {
            while (helper.Any())
            {
                var itemInfo = helper.DequeueItem();
                UnlockedSongs.Add((int)itemInfo.ItemId);
                Log.Info($"NEW ITEM: Obtained Item {itemInfo.ItemId} {itemInfo.ItemName} from {itemInfo.Player.Name} in {itemInfo.Player.Game}");
                if (_postConnecting) continue;
                NotificationSystemGUI.AddMessage($"Song {itemInfo.ItemName} unlocked (courtesy of {itemInfo.Player.Name})");
                if (XDSelectionListMenu.Instance?.isActiveAndEnabled ?? false)
                    XDSelectionListMenu.Instance?.ActiveList?.SnapToIndex((int)itemInfo.ItemId);
            }
        }
    }
}
