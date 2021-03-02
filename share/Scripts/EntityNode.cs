using Godot;
using System;

public class EntityNode : Spatial
{
    public Entity Entity;

    public void Init(string nodeName)
    {
        Name = nodeName;
        Entity = new Entity();
        Entity.EntityNode = this;
    }
}