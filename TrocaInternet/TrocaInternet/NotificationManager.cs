using TrocaInternet.TrocaInternet;
using TrocaInternet.TrocaInternet.Network;

public static class NotificationManager
{
    private static bool _lastConnectionStatus = true;
    private static DateTime _lastNotificationTime = DateTime.MinValue;
    private static readonly TimeSpan NotificationCooldown = TimeSpan.FromMinutes(5);

    public static async Task MonitorAndNotifyAsync()
    {
        while (true)
        {
            bool currentStatus = await NetworkHelper.CheckConnectivityAsync();

            if (currentStatus != _lastConnectionStatus)
            {
                if (ShouldNotify())
                {
                    if (currentStatus)
                    {
                        ShowNotification("Conexão Restabelecida", "A conexão com a internet foi restabelecida.");
                    }
                    else
                    {
                        ShowNotification("Problema de Conexão", "Detectamos problemas com sua conexão de internet.");
                    }

                    _lastNotificationTime = DateTime.Now;
                }

                _lastConnectionStatus = currentStatus;
            }

            await Task.Delay(30000); // Verifica a cada 30 segundos
        }
    }

    private static bool ShouldNotify()
    {
        return DateTime.Now - _lastNotificationTime > NotificationCooldown;
    }

    private static void ShowNotification(string title, string message)
    {
        if (Program._notifyIcon != null)
        {
            Program._notifyIcon.BalloonTipTitle = title;
            Program._notifyIcon.BalloonTipText = message;
            Program._notifyIcon.ShowBalloonTip(5000);
        }

        Logger.LogInfo($"Notificação: {title} - {message}");
    }

    public static void TestNotification()
    {
        ShowNotification("Teste de Notificação", "Esta é uma notificação de teste do TrocaInternet.");
    }
}