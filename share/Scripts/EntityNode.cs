using Godot;
using System;

public class EntityNode : KinematicBody
{
    public Entity Entity;
    public MeshInstance MeshInstance;
    public Particles Particles;

    public void Init(string nodeName)
    {
        MeshInstance = GetNodeOrNull("MeshInstance") as MeshInstance;
        Particles = GetNodeOrNull("Particles") as Particles;
        Name = nodeName;
        Entity = new Entity();
        Entity.EntityNode = this;
    }
}