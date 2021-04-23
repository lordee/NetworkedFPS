using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

public class EntityManager : Node
{
    private UInt16 _entityCount = 0;
    public List<Entity> Entities = new List<Entity>();
    public List<Entity> RemoveEntityQueue = new List<Entity>();
    public List<Entity> SpawnedEntityQueue = new List<Entity>();

    // FIXME - convert to entity base?
    public List<Entity> Players = new List<Entity>();
    StringBuilder sb = new StringBuilder();

    public List<LuaResource> Resources = new List<LuaResource>();


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
            en.Init(item.Name, ENTITYTYPE.GENERIC, null);
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

    public Entity GetEntityByID(UInt16 id)
    {
        return Entities.Where(e => e.EntityID == id).FirstOrDefault();
    }

    public Entity Spawn(UInt16 resID)
    {
        PackedScene ps = Main.World.EntityManager.Resources.Find(e => e.ID == resID).PackedScene;
        EntityNode en = ps.Instance() as EntityNode;
        en.Init(en.Name, ENTITYTYPE.GENERIC, null);
        AddChild(en);

        SpawnedEntityQueue.Add(en.Entity);

        return en.Entity;
    }

    public UInt16 GetEntityID()
    {
        return _entityCount++;

        // FIXME - cycle through count
    }

    public void SpawnWithID(UInt16 resID, UInt16 entID)
    {
        Entity ent = Spawn(resID);
        ent.EntityID = entID;
    }

    public UInt16 GetResourceID()
    {
        return (UInt16)Resources.Count;
    }
}
