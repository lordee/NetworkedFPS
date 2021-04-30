using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class World : Node
{
    Node PlayerNodes;
    public EntityManager EntityManager;

    private string _mapResource = Util.GetResourceString("1on1r.tscn", RESOURCE.MAP);
    public string MapName = "";
    public float GameTime = 0f;
    public float FrameDelta = 0f;
    private bool _active = false;
    public int ServerSnapshot;
    public int LocalSnapshot;

    private float _gravity = 80f;
    public float Gravity { get { return _gravity; }}
    private Vector3 _up = new Vector3(0,1,0);
    public Vector3 Up { get { return _up; }}
    private float _friction = 6;
    private float _backRecTime = 80f;
    public float BackRecTime { get { return _backRecTime; }}

    public List<GameState> GameStates = new List<GameState>();

    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        PlayerNodes = GetNode("Players");
        EntityManager = GetNode("EntityManager") as EntityManager;
    }

    public override void _Process(float delta)
    {
        
    }

    public override void _PhysicsProcess(float delta)
    {
        if (_active)
        {
            FrameDelta = delta;
            GameTime += delta;
            LocalSnapshot++;

            if (IsNetworkMaster())
            {
                ServerSnapshot = LocalSnapshot;
            }
            
            Main.ScriptManager.WorldStartFrame(this);

            // add gamestates
            // FIXME - server only?
            GameState gs = new GameState();
            gs.SnapShotNumber = Main.World.LocalSnapshot;

            foreach (Entity entity in EntityManager.Entities)
            {
                if (entity.EntityNode == null || entity.EntityNode.NativeInstance == IntPtr.Zero)
                {
                    continue;
                }

                EntityState es = EntityManager.GenerateEntityState(entity);
                gs.EntityStates.Add(es);

                if (entity.EntityType == ENTITYTYPE.PLAYER)
                {
                    entity.Frame(delta);
                }

                MoveEntity(entity.EntityNode, delta);

                if (entity.EntityType == ENTITYTYPE.PLAYER)
                {
                    entity.PostFrame();
                }
                else if (entity.NextThink != 0 && entity.NextThink <= Main.World.GameTime)
                {
                    Main.ScriptManager.EntityThink(entity);
                }
            }

            // process spawning collection
            EntityManager.Entities.AddRange(EntityManager.SpawnedEntityQueue);
            EntityManager.SpawnedEntityQueue.Clear();

            GameStates.Add(gs);
            if (GameStates.Count > 32) // arbitrary, maybe do Main.World.BackRecTime / delta
            {
                GameStates.RemoveAt(0);
            }
            
            foreach (Entity entity in EntityManager.RemoveEntityQueue)
            {
                entity.EntityNode.GetParent().RemoveChild(entity.EntityNode);
                entity.EntityNode.QueueFree();
                EntityManager.Entities.Remove(entity);
            }

            EntityManager.RemoveEntityQueue.Clear();
        }
    }

    public void MoveEntity(EntityNode entityNode, float delta)
    {
        MOVETYPE mt = entityNode.Entity.MoveType;
        bool applyGrav = true;
        bool wishJump = false;

        if (entityNode.Entity.EntityType == ENTITYTYPE.PLAYER)
        {
            wishJump = entityNode.Entity.WishJump;
            if (entityNode.Entity.OnLadder)
            {
                applyGrav = false;
            }
        }

        switch (mt)
        {
            case MOVETYPE.STEP:
                if (applyGrav)
                {
                    entityNode.Entity.Velocity = ApplyGravity(entityNode.Entity.Velocity, delta);
                }
                
                if (!wishJump)
                {
                    ApplyFriction(entityNode, 1.0f, delta);
                }
                else
                {
                    ApplyFriction(entityNode, 0, delta);

                    if (entityNode.Entity.EntityType == ENTITYTYPE.PLAYER)
                    {
                        entityNode.Entity.WishJump = false;
                    }
                }
                
                entityNode.Entity.Velocity = entityNode.MoveAndSlide(entityNode.Entity.Velocity, this.Up);
                entityNode.Entity.TouchingGround = entityNode.IsOnFloor();
                break;
            case MOVETYPE.MISSILE:
                Vector3 motion = new Vector3();
                entityNode.Entity.Velocity = -entityNode.Entity.GlobalTransform.basis.z.Normalized() * entityNode.Entity.MoveSpeed;
                motion = entityNode.Entity.Velocity * delta;
                KinematicCollision c = entityNode.MoveAndCollide(motion);
                if (c != null)
                {
                    Main.ScriptManager.EntityTouch(entityNode.Entity, c);
                }
                break;
        }
    }
    
    private void ApplyFriction(EntityNode body, float t, float delta)
    {
        Vector3 vec = body.Entity.Velocity;
        float speed;
        float newspeed;
        float control;
        float drop;

        vec.y = 0.0f;
        speed = vec.Length();
        drop = 0.0f;

        // Only if on the ground then apply friction
        if (body.Entity.TouchingGround)
        {
            control = speed < body.Entity.Deceleration ? body.Entity.Deceleration : speed;
            drop = control * _friction * delta * t;
        }

        newspeed = speed - drop;
        if(newspeed < 0)
            newspeed = 0;
        if(speed > 0)
            newspeed /= speed;

        Vector3 vel = body.Entity.Velocity;
        vel.x *= newspeed;
        vel.z *= newspeed;
        body.Entity.Velocity = vel;
    }

    private Vector3 ApplyGravity(Vector3 velocity, float delta)
    {
        velocity.y -= _gravity * delta;
        return velocity;
    }

    public EntityNode AddPlayer(Client c)
    {
        // add player to world node for each client
        Node n = PlayerNodes.GetNodeOrNull(c.NetworkID.ToString());
        if (n != null)
        {
            PlayerNodes.RemoveChild(n);
            n.Free();
        }

        EntityNode pn = EntityNode.InstancePlayer();
        
        PlayerNodes.AddChild(pn);
        pn.Init(c.NetworkID.ToString(), ENTITYTYPE.PLAYER, c);
        EntityManager.Entities.Add(pn.Entity);
        c.Player = pn.Entity;

        return pn;
    }

    public void RemovePlayer(string id)
    {
        EntityNode p = PlayerNodes.GetNodeOrNull(id) as EntityNode;
        if (p != null)
        {
            Main.ScriptManager.WorldPreRemovePlayer(p.Entity);
            EntityManager.Entities.Remove(p.Entity);
            PlayerNodes.RemoveChild(p);
            p.Free();
        }
    }

    public void LoadWorld()
    {
        // TODO - RemoveOldMapNode();
        EntityManager.Resources.Clear();
        EntityManager.Entities.Clear();
        Main.ScriptManager.WorldPreLoad(this);
        StartWorld();
        foreach (Client c in Main.Network.Clients)
        {
            AddPlayer(c);
        }
        Main.ScriptManager.WorldPostLoad(this);
    }

    public void ChangeMap(string mapResource)
    {
        _mapResource = mapResource;
        LoadWorld();
    }

    private void StartWorld()
    {
        PackedScene map = ResourceLoader.Load(_mapResource) as PackedScene;
        MapName = _mapResource;
        Spatial mapInstance = map.Instance() as Spatial;
        this.AddChild(mapInstance);
        mapInstance.Name = "Map";

        FrameDelta = 0f;
        GameTime = 0f;
        LocalSnapshot = 0;
        _active = true;

        if (IsNetworkMaster())
        {
            Spatial entitySpawns = mapInstance.GetNode("Entity Spawns") as Spatial;
            Godot.Collections.Array ents = entitySpawns.GetChildren();

            foreach(Spatial ent in ents)
            {
                EntityManager.ProcessWorldItem(ent);
            }

            Spatial triggers = mapInstance.GetNode("Triggers") as Spatial;
            Godot.Collections.Array triggerents = triggers.GetChildren();

            foreach(Spatial t in triggerents)
            {
                EntityManager.ProcessWorldItem(t);
            }
        }
    }

    public bool RewindPlayers(int ticks, float delta)
    {
        bool rewound = false;

        ticks = ticks > GameStates.Count ? GameStates.Count : ticks; // FIXME - no longer true - we only hold backrectime worth of ticks
        if (ticks > 0)
        {
            int pos = GameStates.Count - ticks;
            GameState gs = GameStates[pos];
            foreach (EntityState es in gs.EntityStates)
            {
                if (es.EntityType == ENTITYTYPE.PLAYER)
                {
                    EntityNode brp = EntityManager.GetEntityByID(es.EntityID).EntityNode;
                    if (brp != null)
                    {
                        Transform t = brp.GlobalTransform;
                        t.origin = es.GlobalTransform.origin;
                        brp.GlobalTransform = t;
                    }
                }
            }
            rewound = true;
        }

        return rewound;
    }

    public void FastForwardPlayers()
    {
        GameState gs = GameStates[GameStates.Count-1];
        foreach (EntityState es in gs.EntityStates)
        {
            if (es.EntityType == ENTITYTYPE.PLAYER)
            {
                Entity ent = EntityManager.GetEntityByID(es.EntityID);
                EntityNode brp = ent.EntityNode;
                if (brp != null)
                {
                    Transform t = brp.GlobalTransform;
                    t.origin = es.GlobalTransform.origin;
                    brp.GlobalTransform = t;
                }
            }
        }
    }
}
