using System;
using Godot;
using System.Text;

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

        foreach(Client c in Main.Network.Clients)
        {
            c.ReliablePackets.Add(new ReliablePacket {
                Type = BUILTIN.PRINT_HIGH,
                Value = sb.ToString()
            });
        }
    }
}