using Godot;
using System;
using System.Reflection;
using MoonSharp.Interpreter;
using System.Collections.Generic;

public class ScriptManager : Node
{
    static public MoonSharp.Interpreter.Script ScriptServer = new MoonSharp.Interpreter.Script();
    DynValue luaFactFunction;
    DynValue luaClientConnected;
    DynValue luaClientDisconnected;
    DynValue luaPlayerSpawn;
    
    public override void _Ready()
    {
        LoadScripts();
    }

    private void LoadScripts()
    {
        MoonSharp.VsCodeDebugger.MoonSharpVsCodeDebugServer server = new MoonSharp.VsCodeDebugger.MoonSharpVsCodeDebugServer();
        
        // Start the debugger server
        server.Start();

        string sources = Util.GetLuaScriptString("sources.txt");
        string[] sourceLines = System.IO.File.ReadAllLines(sources);
        // load server scripts
        ScriptServer.Options.ScriptLoader = new MoonSharp.Interpreter.Loaders.FileSystemScriptLoader();
        ((MoonSharp.Interpreter.Loaders.ScriptLoaderBase)ScriptServer.Options.ScriptLoader).IgnoreLuaPathGlobal = true;
        ScriptServer.Options.UseLuaErrorLocations = true;
        ScriptServer.Options.DebugPrint = s => { GD.Print("{0}", s); };
        

        MoonSharp.Interpreter.Script.GlobalOptions
            .CustomConverters
            .SetScriptToClrCustomConversion(DataType.Table, typeof(IList<string>),
                v => {
                    List<string> ret = new List<string>();
                    foreach(var s in v.Table.Values)
                    {
                        ret.Add(s.ToString());
                    }
                    return ret;
                }
        );

        foreach (string line in sourceLines)
        {
            ScriptServer.DoFile(Util.GetLuaScriptString(line), null, line);
        }  

        // FIXME - figure out SetScriptToClrCustomConversion
        DynValue extensions = ScriptServer.Globals.Get("fieldExtensions");
        DynValue res = ScriptServer.Call(extensions);
        MoonSharp.Interpreter.Table t = res.Table;
        foreach(var s in t.Values)
        {
            Entity.CustomFieldDefs.Add(s.String, DynValue.NewString(""));
        }    
        
        luaFactFunction = ScriptServer.Globals.Get("fact");
        luaClientConnected = ScriptServer.Globals.Get("clientConnected");
        luaClientDisconnected = ScriptServer.Globals.Get("clientDisconnected");
        luaPlayerSpawn = ScriptServer.Globals.Get("playerSpawn");
        server.AttachToScript(ScriptServer, "main.lua");

        // c# functions
        ScriptServer.Globals["Print"] = (Action<string[]>)Builtins.Print;
        ScriptServer.Globals["BPrint"] = (Action<string[]>)Builtins.BPrint;

        // c# types to pass
        // FIXME - [MoonSharpVisible(false)] types
        UserData.RegisterType<Player>();
        UserData.RegisterType<Dictionary<string, DynValue>>();
    }

    // Player
    public void ClientConnected(Client c)
    {
        ScriptServer.Call(luaClientConnected, c.Player);
    }

    public void ClientDisconnected(Client c)
    {
        ScriptServer.Call(luaClientDisconnected, c.Player);
    }

    public void PlayerPreFrame(Player p)
    {

    }

    public void PlayerPostFrame(Player p)
    {

    }

    public void PlayerSpawn(Player player)
    {
        ScriptServer.Call(luaPlayerSpawn, player);
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

    public void WorldProcessItem(EntityNode item, string func)
    {
        //invoke func
        MethodInfo mi = this.GetType().GetMethod(func);
        if (mi != null)
        {
            mi.Invoke(this, new object[] { item.Entity });
        }
        else
        {
            GD.Print("Spawn function does not exist for " + func);
            // TODO - remove nodes from tree?
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

