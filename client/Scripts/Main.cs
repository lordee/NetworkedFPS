using Godot;
using System;

public class Main : Node
{
    static public Network Network;
    static public ScriptManager ScriptManager;
    static public World World;
    static public Settings Settings;
    static public Commands Commands;
    static public PlayerController PlayerController;
    static public Client Client;
    static private Main self;
    static public SoundManager SoundManager;

    static public string GameDir = "SquadFortress";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Network = GetNode("Network") as Network;
        ScriptManager = GetNode("ScriptManager") as ScriptManager;
        World = GetNode("World") as World;
        SoundManager = GetNode("SoundManager") as SoundManager;
        self = this;
        Commands = new Commands();
        Settings = new Settings();
        Settings.LoadConfig();
        UIManager.OptionsMenu.Init(); // TODO - this shouldn't be needed... at least not here
    }

    public void Quit()
	{
		Network.Disconnect();
		GetTree().Quit();
	}

    static public void Exit()
    {
        self.Quit();
    }
}