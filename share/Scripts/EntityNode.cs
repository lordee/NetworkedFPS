using Godot;
using System;

public class EntityNode : KinematicBody
{
    public Entity Entity;
    public MeshInstance MeshInstance;
    public Particles Particles;

    // Player Nodes
    public Spatial Head;
    public RayCast StairCatcher;

    static private string _resource = Util.GetResourceString("PlayerNode.tscn", RESOURCE.SCENE);

    static public EntityNode InstancePlayer()
    {
        PackedScene ps = ResourceLoader.Load(_resource) as PackedScene;
        EntityNode player = ps.Instance() as EntityNode;

        return player;
    }

    public void Init(string nodeName, ENTITYTYPE type, Client client)
    {
        MeshInstance = GetNodeOrNull("MeshInstance") as MeshInstance;
        Particles = GetNodeOrNull("Particles") as Particles;
        Name = nodeName;
        Entity = new Entity();
        Entity.EntityNode = this;

        Head = (Spatial)GetNodeOrNull("Head");
        StairCatcher = (RayCast)GetNodeOrNull("StairCatcher");

        if (type == ENTITYTYPE.PLAYER)
        {
            Name = client.NetworkID.ToString();
            Entity.InitPlayer(client);
        }
    }

    public void RotateHead(float rad)
    {
        Head.RotateY(rad);
        MeshInstance.RotateY(rad);
    }
}