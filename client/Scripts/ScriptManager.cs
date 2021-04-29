using Godot;
using System;
using MoonSharp.Interpreter;

public class ScriptManager : Node
{
    // client side scriptmanager
    static public MoonSharp.Interpreter.Script ScriptServer = new MoonSharp.Interpreter.Script();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    static public Table GetFieldExtensions()
    {
        return null;
    }

    public void PlayerSpawn(Entity player)
    {
        
    }

    public void PlayerPreFrame(Entity player)
    {
        
    }

    public void PlayerPostFrame(Entity player)
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

    public void WorldPreAddPlayer(Entity player)
    {

    }

    public void WorldPostAddPlayer(Entity player)
    {

    }

    public void WorldPreRemovePlayer(Entity player)
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
