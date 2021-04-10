using System;
using Godot;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Text;

public class Util
{
    static public string GetResourceString(string resourceName, RESOURCE type)
    {
        string resource = CreateResourceString(Main.GameDir, resourceName, type);
        if (!ResourceLoader.Exists(resource))
        {
            if (Main.GameDir != "SquadFortress")
            {
                resource = CreateResourceString("SquadFortress", resourceName, type);
            }
            // FIXME error out
        }
        return resource;
    }

    static string CreateResourceString(string gameDir, string resourceName, RESOURCE type)
    {
        string res = "res://Mods/" + Main.GameDir + "/";

        switch (type)
        {
            case RESOURCE.SCENE:
                res += "Scenes/";
                break;
            case RESOURCE.SOUND:
                res += "Assets/Sounds/";
                break;
            case RESOURCE.MAP:
                res += "Maps/";
                break;
        }
        res += resourceName;

        return res;
    }

    static public string GetLuaScriptString(string scriptName)
    {
        string loc = AppDomain.CurrentDomain.BaseDirectory + "/Mods/" + Main.GameDir + "/Scripts/" + scriptName;
        return loc;
    }

    static public T DeepClone<T>(T obj)
    {
        using (var ms = new MemoryStream())
        {
        var formatter = new BinaryFormatter();
        formatter.Serialize(ms, obj);
        ms.Position = 0;

        return (T) formatter.Deserialize(ms);
        }
    }

    static public byte[] GetNextPacketBytes(byte[] packet, ref PACKET type, ref int i)
    {
        type = (PACKET)packet[i++];
        int length = (int)packet[i++];
        //int length = BitConverter.ToInt32(packet, i);
        byte[] val = new byte[length];
        Array.Copy(packet, i, val, 0, length);
        i += length;
        return val;
    }

    static public Vector3 ReadV3(byte[] packet)
    {
        Vector3 v3 = new Vector3();
        int count = 0;
        v3.x = BitConverter.ToSingle(packet, count);
        count += sizeof(float);
        v3.y = BitConverter.ToSingle(packet, count);
        count += sizeof(float);
        v3.z = BitConverter.ToSingle(packet, count);

        return v3;
    }

    static public void AppendVectorBytes(ref List<byte> packet, PACKET type, Vector3 value)
    {
        byte[] val = BitConverter.GetBytes(value.x);
        packet.Add((byte)type);
        packet.Add((byte)sizeof(float)*3);
        packet.AddRange(val);

        val = BitConverter.GetBytes(value.y);
        packet.AddRange(val);

        val = BitConverter.GetBytes(value.z);
        packet.AddRange(val);
    }

    static public void AppendUInt16Bytes(ref List<byte> packet, PACKET type, UInt16 value)
    {
        byte[] val = BitConverter.GetBytes(value);
        packet.Add((byte)type);
        packet.Add((byte)val.Length);
        packet.AddRange(val);
    }

    static public void AppendIntBytes(ref List<byte> packet, PACKET type, int value)
    {
        byte[] val = BitConverter.GetBytes(value);
        packet.Add((byte)type);
        packet.Add((byte)val.Length);
        packet.AddRange(val);
    }

    static public void AppendStringBytes(ref List<byte> packet, PACKET type, string value)
    {
        byte[] val = Encoding.UTF8.GetBytes(value);
        packet.Add((byte)type);
        packet.Add((byte)val.Length);
        packet.AddRange(val);
    }

    static public void AppendFloatBytes(ref List<byte> packet, PACKET type, float value)
    {
        byte[] val = BitConverter.GetBytes(value);
        packet.Add((byte)type);
        packet.Add((byte)val.Length);
        packet.AddRange(val);
    }

/*
FIXME - change to using unsafe for performance
https://stackoverflow.com/questions/4092393/value-of-type-t-cannot-be-converted-to/4092398
also why bother with generics, this is bad code
*/
    static public void DiffAndAppendBytes<T>(ref List<byte> packet, bool same, PACKET type, T value)
    {
        if (!same)
        {
            if (value is Vector3 v3)
            {
                AppendVectorBytes(ref packet, type, v3);
            }
            else if (value is float f)
            {
                AppendFloatBytes(ref packet, type, f);
            }
            else if (value is int i)
            {
                AppendIntBytes(ref packet, type, i);
            }
        }
    }
}