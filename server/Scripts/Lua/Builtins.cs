using System;
using Godot;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

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

        List<byte> packet = new List<byte>();
        Util.AppendStringBytes(ref packet, PACKET.PRINT_HIGH, sb.ToString());

        QueueBroadcastReliable(packet);
    }

    static public void Precache_Scene(string res)
    {
        Precache(res, RESOURCE.SCENE);
    }

    static public void Precache_Sound(string res)
    {
        Precache(res, RESOURCE.SOUND);
    }

    static public void Precache(string res, RESOURCE type)
    {
        LuaResource lr = new LuaResource();
        lr.ID = Main.World.EntityManager.GetResourceID();
        lr.Resource = Util.GetResourceString(res, type);
        Main.World.EntityManager.Resources.Add(lr);
    }

    static public void BSound(Vector3 origin, string res)
    {
        UInt16 id = Main.World.EntityManager.Resources.Find(e => e.Resource == Util.GetResourceString(res, RESOURCE.SOUND)).ID;
        List<byte> packet = new List<byte>();
        Util.AppendVectorBytes(ref packet, PACKET.BSOUND, origin);
        Util.AppendUInt16Bytes(ref packet, PACKET.RESOURCEID, id);
        
        QueueBroadcastUnreliable(packet);
    }

    static public void Remove(Entity entity)
    {
        Main.World.EntityManager.RemoveEntity(entity);
        List<byte> packet = new List<byte>();
        Util.AppendIntBytes(ref packet, PACKET.REMOVE, entity.EntityID);
        QueueBroadcastReliable(packet);
    }

    static public void QueueBroadcastReliable(List<byte> packet)
    {
        foreach(Client c in Main.Network.Clients)
        {
            c.ReliablePackets.AddRange(packet);
        }
    }

    static public void QueueBroadcastUnreliable(List<byte> packet)
    {
        foreach(Client c in Main.Network.Clients)
        {
            c.UnreliablePackets.AddRange(packet);
        }
    }

    static public float Time()
    {
        return Main.World.GameTime;
    }

    static public Entity Spawn(string sceneName)
    {
        LuaResource lr = Main.World.EntityManager.Resources.Find(e => e.Resource == Util.GetResourceString(sceneName, RESOURCE.SCENE));
        Entity entity = Main.World.EntityManager.Spawn(lr.ID);
        
        if (lr == null)
        {
            GD.Print(sceneName, " has not been precached");
            return null;
        }

        List<byte> packet = new List<byte>();
        Util.AppendIntBytes(ref packet, PACKET.SPAWN, lr.ID);
        Util.AppendIntBytes(ref packet, PACKET.ENTITYID, entity.EntityID);

        QueueBroadcastUnreliable(packet);
        return entity;
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