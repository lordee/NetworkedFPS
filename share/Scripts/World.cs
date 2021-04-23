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

    public List<Snapshot> Snapshots = new List<Snapshot>();
    public List<GameState> GameStates = new List<GameState>();

    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        PlayerNodes = GetNode("Players");
        EntityManager = GetNode("EntityManager") as EntityManager;
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

            Snapshot sn = new Snapshot();
            sn.SnapNum = Main.World.LocalSnapshot;
            // players
            foreach (Client c in Main.Network.Clients)
            {
                if (c.Player == null)
                {
                    continue;
                }
                Vector3 org = c.Player.ServerState.Origin;
                Vector3 velo = c.Player.ServerState.Velocity;
                Vector3 rot = c.Player.ServerState.Rotation;

                // FIXME - change to gamestate etc
                PlayerSnap ps = new PlayerSnap();
                ps.Origin = org;
                ps.Velocity = velo;
                ps.NodeName = c.Player.EntityNode.Name;
                ps.Rotation = rot;
                ps.CmdQueue = c.Player.pCmdQueue;
                sn.PlayerSnap.Add(ps);

                Entity p = c.Player;
                p.Frame(delta);
            }
            Snapshots.Add(sn);

            while(Snapshots.Count > Main.World.BackRecTime / delta)
            {
                Snapshots.RemoveAt(0);
            }

            // add gamestates
            // FIXME - server only?
            GameState gs = new GameState();
            gs.SnapShotNumber = Main.World.LocalSnapshot;

            foreach (Entity entity in EntityManager.Entities)
            {
                MoveEntity(entity.EntityNode, delta);

                if (entity.NextThink != 0 && entity.NextThink <= Main.World.GameTime)
                {
                    Main.ScriptManager.EntityThink(entity);
                }

                EntityState es = new EntityState();
                es.EntityID = entity.EntityID;
                if (entity.Owner != null)
                {
                    es.OwnerID = entity.Owner.EntityID;
                }
                es.GlobalTransform = entity.GlobalTransform;
                es.MoveType = entity.MoveType;
                es.MoveSpeed = entity.MoveSpeed;
                es.CollisionLayer = entity.CollisionLayer;
                es.CollisionMask = entity.CollisionMask;
                es.Emitting = entity.Emitting;
                gs.EntityStates.Add(es);
            }

            // process spawning collection
            EntityManager.Entities.AddRange(EntityManager.SpawnedEntityQueue);
            EntityManager.SpawnedEntityQueue.Clear();

            GameStates.Add(gs);
            if (GameStates.Count > 32) // arbitrary
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
        EntityManager.Players.Add(pn.Entity);
        c.Player = pn.Entity;

        return pn;
    }

    public void RemovePlayer(string id)
    {
        EntityNode p = PlayerNodes.GetNodeOrNull(id) as EntityNode;
        if (p != null)
        {
            Main.ScriptManager.WorldPreRemovePlayer(p.Entity);
            EntityManager.Players.Remove(p.Entity);
            PlayerNodes.RemoveChild(p);
            p.Free();
        }
    }

    public void LoadWorld()
    {
        // TODO - RemoveOldMapNode();
        EntityManager.Resources.Clear();
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

    public bool RewindPlayers(int ticks, float delta)
    {
        bool rewound = false;

        ticks = ticks > Snapshots.Count ? Snapshots.Count : ticks; // we only hold backrectime worth of ticks
        if (ticks > 0)
        {
            int pos = Snapshots.Count - ticks;
            Snapshot sn = Snapshots[pos];
            foreach(PlayerSnap psn in sn.PlayerSnap)
            {
                EntityNode brp = GetNodeOrNull(psn.NodeName) as EntityNode;
                if (brp != null)
                {
                    Transform t = brp.GlobalTransform;
                    t.origin = psn.Origin;
                    brp.GlobalTransform = t;
                }
            }
            rewound = true;
        }

        return rewound;
    }

    public void FastForwardPlayers()
    {
        Snapshot sn = Snapshots[Snapshots.Count - 1];
        foreach(PlayerSnap psn in sn.PlayerSnap)
        {
            EntityNode brp = GetNodeOrNull(psn.NodeName) as EntityNode;
            if (brp != null)
            {
                Transform t = brp.GlobalTransform;
                t.origin = psn.Origin;
                brp.GlobalTransform = t;
            }
        }
    }
}
