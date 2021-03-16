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
        string packetString = BuildClientCmdPacket(id, pCmdQueue);
        byte[] packetBytes = Encoding.UTF8.GetBytes(packetString);

        RpcUnreliableId(RecID, nameof(ReceivePMovementServer), packetBytes);
    }

    private string BuildClientCmdPacket(int id, List<PlayerCmd> pCmdQueue)
    {
        sb.Clear();
        //sb.Append(Main.World.ServerSnapshot);
        sb.Append(Main.World.LocalSnapshot);
        sb.Append(",");
        sb.Append(id.ToString());
        sb.Append(",");
        foreach(PlayerCmd pCmd in pCmdQueue)
        {
            sb.Append(PACKET.HEADER);
            sb.Append(",");
            sb.Append(pCmd.snapshot);
            sb.Append(",");
            sb.Append(pCmd.move_forward);
            sb.Append(",");
            sb.Append(pCmd.move_right);
            sb.Append(",");
            sb.Append(pCmd.move_up);
            sb.Append(",");
            sb.Append(pCmd.aim.x.x);
            sb.Append(",");
            sb.Append(pCmd.aim.x.y);
            sb.Append(",");
            sb.Append(pCmd.aim.x.z);
            sb.Append(",");
            sb.Append(pCmd.aim.y.x);
            sb.Append(",");
            sb.Append(pCmd.aim.y.y);
            sb.Append(",");
            sb.Append(pCmd.aim.y.z);
            sb.Append(",");
            sb.Append(pCmd.aim.z.x);
            sb.Append(",");
            sb.Append(pCmd.aim.z.y);
            sb.Append(",");
            sb.Append(pCmd.aim.z.z);
            sb.Append(",");
            sb.Append(pCmd.cam_angle);
            sb.Append(",");
            sb.Append(pCmd.rotation.x);
            sb.Append(",");
            sb.Append(pCmd.rotation.y);
            sb.Append(",");
            sb.Append(pCmd.rotation.z);
            sb.Append(",");
            sb.Append(pCmd.attack);
            sb.Append(",");
            sb.Append("\"" + pCmd.projName + "\"");
            sb.Append(",");
            sb.Append(pCmd.attackDir.x);
            sb.Append(",");
            sb.Append(pCmd.attackDir.y);
            sb.Append(",");
            sb.Append(pCmd.attackDir.z);
            sb.Append(",");

            if (pCmd.impulses.Count > 0)
            {
                sb.Append(PACKET.IMPULSE);
                sb.Append(",");
                foreach(float imp in pCmd.impulses)
                {
                    sb.Append(imp);
                    sb.Append(",");
                }
            }
            sb.Append(PACKET.END);
            sb.Append(",");
        }
        if (pCmdQueue.Count > 0)
        {
            sb.Remove(sb.Length - 1, 1);
        }
        return sb.ToString();
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
        PlayerNode p = Main.World.AddPlayer(c);

        if(GetTree().GetNetworkUniqueId() == c.NetworkID)
        {
            Main.Client = c;
            PlayerController pc = PlayerController.Instance();
            pc.Attach(p);
            UIManager.LoadHUD(p.Player);
        }
    }

    [Slave]
    public void ChangeMap(string mapName)
    {
        Main.World.ChangeMap(mapName);
        PlayerController pc = Main.PlayerController;
        if (pc == null)
        {
            pc = PlayerController.Instance();
        }
        PlayerNode p = Main.Client.Player.PlayerNode;
        pc.Attach(p);
    }

    [Slave]
    public void ReceiveReliablePacket(byte[] packet)
    {
        string pkStr = Encoding.UTF8.GetString(packet);
        string[] split = pkStr.Split(",");
        int snapNum = Convert.ToInt32(split[0]);

        PACKETSTATE pState = PACKETSTATE.UNINITIALISED;
        for (int i = 1; i < split.Length; i++)
        {
            switch(split[i])
            {
                case PACKET.HEADER:
                    pState = PACKETSTATE.HEADER;
                    i++;
                    break;
                case PACKET.END:
                    pState = PACKETSTATE.END;
                    break;
            }
        
            BUILTIN type = BUILTIN.NONE;
            switch (pState)
            {
                case PACKETSTATE.UNINITIALISED:
                    GD.Print("PACKETSTATE.UNINITIALISED");
                    break;
                case PACKETSTATE.HEADER:
                    type = (BUILTIN)Convert.ToInt32(split[i++]);
                    break;
                case PACKETSTATE.END:
                    return;
            }

            switch (type)
            {
                case BUILTIN.PRINT:

                    break;
                case BUILTIN.PRINT_HIGH:
                    string val = split[i];
                    Console.Print(val);
                    HUD.Print(val);
                    break;
            }
        }        
    }

    [Slave]
    public void ReceivePacket(byte[] packet)
    {
        string pkStr = Encoding.UTF8.GetString(packet);
        string[] split = pkStr.Split(",");
        int snapNum = Convert.ToInt32(split[0]);
        Main.World.ServerSnapshot = snapNum;

        for (int i = 1; i < split.Length; i++)
        {
            ENTITYTYPE type = (ENTITYTYPE)Convert.ToInt32(split[i++]);
            switch(type)
            {
                case ENTITYTYPE.PLAYER:
                    ProcessPlayerPacket(split, ref i);
                    break;
                case ENTITYTYPE.PROJECTILE:
                    //ProcessProjectilePacket(split, ref i);
                    break;
            }
        }
        Main.World.LocalSnapshot = Main.World.LocalSnapshot < Main.World.ServerSnapshot ? Main.World.ServerSnapshot : Main.World.LocalSnapshot;
    }
/*
    private void ProcessProjectilePacket(string[] split, ref int i)
    {
        string pName = split[i++];
        string pID = split[i++];
        WEAPONTYPE weapon = (WEAPONTYPE)Convert.ToInt16(split[i++]);
        Vector3 porg = new Vector3(
            float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
        );
        Vector3 pvel = new Vector3(
            float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
        );

        Vector3 prot = new Vector3(
            float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i],  System.Globalization.CultureInfo.InvariantCulture)
        );
        _game.World.ProjectileManager.AddNetworkedProjectile(pName, pID, porg, pvel, prot, weapon);
    }*/

    private void ProcessPlayerPacket(string[] split, ref int i)
    {
        int id = Convert.ToInt32(split[i++]);
        float ping = float.Parse(split[i++]);
        int health = Convert.ToInt32(split[i++]);
        int armour = Convert.ToInt32(split[i++]);
        Vector3 org = new Vector3(
            float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
        );
        Vector3 vel = new Vector3(
            float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
        );

        Vector3 rot = new Vector3(
            float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
            , float.Parse(split[i++],  System.Globalization.CultureInfo.InvariantCulture)
        );

        UpdatePlayer(id, ping, health, armour, org, vel, rot);
    }

    public void UpdatePlayer(int id, float ping, float health, float armour, Vector3 org, Vector3 velo
        , Vector3 rot)
    {
        Client c = Clients.Where(p2 => p2.NetworkID == id).First();
        c.Ping = ping;
        c.Player.SetServerState(org, velo, rot, health, armour);
    }

    // STUBS
    public void ReceivePMovementServer(byte[] packet)
    {
        // stub for server
    }
}
