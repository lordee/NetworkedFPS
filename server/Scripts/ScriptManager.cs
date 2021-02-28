using Godot;
using System;
using System.Reflection;
using MoonSharp.Interpreter;

public class ScriptManager : Node
{
    MoonSharp.Interpreter.Script script = new MoonSharp.Interpreter.Script();
    DynValue luaFactFunction;
    DynValue luaClientConnected;
    
    public override void _Ready()
    {
        LoadScripts();
    }

    private void LoadScripts()
    {
        string sources = Util.GetLuaScriptString("sources.txt");
        string[] sourceLines = System.IO.File.ReadAllLines(sources);
        // load server scripts
        script.Options.ScriptLoader = new MoonSharp.Interpreter.Loaders.FileSystemScriptLoader();
        ((MoonSharp.Interpreter.Loaders.ScriptLoaderBase)script.Options.ScriptLoader).IgnoreLuaPathGlobal = true;
        script.Options.UseLuaErrorLocations = true;
        script.Options.DebugPrint = s => { GD.Print("{0}", s); };

        foreach (string line in sourceLines)
        {
            script.DoFile(Util.GetLuaScriptString(line), null, line);
        }
        
        luaFactFunction = script.Globals.Get("fact");
        luaClientConnected = script.Globals.Get("clientconnected");

        // c# functions
        script.Globals["Print"] = (Action<string>)Print;

        // c# types to pass
        // FIXME - [MoonSharpVisible(false)] types
        UserData.RegisterType<Player>();
    }

    private void Print(string s)
    {
        GD.Print(s);
    }

    // Player
    public void ClientConnected(Client c)
    {
        script.Call(luaClientConnected, c.Player);
    }

    public void ClientDisconnected(Client c)
    {
        
    }

    public void PlayerPreFrame(Player p)
    {

    }

    public void PlayerPostFrame(Player p)
    {

    }

    public void PlayerSpawn(Player p)
    {
        
    }

    public void PlayerAttack(Player player)
    {
        
    }

    // World
    public void WorldPreLoad(World w)
    {

    }

    public void WorldPostLoad(World w)
    {
        
    }

    public void WorldStartFrame(World w)
    {

    }

    public void WorldPostAddPlayer(Player p)
    {

    }

    // covered on server by client disconnected
    public void WorldPreRemovePlayer(Player player)
    {
        
    }

    public void WorldProcessItem(Entity item, string func)
    {
        //invoke func
        MethodInfo mi = this.GetType().GetMethod(func);
        if (mi != null)
        {
            mi.Invoke(this, new object[] { item });
        }
        else
        {
            GD.Print("Spawn function does not exist for " + func);
        }
    }

    // ent constructors
    public void info_tfgoal(Entity item)
    {
        GD.Print(item);
    }

    public void info_player_teamspawn(Entity item)
    {
        GD.Print(item);
    }

    public void info_tfdetect(Entity item)
    {
        GD.Print(item);
    }

    public void info_player_start(Entity item)
    {
        GD.Print(item);
    }

}

