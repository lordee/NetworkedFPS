using System;
using Godot;
using System.Text;
using System.Reflection;

static public class Builtins
{
    static StringBuilder sb = new StringBuilder();
    static public void Print(params string[] s)
    {
        sb.Clear();
        foreach(string s2 in s)
        {
            sb.Append(s2);
        }
        GD.Print(sb.ToString());
    }

    static public void BPrint(params string[] s)
    {
        sb.Clear();
        foreach(string s2 in s)
        {
            sb.Append(s2);
        }
        GD.Print(sb.ToString());

        BroadcastUnreliable(PACKETTYPE.PRINT_HIGH, sb.ToString());
    }

    static public void BSound(Vector3 origin, string res)
    {
        sb.Clear();
        sb.Append(origin.x);
        sb.Append(",");
        sb.Append(origin.y);
        sb.Append(",");
        sb.Append(origin.z);
        sb.Append(",");
        sb.Append(res); // FIXME - tag resources with IDs shared between client/server instead

        BroadcastUnreliable(PACKETTYPE.BSOUND, sb.ToString());
    }

    static public void Remove(Entity entity)
    {
        Main.World.EntityManager.RemoveEntity(entity);
        
        BroadcastUnreliable(PACKETTYPE.REMOVE, entity.EntityNode.Name);
    }

    static public void BroadcastUnreliable(PACKETTYPE type, string value)
    {
        foreach(Client c in Main.Network.Clients)
        {
            c.UnreliablePackets.Add(new PacketSnippet {
                Type = type,
                Value = value
            });
        }
    }

    static public float Time()
    {
        return Main.World.GameTime;
    }

    static public Entity Spawn(string sceneName)
    {
        Entity ent = Main.World.EntityManager.Spawn(sceneName);
        sb.Clear();
        sb.Append(sceneName);
        sb.Append(",");
        sb.Append(ent.EntityID);
        BroadcastUnreliable(PACKETTYPE.SPAWN, sb.ToString());
        return ent;
    }

    static public Entity Find(Entity entity, string fieldName, string fieldValue)
    {
        // FIXME - need entity manager for created entities, add map ents on load to that
        // FIXME - need to start loop at passed entity in list
        foreach (Entity ent in Main.World.EntityManager.Entities)
        {
            // FIXME - this is awful
            switch (fieldName.ToLower())
            {
                case "classname":
                    if (ent.ClassName.ToLower() == fieldValue.ToLower())
                    {
                        return ent;
                    }
                    break;
            }
        }

        return null;
    }
}