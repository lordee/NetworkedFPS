using Godot;
using System;
using System.Collections.Generic;

public class Network : Node
{
    int _port = 27505;
    int _maxPlayers = 8;

    public List<Player> Players = new List<Player>();

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
        //_game.World.StartWorld();
    }

    public void ClientConnected(string id)
    {
        GD.Print("Client connected - ID: " +  id);
        Player p = new Player(id);
        this.AddChild(p);
        Main.ScriptManager.ClientConnected(p);
    }

    public void ClientDisconnected(string id)
    {
        GD.Print("Client disconnected - ID: " +  id);
        Player p = GetNode(id) as Player;
        
        Main.ScriptManager.ClientDisconnected(p);
        this.RemoveChild(p);
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
        //_game.Quit();
    }
}
