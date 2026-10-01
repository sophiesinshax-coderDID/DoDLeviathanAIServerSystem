[System.Serializable]
public class UserProfile
{
    public int Id { get; set; } 
    public string loginName { get; set; }
    public string displayName { get; set; }
    public string playerID { get; set; }
    public int authLevel { get; set; }
    public int playerTier { get; set; } // -4 to 15
    
    public string email;
    public string discordName;
    public string steamName;
    public string steamID;
    public string passwordHash;
    public string oldPID;
    public string accountCreationIP;
    public string currentLoginSessionIP;
}