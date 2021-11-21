using Godot;
using System;

public class EntityNode : Node
{
    public Entity Entity;
    public KinematicBody KinematicBody;
    public CollisionShape KBCS;
    public MeshInstance MeshInstance;
    public Vector3 MeshRotation;
    public Vector3 KinematicBodyRotation;
    public Particles Particles;

    // Player Nodes
    public Spatial Head;
    public RayCast StairCatcher;

    static private string _resource = Util.GetResourceString("PlayerNode.tscn", RESOURCE.SCENE);

    public override void _Ready()
    {
        Particles = GetNodeOrNull("Particles") as Particles;
        KinematicBody = GetNodeOrNull("KinematicBody") as KinematicBody;
        if (KinematicBody != null)
        {
            KinematicBodyRotation = KinematicBody.RotationDegrees;
            KBCS = KinematicBody.GetNodeOrNull("CollisionShape") as CollisionShape;
            StairCatcher = KinematicBody.GetNodeOrNull("StairCatcher") as RayCast;
            MeshInstance = KinematicBody.GetNodeOrNull("MeshInstance") as MeshInstance;
            if (MeshInstance != null)
            {
                MeshRotation = MeshInstance.RotationDegrees;
                Head = MeshInstance.GetNodeOrNull("Head") as Spatial;
            }
        }
    }

    static public EntityNode InstancePlayer()
    {
        PackedScene ps = ResourceLoader.Load(_resource) as PackedScene;
        EntityNode player = ps.Instance() as EntityNode;

        return player;
    }

    public void Init(string nodeName, ENTITYTYPE type, Client client)
    {
        Name = nodeName;
        Entity = new Entity();
        Entity.EntityNode = this;

        if (type == ENTITYTYPE.PLAYER)
        {
            Name = client.NetworkID.ToString();
            Entity.InitPlayer(client);
        }
    }

    // TODO - refactor
    public void DefaultSceneRotate()
    {
        if (KinematicBody != null)
        {
            if (KinematicBodyRotation.y != 0)
            {
                //KinematicBody.RotateX(Mathf.Deg2Rad(KinematicBodyRotation.x));
                //KinematicBody.RotateY(Mathf.Deg2Rad(KinematicBodyRotation.y));
                //KinematicBody.RotateZ(Mathf.Deg2Rad(KinematicBodyRotation.z));
            }

        }
        if (MeshInstance != null)
        {
            if (MeshRotation.y != 0)
            {
                MeshInstance.RotateY(Mathf.Deg2Rad(MeshRotation.y));
            }
        }
    }

    public void RotateHead(float rad)
    {
        Head.RotateY(rad);
        MeshInstance.RotateY(rad);
    }
}