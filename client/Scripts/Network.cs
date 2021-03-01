using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class Network : Node
{
    public List<Client> Clients = new List<Client>();

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
        Player p = Main.World.AddPlayer(c);

        if(GetTree().GetNetworkUniqueId() == c.NetworkID)
        {
            Main.Client = c;
            PlayerController pc = PlayerController.Instance();
            pc.Attach(p);
            UIManager.LoadHUD(p);
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
        Player p = Main.Client.Player;
        pc.Attach(p);
    }

    [Slave]
    public void ReceiveReliablePacket(byte[] packet)
    {
        string pkStr = Encoding.UTF8.GetString(packet);
        string[] split = pkStr.Split(",");
        GD.Print(split);
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
}
