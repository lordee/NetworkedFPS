using System.Collections.Generic;
using MoonSharp.Interpreter;
using Godot;
using System.Linq;

public class Entity
{
    [MoonSharpHidden]
    public int EntityID;

    public EntityNode EntityNode;
    public Entity Owner {
        get {
            return EntityNode.Entity;
        }
        set {
            EntityNode.RemoveCollisionExceptionWith(Owner.EntityNode);
            EntityNode.AddCollisionExceptionWith(value.EntityNode);
        }
    }
    
    // default fields that engine needs, modified by options etc
    public string NetName { get; set; }
    public string ClassName { get; set; }
    public MoonSharp.Interpreter.Table Fields;

    public MOVETYPE MoveType = MOVETYPE.NONE;
    public float MoveSpeed = 0;
    public Vector3 Velocity;

    public uint CollisionLayer {
        get { return EntityNode.CollisionLayer; }
        set { EntityNode.CollisionLayer = value; }
    }

    public uint CollisionMask {
        get { return EntityNode.CollisionMask; }
        set { EntityNode.CollisionMask = value; }
    }

    public Transform GlobalTransform {
        get { return EntityNode.GlobalTransform; }
        set { EntityNode.GlobalTransform = value; }
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