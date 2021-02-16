using Godot;
using System;

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
    public void ClientConnected(Player p)
    {

    }

    public void ClientDisconnected(Player p)
    {
        
    }

    public void PlayerPreFrame(Player p)
    {

    }

    public void PlayerPostFrame(Player p)
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

    public void WorldProcessItem(Spatial item, string func)
    {
        //invoke func
        GD.Print(func);
    }
}
