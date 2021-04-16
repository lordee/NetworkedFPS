using System.Collections.Generic;
using MoonSharp.Interpreter;
using Godot;
using System.Linq;
using System;

public class Entity
{
    [MoonSharpHidden]
    public UInt16 EntityID;
    [MoonSharpHidden]
    public EntityNode EntityNode;

    

    // default fields that engine needs, modified by options etc

    [MoonSharpHidden]
    public Dictionary<int, int> EntityChanges = new Dictionary<int, int>();
    
    [MoonSharpHidden]
    private Entity _owner;
    public Entity Owner {
        get {
            return _owner;
        }
        set {
            if (_owner != value)
            {
                if (_owner != null)
                {
                    EntityNode.RemoveCollisionExceptionWith(_owner.EntityNode);
                }
                
                EntityNode.AddCollisionExceptionWith(value.EntityNode);
                _owner = value;
            }
        }
    }
    
    public string NetName { get; set; }
    public string ClassName { get; set; }
    public MoonSharp.Interpreter.Table Fields;

    public MOVETYPE MoveType = MOVETYPE.NONE;

    public float MoveSpeed;
    public Vector3 Velocity;

    public bool Emitting {
        get { return EntityNode.Particles != null ? EntityNode.Particles.Emitting : false; }
        set {
            if (EntityNode.Particles != null)
            {
                EntityNode.Particles.Emitting = value;
            }
        }
    }

    public uint CollisionLayer {
        get { return EntityNode.CollisionLayer; }
        set { 
            if (EntityNode.CollisionLayer != value)
            {
                EntityNode.CollisionLayer = value;
            }
        }
    }

    public uint CollisionMask {
        get { return EntityNode.CollisionMask; }
        set { 
            if (EntityNode.CollisionMask != value)
            {
                EntityNode.CollisionMask = value;
            } 
        }
    }

    public Transform GlobalTransform {
        get { return EntityNode.GlobalTransform; }
        set { 
            if (EntityNode.GlobalTransform != value)
            {
                EntityNode.GlobalTransform = value;
            }
        }
    }

    public Vector3 Origin { 
        get {
            return EntityNode.GlobalTransform.origin;
        }
        set {
            Transform t = EntityNode.GlobalTransform;
            t.origin = value;
            EntityNode.GlobalTransform = t;
        }
    }
    public bool TouchingGround = false;
    public float Acceleration = 14.0f;
    public float Deceleration = 10.0f;
    
    [MoonSharpHidden]
    public DynValue TouchFunc;
    public string Touch {
        get { return TouchFunc.String; }
        set {
            TouchFunc = ScriptManager.ScriptServer.Globals.Get(value);
        }
    }

    public float NextThink = 0;
    [MoonSharpHidden]
    public DynValue ThinkFunc;
    public string Think {
        get { return ThinkFunc.String; }
        set {
            ThinkFunc = ScriptManager.ScriptServer.Globals.Get(value);
        }
    }

    // FIXME - testing, incorporate in to fields later
    public Dictionary<string, string> MapFields = new Dictionary<string, string>();

    static public List<string> MapCustomFieldDefs = new List<string>();
    static public MoonSharp.Interpreter.Table Fields2;
    public Entity()
    {
        if (Main.Network.IsNetworkMaster())
        {
            Fields = Fields2;
            EntityID = Main.World.EntityManager.GetEntityID();
        }
    }
}