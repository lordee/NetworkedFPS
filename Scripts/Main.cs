using Godot;
using System;

public class Main : Node
{
    static public Network Network;
    static public ScriptManager ScriptManager;
    static public World World;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Network = GetNode("Network") as Network;
        ScriptManager = GetNode("ScriptManager") as ScriptManager;
        World = GetNode("World") as World;

        // TODO - default config
        
        World.LoadWorld();
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
    public void Quit()
	{
		//Network.Disconnect();
		GetTree().Quit();
	}
}