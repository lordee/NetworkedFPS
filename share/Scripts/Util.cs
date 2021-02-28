using System;
using Godot;

public class Util
{
    static public string GetResourceString(string resourceName)
    {
        string resource = CreateResourceString(Main.GameDir, resourceName);
        if (!ResourceLoader.Exists(resource))
        {
            if (Main.GameDir != "SquadFortress")
            {
                resource = CreateResourceString("SquadFortress", resourceName);
            }
        }
        return resource;
    }

    static string CreateResourceString(string gameDir, string resourceName)
    {
        return "res://Mods/" + Main.GameDir + "/" + resourceName;
    }

    static public string GetLuaScriptString(string scriptName)
    {
        string loc = AppDomain.CurrentDomain.BaseDirectory + "/Mods/" + Main.GameDir + "/Scripts/" + scriptName;
        return loc;
    }
}