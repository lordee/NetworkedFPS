using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public class Network : Node
{
    int _port = 27505;
    int _maxPlayers = 8;

    public List<Client> Clients = new List<Client>();
    StringBuilder sb = new StringBuilder();
    StringBuilder sb2 = new StringBuilder();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GetTree().Connect("network_peer_connected", this, "ClientConnected");
        GetTree().Connect("network_peer_disconnected", this, "ClientDisconnected");
        GetTree().Connect("connected_to_server", this, "ConnectionSuccess");
        GetTree().Connect("connection_failed", this, "ConnectionFailed");
        GetTree().Connect("server_disconnected", this, "ConnectionRemoved");

        var Peer = new NetworkedMultiplayerENet();
		Peer.CreateServer(_port, _maxPlayers);

		GD.Print($"Started hosting on port '{_port}'");
        GetTree().NetworkPeer = Peer;
    }

    public override void _PhysicsProcess(float delta)
    {
        foreach (Client c in Clients)
        {
            // FIXME - culling of info sent based on leaf/cull data
            byte[] packetBytes = BuildUnreliablePacket(c);
            if (packetBytes.Length > 0)
            {
                RpcUnreliableId(c.NetworkID, nameof(ReceivePacket), packetBytes);
            }
            
            // FIXME
            byte[] packetBytesReliable = BuildReliablePacket(c);
            if (packetBytesReliable.Length > 0)
            {
                RpcId(c.NetworkID, nameof(ReceivePacket), packetBytesReliable);
            }
        }
    }

    public void ClientConnected(string id)
    {
        GD.Print("Client connected - ID: " +  id);
        Client c = new Client(id);
        Clients.Add(c);
        // FIXME - also do this on map change to support map entities

        Main.World.AddPlayer(c);

        // tell all other clients, new client is in, spawn their client/player
        Rpc(nameof(AddPlayer), id);

        // tell new client to load the map
        RpcId(c.NetworkID, nameof(LoadMap), Main.World.MapName);
        SendResourceList(c);
        Main.ScriptManager.ClientConnected(c);
    }

    public void ClientDisconnected(string id)
    {
        GD.Print("Client disconnected - ID: " +  id);
        Client c = Clients.Find(e => e.NetworkID == Convert.ToInt32(id));
        
        Main.ScriptManager.ClientDisconnected(c);
        Clients.Remove(c);
        Main.World.RemovePlayer(id);
        Rpc(nameof(RemovePlayer), id);
    }

    public void ConnectionSuccess()
    {
        GD.Print("ConnectionSuccess");    
    }

    public void ConnectionFailed()
    {
        GD.Print("ConnectionFailed");
    }

    public void ConnectionRemoved()
    {
        GD.Print("ConnectionRemoved");
    }

    public void SendResourceList(Client client)
    {
        List<byte> packet = new List<byte>();
        foreach(LuaResource lr in Main.World.EntityManager.Resources)
        {
            Util.AppendIntBytes(ref packet, PACKET.RESOURCEID, lr.ID);
            Util.AppendStringBytes(ref packet, PACKET.RESOURCE, lr.Resource);
        }
        RpcId(client.NetworkID, nameof(ReceiveResourceList), packet);
    }

    private byte[] BuildReliablePacket(Client client)
    {
        List<byte> packet = new List<byte>();

        if (client.ReliablePackets.Count > 0)
        {
            Util.AppendIntBytes(ref packet, PACKET.SNAPSHOT, Main.World.ServerSnapshot);
            packet.AddRange(client.ReliablePackets);
            client.ReliablePackets.Clear();
        }
        
        return packet.ToArray();
    }

    private byte[] BuildUnreliablePacket(Client client)
    {
        // FIXME - c# always 32 length??
        List<byte> packet = new List<byte>();
        Util.AppendIntBytes(ref packet, PACKET.SNAPSHOT, Main.World.ServerSnapshot);

        if (client.UnreliablePackets.Count > 0)
        {
            packet.AddRange(client.UnreliablePackets);
            client.UnreliablePackets.Clear();
        }

        GameState currentState = Main.World.GameStates.Last();
        // get last acked clientstate
        GameState clientState = new GameState();
        for (int i = client.GameStates.Count - 1; i >= 0; i--)
        {
            clientState = client.GameStates[i];
            if (clientState.Acked)
            {
                break;
            }
        }

        // diff state
        // FIXME - map entities are not getting IDs set - they should be spawned by server, not assumed by client
        foreach(EntityState es in currentState.EntityStates)
        {
            EntityState ces = clientState.EntityStates.Find(e => e.EntityID == es.EntityID);
            if (ces == null)
            {
                ces = new EntityState();
            }

            Util.AppendIntBytes(ref packet, PACKET.ENTITYID, es.EntityID);

            Util.DiffAndAppendBytes(ref packet, (es.Ping == ces.Ping), PACKET.PING, es.Ping);
            // FIXME - only send clients their own correct health/armour
            Util.DiffAndAppendBytes(ref packet, (es.CurrentHealth == ces.CurrentHealth), PACKET.HEALTH, es.CurrentHealth);
            Util.DiffAndAppendBytes(ref packet, (es.CurrentArmour == ces.CurrentArmour), PACKET.ARMOUR, es.CurrentArmour);
            Util.DiffAndAppendBytes(ref packet, (es.OwnerID == ces.OwnerID), PACKET.OWNERID, es.OwnerID);
            Basis esb = es.GlobalTransform.basis;
            Basis cesb = ces.GlobalTransform.basis;
            Util.DiffAndAppendBytes(ref packet, (esb.x == cesb.x), PACKET.BASISX, esb.x);
            Util.DiffAndAppendBytes(ref packet, (esb.y == cesb.y), PACKET.BASISY, esb.y);
            Util.DiffAndAppendBytes(ref packet, (esb.z == cesb.z), PACKET.BASISZ, esb.z);
            Util.DiffAndAppendBytes(ref packet, (es.GlobalTransform.origin == ces.GlobalTransform.origin), PACKET.ORIGIN, es.GlobalTransform.origin);
            Util.DiffAndAppendBytes(ref packet, (es.Velocity == ces.Velocity), PACKET.VELOCITY, es.Velocity);
            Util.DiffAndAppendBytes(ref packet, (es.CollisionLayer == ces.CollisionLayer), PACKET.COLLISIONLAYER, es.CollisionLayer);
            Util.DiffAndAppendBytes(ref packet, (es.CollisionMask == ces.CollisionMask), PACKET.COLLISIONMASK, es.CollisionMask);
            Util.DiffAndAppendBytes(ref packet, (es.MoveSpeed == ces.MoveSpeed), PACKET.MOVESPEED, es.MoveSpeed);
            Util.DiffAndAppendBytes(ref packet, (es.MoveType == ces.MoveType), PACKET.MOVETYPE, (int)es.MoveType);
            Util.DiffAndAppendBytes(ref packet, (es.Emitting == ces.Emitting), PACKET.EMITTING, es.Emitting);
        }
        client.GameStates.Add(Util.DeepClone(currentState));
        if (client.GameStates.Count > 32)
        {
            client.GameStates.RemoveAt(0);
        }

        return packet.ToArray();
    }

    [Remote]
    public void ReceivePMovementServer(byte[] packet)
    {
        int i = 0;
        PACKET type = PACKET.NONE;
        byte[] val = Util.GetNextPacketBytes(packet, ref type, ref i);
        int id = BitConverter.ToInt32(val, 0);
        Client c = Clients.Where(x => x.NetworkID == id).FirstOrDefault();
        if (c == null)
        {
            return;
        }

        val = Util.GetNextPacketBytes(packet, ref type, ref i);
        int serverSnapNumAck = BitConverter.ToInt32(val, 0);

        GameState gs = c.GameStates.Find(e => e.SnapShotNumber == serverSnapNumAck);
        if (gs != null)
        {
            gs.Acked = true;
        }
        c.Ping = (Main.World.ServerSnapshot - serverSnapNumAck) * Main.World.FrameDelta;

        PlayerCmd pCmd = null;
        while (i < packet.Length)
        {
            type = PACKET.NONE;
            val = Util.GetNextPacketBytes(packet, ref type, ref i);
            
            switch(type)
            {
                case PACKET.PCMDSNAPSHOT:
                    if (pCmd != null)
                    {
                        if (pCmd.snapshot > c.LastSnapshot)
                        {
                            c.Player.pCmdQueue.Add(pCmd);
                        }
                    }
                    pCmd = new PlayerCmd();
                    pCmd.snapshot = BitConverter.ToInt32(val, 0);
                    break;
                case PACKET.PCMDFORWARD:
                    pCmd.move_forward = BitConverter.ToSingle(val, 0);
                    break;
                case PACKET.PCMDRIGHT:
                    pCmd.move_right = BitConverter.ToSingle(val, 0);
                    break;
                case PACKET.PCMDUP:
                    pCmd.move_up = BitConverter.ToSingle(val, 0);
                    break;
                case PACKET.BASISX:
                    pCmd.basis.x = Util.ReadV3(val);
                    break;
                case PACKET.BASISY:
                    pCmd.basis.y = Util.ReadV3(val);
                    break;
                case PACKET.BASISZ:
                    pCmd.basis.z = Util.ReadV3(val);
                    break;
                case PACKET.PCMDCAMANGLE:
                    pCmd.cam_angle = BitConverter.ToSingle(val, 0);
                    break;
                case PACKET.PCMDATTACK:
                    pCmd.attack = BitConverter.ToInt32(val, 0);
                    break;
                case PACKET.IMPULSE:
                    pCmd.impulses.Add(BitConverter.ToSingle(val, 0));
                    break;
            }
        }
        if (pCmd != null)
        {
            if (pCmd.snapshot > c.LastSnapshot)
            {
                c.Player.pCmdQueue.Add(pCmd);
            }
        }
    }

    // stubs
    public void ReceivePacket(byte[] packet)
    {
        // STUB for clients
    }

    public void AddPlayer(string id)
    {
        // STUB for client
    }

    public void RemovePlayer(string id)
    {
        // STUB for client
    }

    public void LoadMap(string mapName)
    {
        // STUB for client
    }

    // FIXME - player shouldn't reference this, playercontroller should
    public void SendPMovement(int RecID, int id, List<PlayerCmd> pCmdQueue)
    {  
        // STUB
    }

    public void ReceiveResourceList(byte[] packet)
    {
        // STUB
    }
}
