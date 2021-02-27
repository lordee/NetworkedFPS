using Godot;
using System;

public class Body : KinematicBody
{
    public MOVETYPE MoveType;
    public Vector3 Velocity;
    public Entity BodyOwner;
    public bool TouchingGround = false;
    public float Acceleration;
    public float Deceleration;
    public bool OnLadder = false;
    public Spatial Head;
    public RayCast StairCatcher;
    public MeshInstance MeshInstance;

    

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public void Init(Entity owner)
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
