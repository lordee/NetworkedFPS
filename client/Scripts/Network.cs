using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class Network : Node
{
    public List<Client> Clients = new List<Client>();
    StringBuilder sb = new StringBuilder();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GetTree().Connect("network_peer_connected", this, "ClientConnected");
        GetTree().Connect("network_peer_disconnected", this, "ClientDisconnected");
        GetTree().Connect("connected_to_server", this, "ConnectionSuccess");
        GetTree().Connect("connection_failed", this, "ConnectionFailed");
        GetTree().Connect("server_disconnected", this, "ConnectionRemoved");
    }

    public void Connect(string ip, int port)
    {
        NetworkedMultiplayerENet Peer = new NetworkedMultiplayerENet();
		Peer.CreateClient(ip, port);
        GetTree().NetworkPeer = Peer;
    }

    public void Disconnect()
    {
        // TODO - implement disconnect
    }

    public void ClientConnected(string id)
    {
        GD.Print("Client connected: " + id);
    }
    public void ClientDisconnected(string id)
    {
        GD.Print("ClientDisconnected: " + id);
    }
    public void ConnectionSuccess()
    {
        GD.Print("ConnectionSuccess: ");
    }
    public void ConnectionFailed()
    {
        GD.Print("ConnectionFailed: ");
    }
    public void ConnectionRemoved()
    {
        GD.Print("ConnectionRemoved: ");
    }

    public void SendPMovement(int RecID, int id, List<PlayerCmd> pCmdQueue)
    {       
        byte[] packetBytes = BuildClientCmdPacket(id, pCmdQueue);

        RpcUnreliableId(RecID, nameof(ReceivePMovementServer), packetBytes);
    }

    private byte[] BuildClientCmdPacket(int id, List<PlayerCmd> pCmdQueue)
    {
        List<byte> packet = new List<byte>();
        Util.AppendIntBytes(ref packet, PACKET.PLAYERID, id);
        Util.AppendIntBytes(ref packet, PACKET.SNAPSHOT, Main.World.ServerSnapshot);

        // FIXME - eventually we want to do this properly with bits
        foreach(PlayerCmd pCmd in pCmdQueue)
        {
            Util.AppendIntBytes(ref packet, PACKET.PCMDSNAPSHOT, pCmd.snapshot);
            Util.AppendFloatBytes(ref packet, PACKET.PCMDFORWARD, pCmd.move_forward);
            Util.AppendFloatBytes(ref packet, PACKET.PCMDUP, pCmd.move_up);
            Util.AppendFloatBytes(ref packet, PACKET.PCMDRIGHT, pCmd.move_right);
            Util.AppendVectorBytes(ref packet, PACKET.BASISX, pCmd.basis.x);
            Util.AppendVectorBytes(ref packet, PACKET.BASISY, pCmd.basis.y);
            Util.AppendVectorBytes(ref packet, PACKET.BASISZ, pCmd.basis.z);
            Util.AppendFloatBytes(ref packet, PACKET.PCMDCAMANGLE, pCmd.cam_angle); // FIXME - needed?
            Util.AppendIntBytes(ref packet, PACKET.PCMDATTACK, pCmd.attack);

            foreach(float imp in pCmd.impulses)
            {
                Util.AppendFloatBytes(ref packet, PACKET.IMPULSE, imp);
            }
        }
        return packet.ToArray();
    }

    [Slave]
    public void RemovePlayer(string id)
    {
        Main.World.RemovePlayer(id);
    }

    [Slave]
    public void AddPlayer(string id)
    {
        Client c = new Client(id);
        Clients.Add(c);
        EntityNode p = Main.World.AddPlayer(c);
        p.Entity.EntityID = (ushort)Convert.ToInt32(id);

        if(GetTree().GetNetworkUniqueId() == c.NetworkID)
        {
            Main.Client = c;
            Main.PlayerController.Attach(p);
            UIManager.LoadHUD();
        }
    }

    [Slave]
    public void LoadMap(string mapName)
    {
        // load map
        Main.World.ChangeMap(mapName);
        PlayerController pc = Main.PlayerController;
        EntityNode p = Main.Client.Player.EntityNode;
        pc.Attach(p);
    }

    private void PacketBSound(byte[] val, byte[] packet, ref int i)
    {
        Vector3 org = Util.ReadV3(packet);
        PACKET type = PACKET.NONE;
        val = Util.GetNextPacketBytes(packet, ref type, ref i);
        if (type == PACKET.RESOURCEID)
        {
            UInt16 resourceID = BitConverter.ToUInt16(val, 0);
            string resource = Main.World.EntityManager.Resources.Find(e => e.ID == resourceID).Resource;
            Main.SoundManager.Sound3D(org, resource);
        }
    }

    private void PacketRemove(byte[] packet)
    {
        UInt16 id = BitConverter.ToUInt16(packet, 0);
        Main.World.EntityManager.RemoveEntity(Main.World.EntityManager.GetEntityByID(id));
    }

    private void PacketEntity(byte[] val, byte[] packet, ref int i)
    {
        UInt16 id = BitConverter.ToUInt16(val, 0);

        Entity ent = Main.World.EntityManager.Entities.Find(e => e.EntityID == id);
        bool process = true;
        Transform t = new Transform();
        if (ent == null || ent.EntityNode.NativeInstance == IntPtr.Zero)
        {
            //FIXME - spawn not received yet, hold packet?
            process = false;
        }
        else
        {
            t = ent.EntityNode.Transform;
        }
        PACKET type = PACKET.NONE;
        
        bool entPacket = true;
        while (entPacket && i < packet.Length)
        {
            int oldi = i;
            val = Util.GetNextPacketBytes(packet, ref type, ref i);
            if (type == PACKET.ENTITYID)
            {
                entPacket = false;
                i = oldi;
            }
            if (process)
            {
                switch (type)
                {
                    case PACKET.OWNERID:
                        UInt16 ownerID = BitConverter.ToUInt16(val, 0);
                        Entity owner = Main.World.EntityManager.Entities.Find(e2 => e2.EntityID == ownerID);
                        ent.Owner = owner;
                        break;
                    case PACKET.PING:
                        ent.ClientOwner.Ping = BitConverter.ToSingle(val, 0);
                        break;
                    case PACKET.HEALTH:
                        ent.CurrentHealth = BitConverter.ToSingle(val, 0);
                        break;
                    case PACKET.ARMOUR:
                        ent.CurrentArmour = BitConverter.ToSingle(val, 0);
                        break;
                    case PACKET.MOVETYPE:
                        ent.MoveType = (MOVETYPE)BitConverter.ToUInt16(val, 0);
                        break;
                    case PACKET.MOVESPEED:
                        ent.MoveSpeed = BitConverter.ToSingle(val, 0);
                        break;
                    case PACKET.VELOCITY:
                        ent.Velocity = Util.ReadV3(val);
                        break;
                    case PACKET.COLLISIONLAYER:
                        ent.CollisionLayer = BitConverter.ToUInt32(val, 0);
                        break;
                    case PACKET.COLLISIONMASK:
                        ent.CollisionMask = BitConverter.ToUInt32(val, 0);
                        break;
                    case PACKET.BASISX:
                        Vector3 x = Util.ReadV3(val);
                        if (ent.ClientOwner == null || ent.ClientOwner.NetworkID != GetTree().GetNetworkUniqueId())
                        {
                            t.basis.x = x;
                        }
                        break;
                    case PACKET.BASISY:
                        Vector3 y = Util.ReadV3(val);
                        if (ent.ClientOwner == null || ent.ClientOwner.NetworkID != GetTree().GetNetworkUniqueId())
                        {
                            t.basis.y = y;
                        }
                        break;
                    case PACKET.BASISZ:
                        Vector3 z = Util.ReadV3(val);
                        if (ent.ClientOwner == null || ent.ClientOwner.NetworkID != GetTree().GetNetworkUniqueId())
                        {
                            t.basis.z = z;
                        }
                        break;
                    case PACKET.ORIGIN:
                        t.origin = Util.ReadV3(val);
                        break;
                    case PACKET.EMITTING:
                        UInt16 emit = BitConverter.ToUInt16(val, 0);
                        ent.Emitting = (emit == 1 ? true : false);
                        break;
                    default:
                        entPacket = false;
                        i = oldi;
                        break;
                }
            }
            
        }
        if (ent != null && ent.EntityNode.NativeInstance != IntPtr.Zero && t != ent.EntityNode.GlobalTransform)
        {
            ent.EntityNode.GlobalTransform = t;

            if (ent.EntityType == ENTITYTYPE.PLAYER)
            {
                ent.SetServerState(ent.Origin, ent.Velocity, ent.EntityNode.Rotation, ent.CurrentHealth, ent.CurrentArmour);
            }
        }
    }

    [Slave]
    public void ReceiveResourceList(byte[] packet)
    {
        Main.World.EntityManager.Resources.Clear();
        int i = 0;

        LuaResource lr = null;
        while (i < packet.Length)
        {
            PACKET type = PACKET.NONE;
            byte[] val = Util.GetNextPacketBytes(packet, ref type, ref i);

            switch(type)
            {
                case PACKET.RESOURCEID:
                    lr = new LuaResource();
                    lr.ID = BitConverter.ToUInt16(val, 0);
                    break;
                case PACKET.RESOURCE:
                    lr.Resource = Encoding.UTF8.GetString(val);
                    Main.World.EntityManager.Resources.Add(lr);
                    break;
            }
        }
    }

    [Slave]
    public void ReceivePacket(byte[] packet)
    {
        int i = 0;
        PACKET type = PACKET.NONE;
        byte[] val = Util.GetNextPacketBytes(packet, ref type, ref i);
        Main.World.ServerSnapshot = BitConverter.ToInt32(val, 0);
        while (i < packet.Length)
        {
            type = PACKET.NONE;
            val = Util.GetNextPacketBytes(packet, ref type, ref i);
            sb.Clear();
            switch(type)
            {
                case PACKET.BSOUND:
                    // TODO - sound manager, nodes from precache in script so no node creation
                    PacketBSound(val, packet, ref i);
                    break;
                case PACKET.PRINT_HIGH:
                    foreach (byte b in val)
                    {
                        sb.Append(b);
                        sb.Append(" ");
                    }
                    string msg = Encoding.UTF8.GetString(val);
                    Console.Print(msg);
                    HUD.Print(msg);
                    break;
                case PACKET.SPAWN:
                    UInt16 resID = BitConverter.ToUInt16(val, 0);
                    val = Util.GetNextPacketBytes(packet, ref type, ref i);
                    UInt16 entID = BitConverter.ToUInt16(val, 0);

                    Main.World.EntityManager.SpawnWithID(resID, entID);
                    break;
                case PACKET.ENTITYID:
                    PacketEntity(val, packet, ref i);
                    break;
                case PACKET.REMOVE:
                    PacketRemove(val);
                    break;
            }
        }

        Main.World.LocalSnapshot = Main.World.LocalSnapshot < Main.World.ServerSnapshot ? Main.World.ServerSnapshot : Main.World.LocalSnapshot;
    }

    // STUBS
    public void ReceivePMovementServer(byte[] packet)
    {
        // stub for server
    }
}