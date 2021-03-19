using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class World : Node
{
    Node Players;

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

    // FIXME - replace with entity manager node
    public List<Entity> Entities = new List<Entity>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Players = GetNode("Players");
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

                PlayerSnap ps = new PlayerSnap();
                ps.Origin = org;
                ps.Velocity = velo;
                ps.NodeName = c.Player.PlayerNode.Name;
                ps.Rotation = rot;
                ps.CmdQueue = c.Player.pCmdQueue;
                sn.PlayerSnap.Add(ps);

                Player p = c.Player;
                p.Frame(delta);
            }
            Snapshots.Add(sn);

            while(Snapshots.Count > Main.World.BackRecTime / delta)
            {
                Snapshots.RemoveAt(0);
            }

            // move entities
            
        }
    }

    // TODO - generic for other ents
    public void MoveEntity(Body b, float delta)
    {
        MOVETYPE mt = b.BodyOwner.Player.MoveType;
        bool applyGrav = true;
        bool wishJump = false;

        if (b.BodyOwner is PlayerNode p)
        {
            wishJump = p.Player.WishJump;
            if (p.Player.OnLadder)
            {
                applyGrav = false;
            }
        }

        switch (mt)
        {
            case MOVETYPE.STEP:
                if (applyGrav)
                {
                    b.BodyOwner.Player.Velocity = ApplyGravity(b.BodyOwner.Player.Velocity, delta);
                }
                
                if (!wishJump)
                {
                    ApplyFriction(b, 1.0f, delta);
                }
                else
                {
                    ApplyFriction(b, 0, delta);

                    // FIXME - make more generic
                    if (b.BodyOwner is PlayerNode p2)
                    {
                        p2.Player.WishJump = false;
                    }
                }

                b.BodyOwner.Player.Velocity = b.MoveAndSlide(b.BodyOwner.Player.Velocity, this.Up);
                b.BodyOwner.Player.TouchingGround = b.IsOnFloor();
                break;
        }
    }
    
    private void ApplyFriction(Body body, float t, float delta)
    {
        Vector3 vec = body.BodyOwner.Player.Velocity;
        float speed;
        float newspeed;
        float control;
        float drop;

        vec.y = 0.0f;
        speed = vec.Length();
        drop = 0.0f;

        // Only if on the ground then apply friction
        if (body.BodyOwner.Player.TouchingGround)
        {
            control = speed < body.BodyOwner.Player.Deceleration ? body.BodyOwner.Player.Deceleration : speed;
            drop = control * _friction * delta * t;
        }

        newspeed = speed - drop;
        if(newspeed < 0)
            newspeed = 0;
        if(speed > 0)
            newspeed /= speed;

        body.BodyOwner.Player.Velocity.x *= newspeed;
        body.BodyOwner.Player.Velocity.z *= newspeed;
    }

    private Vector3 ApplyGravity(Vector3 velocity, float delta)
    {
        velocity.y -= _gravity * delta;
        return velocity;
    }

    public PlayerNode AddPlayer(Client c)
    {
        // add player to world node for each client
        Node n = GetNodeOrNull(c.NetworkID.ToString());
        if (n != null)
        {
            RemoveChild(n);
            n.Free();
        }

        PlayerNode pn = PlayerNode.Instance();
        
        Players.AddChild(pn);
        pn.Init(c);
        c.Player = pn.Player;

        return pn;
    }

    public void RemovePlayer(string id)
    {
        PlayerNode p = Players.GetNodeOrNull(id) as PlayerNode;
        if (p != null)
        {
            Main.ScriptManager.WorldPreRemovePlayer(p.Player);
            Players.RemoveChild(p);
            p.Free();
        }
    }

    public void LoadWorld()
    {
        // TODO - RemoveOldMapNode();
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
            ProcessWorldItem(ent);
        }

        Spatial triggers = mapInstance.GetNode("Triggers") as Spatial;
        Godot.Collections.Array triggerents = triggers.GetChildren();

        foreach(Spatial t in triggerents)
        {
            ProcessWorldItem(t);
        }
    }

    private void ProcessWorldItem(Spatial item)
    {
        Godot.Collections.Dictionary fields = item.Get("properties") as Godot.Collections.Dictionary;

        // FIXME - item contains properties, so integrate entity in that instead... Make qodot create entitynodes instead of spatials
        if (fields != null)
        {
            EntityNode en = new EntityNode();
            // FIXME - need entity manager, add this to  world for now even though it's under bsp
            AddChild(en);
            en.Init(item.Name);
            PropertyInfo[] entFields = typeof(Entity).GetProperties();
            foreach (PropertyInfo pi in entFields)
            {
                string fieldName = pi.Name.ToLower();
                if (fields.Contains(fieldName))
                {
                    pi.SetValue(en.Entity, fields[fieldName]);
                }
            }
            foreach(string field in Entity.MapCustomFieldDefs)
            {
                if (fields.Contains(field.ToLower()))
                {
                    en.Entity.MapFields.Add(field, fields[field.ToLower()].ToString());
                }
            }
            string cn = fields["classname"] != null ? (fields["classname"] as string).ToLower() : "";
            Entities.Add(en.Entity);
            Main.ScriptManager.WorldProcessItem(en, cn);
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
                PlayerNode brp = GetNodeOrNull(psn.NodeName) as PlayerNode;
                if (brp != null)
                {
                    Transform t = brp.Body.GlobalTransform;
                    t.origin = psn.Origin;
                    brp.Body.GlobalTransform = t;
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
            PlayerNode brp = GetNodeOrNull(psn.NodeName) as PlayerNode;
            if (brp != null)
            {
                Transform t = brp.Body.GlobalTransform;
                t.origin = psn.Origin;
                brp.Body.GlobalTransform = t;
            }
        }
    }
}
