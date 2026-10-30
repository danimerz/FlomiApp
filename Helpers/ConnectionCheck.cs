using Microsoft.JSInterop;

namespace FlomiApp.Helpers;

/// <summary>
/// Gegenstelle für <c>wwwroot/js/reconnect.js</c>: Nach einer Pause im Hintergrund fragt der
/// Browser hier an, ob die Verbindung zum Server noch lebt. Bleibt die Antwort aus, lädt er neu.
/// </summary>
public static class ConnectionCheck
{
    [JSInvokable("Ping")]
    public static bool Ping() => true;
}
