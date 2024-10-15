using SpinCore.UI;

namespace SpinArchipelago
{
    internal static class ArchipelagoManager
    {
        private static string _playerName;
        private static string _serverAddress;
        private static string _password;

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
                    UIHelper.CreateInputField(
                        g.Transform,
                        "Input Field",
                        (_, newValue) => _serverAddress = newValue
                    );
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
            NotificationSystemGUI.AddMessage("Not implemented yet");
        }
    }
}
