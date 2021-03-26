using Godot;
using System;

public class EntityNode : KinematicBody
{
    public Entity Entity;
    public MeshInstance MeshInstance;

    public void Init(string nodeName)
    {
        MeshInstance = GetNodeOrNull("MeshInstance") as MeshInstance;
        Name = nodeName;
        Entity = new Entity();
        Entity.EntityNode = this;
    }
}