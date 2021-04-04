using System;
using System.Collections.Generic;

public class Client
{
    public Player Player;
    public int NetworkID;
    public float Ping = 0;
    public int LastAckSnapShot = 0;
    public int LastSnapshot = 0; // this is for sorting/applying only future pcmd packets
    public List<PacketSnippet> ReliablePackets = new List<PacketSnippet>();
    public List<PacketSnippet> UnreliablePackets = new List<PacketSnippet>();
    public List<GameState> GameStates = new List<GameState>();


    public Client(string id)
    {
        NetworkID = Convert.ToInt32(id);
        GameState gs = new GameState();
        GameStates.Add(gs);
    }
}