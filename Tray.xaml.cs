using System.Drawing;
using System.Windows;

namespace CoolPaster;

public partial class Tray : Window
{
    public Tray()
    {
        InitializeComponent();
        myNotifyIcon.Icon = new Icon("klipper.ico");
    }
}

