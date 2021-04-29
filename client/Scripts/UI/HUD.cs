using Godot;
using System;
using System.Collections.Generic;

public class HUD : CanvasLayer
{
    Label _health;
    Sprite _healthSprite;
    Sprite _armourSprite;
    Label _armour;
    public Sprite Crosshair;
    Control _manager;
    public Node2D AimAt;
    List<uint> times = new List<uint>();
    Label _fps;

    static Label _printHigh;
    static List<Tuple<string, float>> _removeHighLines = new List<Tuple<string, float>>();
    static List<Tuple<string, float>> _printHighLines = new List<Tuple<string, float>>();

    // Canvas layer currently has no visibility controls, parent a control node and use that for all other children
    private bool _visible = true;
    public bool Visible { 
        get { return _visible; }
        set { 
                _visible = value; 
                _manager.Visible = value;
            }
        }

    public override void _Ready()
    {
        _manager = GetNode("Manager") as Control;
        _health = _manager.GetNode("HealthLabel") as Label;
        _armour = _manager.GetNode("ArmourLabel") as Label;
        _healthSprite = _manager.GetNode("Health") as Sprite;
        _armourSprite = _manager.GetNode("Armour") as Sprite;
        _printHigh = _manager.GetNode("PrintHigh") as Label;
        
        AimAt = _manager.GetNode("AimAt") as Node2D;
        Crosshair = AimAt.GetNode("Crosshair") as Sprite;
        _fps = _manager.GetNode("FPS") as Label;
        this.Visible = false;
    }

    public override void _Process(float delta)
    {
        if (this.Visible)
        {
            Vector2 size = GetViewport().Size;
            Vector2 pos = size;
            pos.x = pos.x / 2 - _armour.RectSize.x;
            pos.y = pos.y - _armour.RectSize.y;
            _armour.SetGlobalPosition(pos);
            pos.x = pos.x - _armourSprite.Texture.GetSize().x * _armourSprite.Scale.x;
            pos.y = pos.y + _armourSprite.Texture.GetSize().y * _armourSprite.Scale.y / 2;
            _armourSprite.GlobalPosition = pos;

            pos = size;
            float healthSpriteX = _healthSprite.Texture.GetSize().x * _healthSprite.Scale.x;
            pos.x = pos.x / 2 + healthSpriteX + _health.RectSize.x;
            pos.y = pos.y - _health.RectSize.y;
            _health.SetGlobalPosition(pos);
            pos.x = size.x / 2 + healthSpriteX;
            pos.y = pos.y + _healthSprite.Texture.GetSize().y * _healthSprite.Scale.y / 2;
            _healthSprite.GlobalPosition = pos;

            _health.Text = Mathf.CeilToInt(Main.Client.Player.Health).ToString();
            _armour.Text = Mathf.CeilToInt(Main.Client.Player.Armour).ToString();
            
            AimAt.GlobalPosition = size / 2;

            uint now = OS.GetTicksMsec();
            while (times.Count > 0 && times[0] <= now - 1000)
            {
                times.RemoveAt(0);
            }

            times.Add(now);
            _fps.Text = times.Count.ToString() + " FPS";
            _fps.SetGlobalPosition(new Vector2(size.x - _fps.RectSize.x, 0));

            PrintHighText();
        }
    }

    private void PrintHighText()
    {
        // TODO - setting for max lines printed
        _printHigh.Text = "";
        foreach(Tuple<string, float> s in _printHighLines)
        {
            if (s.Item2 >= Main.World.GameTime)
            {
                _printHigh.Text += s.Item1 + System.Environment.NewLine;
            }
            else
            {
                _removeHighLines.Add(s);
            }
        }

        foreach(Tuple<string, float> i in _removeHighLines)
        {
            _printHighLines.Remove(i);
        }
        _removeHighLines.Clear();
    }

    static public void Print(string s)
    {
        // TODO - change 2 second delay to option set by client
        _printHighLines.Add(new Tuple<string, float>(s, Main.World.GameTime + 2));
    }
}
