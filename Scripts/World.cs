using Godot;
using System;
using System.Collections;

public class World : Node
{
    private string _mapResource = "res://Maps/1on1r.tscn";
    private float _gameTime = 0f;
    public float FrameDelta = 0f;
    private int _serverSnapNum = 0;
    private bool _active = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

    public override void _PhysicsProcess(float delta)
    {
        if (_active)
        {
            FrameDelta = delta;
            _gameTime += delta;
            _serverSnapNum++;

            Main.ScriptManager.WorldStartFrame(this);

            // players
            foreach (Player p in Main.Network.Players)
            {
                Main.ScriptManager.PlayerPreFrame(p);
                p.Frame(delta);
                Main.ScriptManager.PlayerPostFrame(p);
            }
            
            
            // move entities


            
        }
    }

    public void LoadWorld()
    {
        Main.ScriptManager.WorldPreLoad(this);
        // TODO - Load world
        StartWorld();
        Main.ScriptManager.WorldPostLoad(this);
    }

    private void StartWorld()
    {
        PackedScene map = ResourceLoader.Load(_mapResource) as PackedScene;
        Spatial mapInstance = map.Instance() as Spatial;
        this.AddChild(mapInstance);
        mapInstance.Name = "Map";

        FrameDelta = 0f;
        _gameTime = 0f;
        _serverSnapNum = 0;
        _active = true;

        Spatial entitySpawns = mapInstance.GetNode("Entity Spawns") as Spatial;
        Godot.Collections.Array ents = entitySpawns.GetChildren();

        foreach(Spatial ent in ents)
        {
            ProcessWorldItem(ent);
        }

        Spatial triggers = mapInstance.GetNode("Triggers") as Spatial;
        Godot.Collections.Array triggerents = triggers.GetChildren();

        foreach(Spatial t in triggerents)
        {
            ProcessWorldItem(t);
        }
    }

    private void ProcessWorldItem(Spatial item)
    {
        Godot.Collections.Dictionary fields = item.Get("properties") as Godot.Collections.Dictionary;

        if (fields != null)
        {
            if (fields.Contains("classname"))
            {
                Main.ScriptManager.WorldProcessItem(item, fields["classname"].ToString().ToLower());
            }
        }
    }
}
