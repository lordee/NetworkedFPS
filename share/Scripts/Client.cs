using System;

public class Client
{
    public Player Player;
    public int NetworkID;
    public float Ping = 0;
    public int LastSnapshot = 0;

    public Client(string id)
    {
        NetworkID = Convert.ToInt32(id);
    }
}