using System;
using System.Collections.Generic;

[Serializable]
public class AccountLogEntry
{
    public string username;
    public string playerID;
    public string registrationDate;
}

[Serializable]
public class ServerData
{
    public int globalUserCount = 0;
    public int maxConcurrentClients = 50;
    public List<string> activeClients = new List<string>();
    public AccountLogEntry[] accountRegistryLog = new AccountLogEntry[0];
}