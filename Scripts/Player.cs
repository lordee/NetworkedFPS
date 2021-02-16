using Godot;
using System;

public class Player : Node
{
    public int NetworkID;
    public MOVETYPE MoveType;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public Player(string id)
    {
        NetworkID = Convert.ToInt32(id);
        Name = id;
    }

    public void Frame(float delta)
    {

    }
}
