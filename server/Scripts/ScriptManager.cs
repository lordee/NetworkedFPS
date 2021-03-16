using Godot;
using System;
using System.Reflection;
using MoonSharp.Interpreter;
using System.Collections.Generic;

public class ScriptManager : Node
{
    static public MoonSharp.Interpreter.Script ScriptServer = new MoonSharp.Interpreter.Script();
    DynValue luaClientConnected;
    DynValue luaClientDisconnected;
    DynValue luaPlayerSpawn;
    DynValue luaProcessEntity;

    
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

        foreach (string line in sourceLines)
        {
            ScriptServer.DoFile(Util.GetLuaScriptString(line), null, line);
        }  

        // c# types to pass
        UserData.RegisterType<Player>();
        UserData.RegisterType<Godot.Vector3>();
        UserData.RegisterType<Entity>();

        DynValue extensions = ScriptServer.Globals.Get("FieldExtensions");
        DynValue res = ScriptServer.Call(extensions);
        MoonSharp.Interpreter.Table t = res.Table;
        foreach(var s in t.Values)
        {
            Entity.MapCustomFieldDefs.Add(s.String);
        }   
        
        luaClientConnected = ScriptServer.Globals.Get("ClientConnected");
        luaClientDisconnected = ScriptServer.Globals.Get("ClientDisconnected");
        luaPlayerSpawn = ScriptServer.Globals.Get("PlayerSpawn");
        luaProcessEntity = ScriptServer.Globals.Get("ProcessEntity");
        server.AttachToScript(ScriptServer, "main.lua");

        // c# functions
        ScriptServer.Globals["Print"] = (Action<string[]>)Builtins.Print;
        ScriptServer.Globals["BPrint"] = (Action<string[]>)Builtins.BPrint;
        ScriptServer.Globals["Find"] = (Func<Entity, string, string, Entity>)Builtins.Find;
    }

    // Player
    public void ClientConnected(Client c)
    {
        ScriptServer.Call(luaClientConnected, c.Player);
        PlayerSpawn(c.Player);
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

    // covered on server by client disconnected
    public void WorldPreRemovePlayer(Player player)
    {
        
    }

    public void WorldProcessItem(EntityNode item, string func)
    {
        //invoke func
        var scriptMethod = ScriptServer.Globals.Get(func);

        if (scriptMethod.Type == DataType.Nil)
        {
            Builtins.BPrint("Spawn function does not exist: ", func);
        }
        else
        {
            ScriptServer.Call(luaProcessEntity, item.Entity);
            ScriptServer.Call(scriptMethod, item.Entity);
        }
    }
}

