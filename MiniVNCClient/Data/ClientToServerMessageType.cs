#pragma warning disable CS1591

namespace MiniVNCClient.Data
{
    /// <summary>
    /// Codes of types of message types a VNC client can send to server
    /// </summary>
    public enum ClientToServerMessageType : byte
    {
        SetPixelFormat = 0,
        SetEncodings = 2,
        FramebufferUpdateRequest = 3,
        KeyEvent = 4,
        PointerEvent = 5,
        ClientCutText = 6,
        FileTransfer = 7,
        SetScale = 8,
        SetServerInput = 9,
        SetSW = 10,
        TextChat = 11,
        KeyFrameRequest = 12,
        KeepAlive = 13,
        SetScaleFactor = 15,
        /* 16 to 19 */
        UltraVNC = 16,
        RequestSession = 20,
        SetSession = 21,
        NotifyPluginStreaming = 80,
        /* 127, 254 */
        VMware = 127,
        CarConnectivity = 128,
        EnableContinuousUpdates = 150,
        ClientFence = 248,
        OLIVECallControl = 249,
        xvpClientMessage = 250,
        SetDesktopSize = 251,
        Tight = 252,
        giiClientMessage = 253,
        QEMUClientMessage = 255
    }
}

#pragma warning restore CS1591
