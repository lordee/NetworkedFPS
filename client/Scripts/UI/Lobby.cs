using Godot;
using System;

public class Lobby : Control, IUIItem
{
    Button _joinBtn;
    Button _hostBtn;
    LineEdit _address;
    LineEdit _port;
    public override void _Ready()
    {
        _joinBtn = (Button)GetNode("panel/join");
        _hostBtn  = (Button)GetNode("panel/host");
        _hostBtn.Connect("pressed", this, "_On_Host_Pressed");
        _joinBtn.Connect("pressed", this, "_On_Join_Pressed");
        _address = GetNode("panel/address") as LineEdit;
        _port = GetNode("panel/port") as LineEdit;
        UIManager.Open(this);
    }

    private void _Set_Status(string text, bool isok)
    {
        // simple way to show status		
        Label ok = (Label)GetNode("panel/status_ok");
        Label fail = (Label)GetNode("panel/status_fail");
        if (isok)
        {
            ok.Text = text;
            fail.Text = "";
        }
        else
        {
            ok.Text = "";
            fail.Text = text;
        }      
    }

    private void _On_Host_Pressed()
    {
        UIManager.Close();
        //Network.Host(Convert.ToInt32(_port.Text));
    }

    private void _On_Join_Pressed()
    {   
        Main.Network.Connect(_address.Text, Convert.ToInt32(_port.Text));
        UIManager.Close();
    }

    public void Open()
    {
        this.Show();
    }

    public void Close()
    {
        this.Hide();
    }

    public void UI_Up()
    {

    }

    public void UI_Down()
    {

    }

    public void UI_Cancel()
    {
        UIManager.Open(UIManager.MainMenu);
    }

    public void UI_Accept()
    {
        
    }
}