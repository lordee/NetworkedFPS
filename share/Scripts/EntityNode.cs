using Godot;
using System;

public class EntityNode : KinematicBody
{
    public Entity Entity;

    public void Init(string nodeName)
    {
        Name = nodeName;
        Entity = new Entity();
        Entity.EntityNode = this;
    }
}