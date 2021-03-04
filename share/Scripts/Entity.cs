using System.Collections.Generic;
using MoonSharp.Interpreter;


public class Entity
{
    public EntityNode EntityNode;
    public Dictionary<string, DynValue> Fields;
    
    // default fields that engine needs, modified by options etc
    public string NetName = "";
    public string ClassName = "";

    static public Dictionary<string, DynValue> CustomFieldDefs = new Dictionary<string, DynValue>();

    public Entity()
    {
        Fields = new Dictionary<string, DynValue>(CustomFieldDefs); // TODO - is this deep copy?
    }
}