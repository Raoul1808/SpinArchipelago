using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using SpinCore.UI;

namespace SpinArchipelago
{
    internal static class ArchipelagoManager
    {
        private const string DEFAULT_ARCHIPELAGO_URL = "archipelago.gg:38281";

        private static string _playerName;
        private static string _serverAddress;
        private static string _password;

        private static ArchipelagoSession _session;

        private static List<int> _unlockedSongs = new List<int>();

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
                    UIHelper.CreateInputField(
                        g.Transform,
                        "Input Field",
                        (_, newValue) => _playerName = newValue
                    );
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
                        (_, newValue) => _serverAddress = newValue
                    );
                    field.InputField.tmpInputField.text = DEFAULT_ARCHIPELAGO_URL;
                    _serverAddress = DEFAULT_ARCHIPELAGO_URL;
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

        private static void ConnectToServer()
        {
            if (string.IsNullOrWhiteSpace(_playerName))
            {
                NotificationSystemGUI.AddMessage("Player Name is empty");
                return;
            }
            var addressParts = _serverAddress?.Split(':') ?? Array.Empty<string>();
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
                    _playerName,
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
                string failureMessage = $"Failed to connect to {_serverAddress} as {_playerName}:";
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

            //var success = (LoginSuccessful)result;
            _session.Items.ItemReceived += ReceivedItemHandler;
            _session.SetClientState(ArchipelagoClientState.ClientConnected);
            NotificationSystemGUI.AddMessage("Connected!");
            Log.Info("Connected!");
        }

        private static void ReceivedItemHandler(ReceivedItemsHelper helper)
        {
            while (helper.Any())
            {
                var itemInfo = helper.DequeueItem();
                Log.Info($"CALLBACK: Obtained Item {itemInfo.ItemId} {itemInfo.ItemName} from {itemInfo.Player.Name} in {itemInfo.Player.Game}");
            }
        }
    }
}
