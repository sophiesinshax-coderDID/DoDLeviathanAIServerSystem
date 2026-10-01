using System;

[Serializable]
public class ChatPacket
{
    public string type;
    public string sender;
    public string channel;
    public string content;
    public long timestamp;
}