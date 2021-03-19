using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Network : Node
{
    int _port = 27505;
    int _maxPlayers = 8;

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

        var Peer = new NetworkedMultiplayerENet();
		Peer.CreateServer(_port, _maxPlayers);

		GD.Print($"Started hosting on port '{_port}'");
        GetTree().NetworkPeer = Peer;
    }

    public override void _PhysicsProcess(float delta)
    {
        // FIXME - culling of info sent based on leaf/cull data
        string packetString = BuildPacketString();
        byte[] packetBytes = Encoding.UTF8.GetBytes(packetString);
        RpcUnreliable(nameof(ReceivePacket), packetBytes);

        foreach (Client c in Clients)
        {
            if (c.ReliablePackets.Count > 0)
            {
                string reliablePacketString = BuildReliablePacketString(c.ReliablePackets);
                byte[] reliablePacketBytes = Encoding.UTF8.GetBytes(reliablePacketString);
                RpcId(c.NetworkID, nameof(ReceiveReliablePacket), reliablePacketBytes);
                c.ReliablePackets.Clear();
            }

            if (c.UnreliablePackets.Count > 0)
            {
                string unreliablePacketString = BuildUnreliablePacketString(c.UnreliablePackets);
                byte[] unreliablePacketBytes = Encoding.UTF8.GetBytes(unreliablePacketString);
                RpcUnreliableId(c.NetworkID, nameof(ReceiveUnreliablePacket), unreliablePacketBytes);
                c.UnreliablePackets.Clear();
            }
        }
    }

    public void ClientConnected(string id)
    {
        GD.Print("Client connected - ID: " +  id);
        Client c = new Client(id);
        Clients.Add(c);
        
        Main.World.AddPlayer(c);
        Rpc(nameof(AddPlayer), id);
        RpcId(c.NetworkID, nameof(ChangeMap), Main.World.MapName);
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

    private string BuildReliablePacketString(List<PacketSnippet> packets)
    {
        sb.Clear();
        sb.Append(Main.World.ServerSnapshot);
        sb.Append(",");
        foreach (PacketSnippet packet in packets)
        {
            packet.SnapNumSent = Main.World.ServerSnapshot;
            sb.Append(PACKET.HEADER);
            sb.Append(",");
            sb.Append((int)packet.Type);
            sb.Append(",");
            sb.Append(packet.Value);
            sb.Append(",");
            sb.Append(PACKET.END);
            sb.Append(",");
        }
        if (packets.Count > 0)
        {
            sb.Remove(sb.Length - 1, 1);
        }

        return sb.ToString();
    }

    private string BuildUnreliablePacketString(List<PacketSnippet> packets)
    {
        sb.Clear();
        sb.Append(Main.World.ServerSnapshot);
        sb.Append(",");
        
        foreach (PacketSnippet packet in packets)
        {
            packet.SnapNumSent = Main.World.ServerSnapshot;
            sb.Append(PACKET.HEADER);
            sb.Append(",");
            sb.Append((int)packet.Type);
            sb.Append(",");
            sb.Append(packet.Value);
            sb.Append(",");
            sb.Append(PACKET.END);
            sb.Append(",");
        }
        if (packets.Count > 0)
        {
            sb.Remove(sb.Length - 1, 1);
        }

        return sb.ToString();
    }

    private string BuildPacketString()
    {
        sb.Clear();
        sb.Append(Main.World.ServerSnapshot);
        sb.Append(",");

        // FIXME - only send info within certain distance of clients (non culled players/ents)
        // players
        foreach(Client c in Clients)
        {
            if (c.Player == null)
            {
                continue;
            }

            Vector3 org = c.Player.ServerState.Origin;
            Vector3 velo = c.Player.ServerState.Velocity;
            Vector3 rot = c.Player.ServerState.Rotation;

            sb.Append((int)PACKETTYPE.PLAYER);
            sb.Append(",");
            sb.Append(c.NetworkID);
            sb.Append(",");
            sb.Append(c.Ping);
            sb.Append(",");
            sb.Append(c.Player.CurrentHealth);
            sb.Append(",");
            sb.Append(c.Player.CurrentArmour);
            sb.Append(",");
            sb.Append(org.x);
            sb.Append(",");
            sb.Append(org.y);
            sb.Append(",");
            sb.Append(org.z);
            sb.Append(",");
            sb.Append(velo.x);
            sb.Append(",");
            sb.Append(velo.y);
            sb.Append(",");
            sb.Append(velo.z);
            sb.Append(",");
            sb.Append(rot.x);
            sb.Append(",");
            sb.Append(rot.y);
            sb.Append(","); 
            sb.Append(rot.z);
            sb.Append(",");
        }

/*
        // projectiles
        foreach(Projectile p in _game.World.ProjectileManager.Projectiles)
        {
            sb.Append((int)PACKETTYPE.PROJECTILE);
            sb.Append(",");
            sb.Append(p.Name);
            sb.Append(",");
            sb.Append(p.PlayerOwner.ID);
            sb.Append(",");
            sb.Append((int)p.Weapon);
            sb.Append(",");
            sb.Append(p.GlobalTransform.origin.x);
            sb.Append(",");
            sb.Append(p.GlobalTransform.origin.y);
            sb.Append(",");
            sb.Append(p.GlobalTransform.origin.z);
            sb.Append(",");
            sb.Append(p.Velocity.x);
            sb.Append(",");
            sb.Append(p.Velocity.y);
            sb.Append(",");
            sb.Append(p.Velocity.z);
            sb.Append(",");
            sb.Append(p.Rotation.x);
            sb.Append(",");
            sb.Append(p.Rotation.y);
            sb.Append(",");
            sb.Append(p.Rotation.z);
            sb.Append(",");
        }
*/
        if (sb.Length > (Main.World.ServerSnapshot.ToString().Length + 1))
        {
            sb.Remove(sb.Length - 1, 1);
        }
        return sb.ToString();
    }

    [Remote]
    public void ReceivePMovementServer(byte[] packet)
    {
        string pkStr = Encoding.UTF8.GetString(packet);
        string[] split = pkStr.Split(",");

        int serverSnapNumAck = Convert.ToInt32(split[0]);
        int id = Convert.ToInt32(split[1]);

        Client c = Clients.Where(x => x.NetworkID == id).FirstOrDefault();
        if (c == null)
        {
            return;
        }
        c.Ping = (Main.World.ServerSnapshot - serverSnapNumAck) * Main.World.FrameDelta;

        PACKETSTATE pState = PACKETSTATE.UNINITIALISED;
        PlayerCmd pCmd = new PlayerCmd();
        for (int i = 2; i < split.Length; i++)
        {
            switch(split[i])
            {
                case PACKET.IMPULSE:
                    pState = PACKETSTATE.IMPULSE;
                    i++;
                    break;
                case PACKET.HEADER:
                    pCmd = new PlayerCmd();
                    pState = PACKETSTATE.HEADER;
                    i++;
                    break;
                case PACKET.END:
                    pState = PACKETSTATE.END;
                    break;
            }

            switch (pState)
            {
                case PACKETSTATE.UNINITIALISED:
                    GD.Print("PACKETSTATE.UNINITIALISED");
                    break;
                case PACKETSTATE.HEADER:
                    pCmd.playerID = id;
                    pCmd.snapshot = Int32.Parse(split[i++]);
                    pCmd.move_forward = float.Parse(split[i++]);
                    pCmd.move_right = float.Parse(split[i++]);
                    pCmd.move_up = float.Parse(split[i++]);
                    pCmd.aim = new Basis(
                                        new Vector3(float.Parse(split[i++]), float.Parse(split[i++]), float.Parse(split[i++])),
                                        new Vector3(float.Parse(split[i++]), float.Parse(split[i++]), float.Parse(split[i++])),
                                        new Vector3(float.Parse(split[i++]), float.Parse(split[i++]), float.Parse(split[i++]))
                                        );
                    pCmd.cam_angle = float.Parse(split[i++]);
                    pCmd.rotation = new Vector3(float.Parse(split[i++]), float.Parse(split[i++]), float.Parse(split[i++]));
                    pCmd.attack = int.Parse(split[i++]);
                    pCmd._projName = split[i++];
                    pCmd.attackDir = new Vector3(float.Parse(split[i++]), float.Parse(split[i++]), float.Parse(split[i]));
                    break;
                case PACKETSTATE.IMPULSE:
                    pCmd.impulses.Add(float.Parse(split[i]));
                    break;
                case PACKETSTATE.END:
                    if (pCmd.snapshot > c.LastSnapshot)
                    {
                        c.Player.pCmdQueue.Add(pCmd);
                    }
                    break;
            } 
        }
    }

    // stubs
    public void ReceivePacket(byte[] packet)
    {
        // STUB for clients
    }
    public void ReceiveUnreliablePacket(byte[] packet)
    {
        // STUB for clients
    }
    public void ReceiveReliablePacket(byte[] packet)
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

    public void ChangeMap(string mapName)
    {
        // STUB for client
    }

    // FIXME - player shouldn't reference this, playercontroller should
    public void SendPMovement(int RecID, int id, List<PlayerCmd> pCmdQueue)
    {  
        // STUB
    }
}
