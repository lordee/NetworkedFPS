using System;
using System.Collections.Generic;

public class Client
{
    public Player Player;
    public int NetworkID;
    public float Ping = 0;
    public int LastSnapshot = 0;
    public List<PacketSnippet> ReliablePackets = new List<PacketSnippet>();
    public List<PacketSnippet> UnreliablePackets = new List<PacketSnippet>();

    public Client(string id)
    {
        NetworkID = Convert.ToInt32(id);
    }
}