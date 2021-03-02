using Godot;
using System;

public class Body : KinematicBody
{
    
    public PlayerNode BodyOwner;
    public Spatial Head;
    public RayCast StairCatcher;
    public MeshInstance MeshInstance;  

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public void Init(PlayerNode owner)
    {
        BodyOwner = owner;
        Head = (Spatial)GetNode("Head");
        StairCatcher = (RayCast)GetNode("StairCatcher");
        MeshInstance = GetNode("MeshInstance") as MeshInstance;
    }

    public void RotateHead(float rad)
    {
        Head.RotateY(rad);
        MeshInstance.RotateY(rad);
    }
}
