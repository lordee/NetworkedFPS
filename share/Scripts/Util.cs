using System;
using Godot;

public class Util
{
    static public string GetResourceString(string resourceName, RESOURCE type)
    {
        string resource = CreateResourceString(Main.GameDir, resourceName, type);
        if (!ResourceLoader.Exists(resource))
        {
            if (Main.GameDir != "SquadFortress")
            {
                resource = CreateResourceString("SquadFortress", resourceName, type);
            }
            // FIXME error out
        }
        return resource;
    }

    static string CreateResourceString(string gameDir, string resourceName, RESOURCE type)
    {
        string res = "res://Mods/" + Main.GameDir + "/";

        switch (type)
        {
            case RESOURCE.SCENE:
                res += "Scenes/";
                break;
            case RESOURCE.SOUND:
                res += "Assets/Sounds/";
                break;
            case RESOURCE.MAP:
                res += "Maps/";
                break;
        }
        res += resourceName;

        return res;
    }

    static public string GetLuaScriptString(string scriptName)
    {
        string loc = AppDomain.CurrentDomain.BaseDirectory + "/Mods/" + Main.GameDir + "/Scripts/" + scriptName;
        return loc;
    }
}