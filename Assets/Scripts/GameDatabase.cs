using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LiteDB;

public static class GameDatabase
{
    // This creates a single local database file in your persistent data path
    private static string DBPath => Path.Combine(Application.persistentDataPath, "CorrosiveWorld.db");

    // Save or Update a User Profile
    public static void SaveUser(UserProfile profile)
    {
        using (var db = new LiteDatabase(DBPath))
        {
            var col = db.GetCollection<UserProfile>("users");
            // Upsert (Inserts if new, updates if loginName already exists)
            col.Upsert(profile);
            Debug.Log($"<color=green>LiteDB:</color> Saved user {profile.loginName}");
        }
    }

    // Load a User Profile by Login Name
    public static UserProfile LoadUser(string loginName)
    {
        using (var db = new LiteDatabase(DBPath))
        {
            var col = db.GetCollection<UserProfile>("users");
            string cleanName = loginName.Trim().ToLower();
            // Query using clean C# lambda expression (No SQL!)
            return col.FindOne(x => x.loginName.ToLower() == cleanName);
        }
    }

    // Get All Users (Great for your Editor Tool or UI lists)
    public static List<UserProfile> GetAllUsers()
    {
        using (var db = new LiteDatabase(DBPath))
        {
            var col = db.GetCollection<UserProfile>("users");
            return new List<UserProfile>(col.FindAll());
        }
    }

    // Wipe Entire Database
    public static void WipeDatabase()
    {
        if (File.Exists(DBPath))
        {
            File.Delete(DBPath);
            Debug.LogWarning("<color=red>LiteDB:</color> CorrosiveWorld.db completely wiped.");
        }
    }
}