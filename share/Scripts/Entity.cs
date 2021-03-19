using System.Collections.Generic;
using MoonSharp.Interpreter;
using Godot;
using System.Linq;

public class Entity
{
    public EntityNode EntityNode;
    
    // default fields that engine needs, modified by options etc
    public string NetName { get; set; }
    public string ClassName { get; set; }
    public MoonSharp.Interpreter.Table Fields;

    // FIXME - testing, incorporate in to fields later
    public Dictionary<string, string> MapFields = new Dictionary<string, string>();

    private Vector3 _origin;
    // FIXME - handle this in entitynode instead?
    public Vector3 Origin { 
        get {
            return _origin;
        }
        set {
            Transform t = EntityNode.GlobalTransform;
            t.origin = value;
            EntityNode.GlobalTransform = t;
            _origin = value;
        }
    }

    static public List<string> MapCustomFieldDefs = new List<string>();
    static public MoonSharp.Interpreter.Table Fields2;
    public Entity()
    {
        if (Main.Network.IsNetworkMaster())
        {
            //Fields = new Table(ScriptManager.ScriptServer);
            Fields = Fields2;
        }
    }
}