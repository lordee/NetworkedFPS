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
    DynValue luaPlayerPostFrame;
    DynValue luaWorldPostLoad;
    
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
            try
            {
                ScriptServer.DoFile(Util.GetLuaScriptString(line), null, line);
            }
            catch (Exception e)
            {
                GD.Print(e);
            }
        }

        MoonSharp.Interpreter.Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table, typeof(Vector3),
            dynVal => {
                Table table = dynVal.Table;
                float x = (float)((Double)table[1]);
                float y = (float)((Double)table[2]);
                float z = (float)((Double)table[3]);
                return new Vector3(x, y, z);
            }
        );

        // c# types to pass
        UserData.RegisterType<Entity>();
        UserData.RegisterType<Godot.Vector3>();
        UserData.RegisterType<Godot.Transform>();
        UserData.RegisterType<Entity>();
        UserData.RegisterType<List<Entity>>();

        DynValue extensions = ScriptServer.Globals.Get("FieldExtensions");
        DynValue res = ScriptServer.Call(extensions);
        MoonSharp.Interpreter.Table t = res.Table;
        Entity.Fields2 = t;
        foreach(var s in t.Pairs)
        {
            Entity.MapCustomFieldDefs.Add(s.Key.String);
        }   
        
        luaClientConnected = ScriptServer.Globals.Get("ClientConnected");
        luaClientDisconnected = ScriptServer.Globals.Get("ClientDisconnected");
        luaPlayerSpawn = ScriptServer.Globals.Get("PlayerSpawn");
        luaProcessEntity = ScriptServer.Globals.Get("ProcessEntity");
        luaPlayerPostFrame = ScriptServer.Globals.Get("PlayerPostFrame");
        luaWorldPostLoad = ScriptServer.Globals.Get("WorldPostLoad");
        server.AttachToScript(ScriptServer, "main.lua");

        // c# functions
        ScriptServer.Globals["Print"] = (Action<string[]>)Builtins.Print;
        ScriptServer.Globals["BPrint"] = (Action<string[]>)Builtins.BPrint;
        ScriptServer.Globals["Find"] = (Func<Entity, string, string, Entity>)Builtins.Find;
        ScriptServer.Globals["Time"] = (Func<float>)Builtins.Time;
        ScriptServer.Globals["BSound"] = (Action<Vector3, string>)Builtins.BSound;
        ScriptServer.Globals["Remove"] = (Action<Entity>)Builtins.Remove;
        ScriptServer.Globals["Spawn"] = (Func<string, Entity>)Builtins.Spawn;
        ScriptServer.Globals["Normalise"] = (Func<Vector3, Vector3>)Builtins.Normalise;
        ScriptServer.Globals["Precache"] = (Action<string, RESOURCE>)Builtins.Precache;
        ScriptServer.Globals["Precache_Sound"] = (Action<string>)Builtins.Precache_Sound;
        ScriptServer.Globals["Precache_Scene"] = (Action<string>)Builtins.Precache_Scene;
        ScriptServer.Globals["FindRadius"] = (Func<Vector3, float, List<Entity>>)Builtins.FindRadius;
        ScriptServer.Globals["VLen"] = (Func<Vector3, Vector3, float>)Builtins.VLen;
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

    public void PlayerPreFrame(Entity player)
    {

    }

    public void PlayerPostFrame(Entity player)
    {
        ScriptServer.Call(luaPlayerPostFrame, player);
    }

    public void PlayerSpawn(Entity player)
    {
        ScriptServer.Call(luaPlayerSpawn, player);
    }

    // Entities
    public void EntityTouch(Entity entity, KinematicCollision collision)
    {
        if (entity.TouchFunc != null)
        {
            Entity other = null;
            if (collision.Collider is EntityNode en)
            {
                other = en.Entity;
            }
            ScriptServer.Call(entity.TouchFunc, entity, other);
        }
    }

    public void EntityThink(Entity entity)
    {
        if (entity.ThinkFunc != null)
        {
            ScriptServer.Call(entity.ThinkFunc, entity);
        }
    }

    // World
    public void WorldPreLoad(World w)
    {

    }

    public void WorldPostLoad(World w)
    {
        ScriptServer.Call(luaWorldPostLoad);
    }

    public void WorldStartFrame(World w)
    {

    }

    // covered on server by client disconnected
    public void WorldPreRemovePlayer(Entity player)
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

