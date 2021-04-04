using Godot;
using System;

[Serializable]
public class EntityState
{
    public int EntityID = -1;
    public int OwnerID = -1;
    public Transform GlobalTransform = new Transform();
    public MOVETYPE MoveType = MOVETYPE.NONE;
    public float MoveSpeed = 0;
    public uint CollisionMask = 1;
    public uint CollisionLayer = 1;
    public Vector3 Velocity = new Vector3();
}