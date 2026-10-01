public static class LeviathanNetworkTelemetry
{
    public static long TotalBytesSent { get; private set; } = 0;
    public static long TotalBytesReceived { get; private set; } = 0;
    public static int ActiveRequestsCount { get; set; } = 0;
    public static string LastPacketSizeText { get; private set; } = "None";

    public static void LogPacketSent(string payloadJson)
    {
        int size = System.Text.Encoding.UTF8.GetByteCount(payloadJson);
        TotalBytesSent += size;
        LastPacketSizeText = $"Out: {size} B";
    }

    public static void LogPacketReceived(string responseJson)
    {
        int size = System.Text.Encoding.UTF8.GetByteCount(responseJson);
        TotalBytesReceived += size;
        LastPacketSizeText = $"In: {size} B";
    }

    public static void ResetStats()
    {
        TotalBytesSent = 0;
        TotalBytesReceived = 0;
        ActiveRequestsCount = 0;
        LastPacketSizeText = "None";
    }
}