using Godot;
using System;

public class ScriptManager : Node
{
    // client side scriptmanager
    static public MoonSharp.Interpreter.Script ScriptServer = new MoonSharp.Interpreter.Script();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public void PlayerSpawn(Player player)
    {
        
    }

    public void PlayerPreFrame(Player player)
    {
        
    }

    public void PlayerPostFrame(Player player)
    {
        
    }

    // Entities
    public void EntityTouch(Entity entity, KinematicCollision collision)
    {

    }

    public void EntityThink(Entity entity)
    {

    }

    // world

    public void WorldPreAddPlayer(Player player)
    {

    }

    public void WorldPostAddPlayer(Player player)
    {

    }

    public void WorldPreRemovePlayer(Player player)
    {
        
    }

    public void WorldStartFrame(World world)
    {

    }

    public void WorldPreLoad(World world)
    {

    }

    public void WorldPostLoad(World world)
    {

    }

    public void WorldProcessItem(EntityNode entityNode, string className)
    {

    }
}
