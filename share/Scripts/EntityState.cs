using Godot;
using System;

[Serializable]
public class EntityState
{
    public ushort EntityID = 0;
    public int OwnerID = -1;
    public ENTITYTYPE EntityType = ENTITYTYPE.NONE;
    public Transform GlobalTransform = new Transform();
    public MOVETYPE MoveType = MOVETYPE.NONE;
    public float MoveSpeed = 0;
    public uint CollisionMask = 1;
    public uint CollisionLayer = 1;
    public Vector3 Velocity = new Vector3();
    public bool Emitting = false;
    public Vector3 ViewOffset = new Vector3();
    public float Ping = 0;
    public float CurrentHealth = 0;
    public float CurrentArmour = 0;

}