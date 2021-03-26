using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class EntityManager : Node
{

    // FIXME - replace with entity manager node
    public List<Entity> Entities = new List<Entity>();
    public List<Entity> RemoveEntityQueue = new List<Entity>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public void ProcessWorldItem(Spatial item)
    {
        Godot.Collections.Dictionary fields = item.Get("properties") as Godot.Collections.Dictionary;

        // FIXME - item contains properties, so integrate entity in that instead... Make qodot create entitynodes instead of spatials
        if (fields != null)
        {
            EntityNode en = new EntityNode();
            en.CollisionLayer = 0;
            en.CollisionMask = 0;
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

    public void RemoveEntity(Entity entity)
    {
        RemoveEntityQueue.Add(entity);
    }

    public Entity GetEntityByNodeName(string nodeName)
    {
        return Entities.Where(e => e.EntityNode.Name == nodeName).FirstOrDefault();
    }

    public Entity Spawn(string sceneName)
    {
        PackedScene ps = ResourceLoader.Load(Util.GetResourceString(sceneName, RESOURCE.SCENE)) as PackedScene;
        EntityNode en = ps.Instance() as EntityNode;
        AddChild(en);
        en.Init(en.Name);
        Entities.Add(en.Entity);

        return en.Entity;
    }
}
