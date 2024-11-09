using BepInEx;

namespace SpinArchipelago
{
    public static class Util
    {
        public static void Notify(string message)
        {
            ThreadingHelper.Instance.StartSyncInvoke(() =>
            {
                NotificationSystemGUI.AddMessage(message);
            });
        }
    }
}
