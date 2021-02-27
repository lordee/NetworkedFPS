using Godot;
using System;
using System.Reflection;

// eventually replace with bytecode

public class ScriptManager : Node
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        LoadScripts();
    }

    private void LoadScripts()
    {
        // load client script
    }

    // Player
    public void ClientConnected(Client c)
    {

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
