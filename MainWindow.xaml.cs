using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Newtonsoft.Json;
using NHotkey;
using NHotkey.Wpf;
using Clipboard = System.Windows.Clipboard;

namespace CoolPaster;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    enum Actions
    {
        Prettify,
        Escape,
        Unescape,
        EscapeQuotes,
        UnescapeTrim
    }
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        HotkeyManager.Current.AddOrReplace("OpenCoolClipboard", Key.V, ModifierKeys.Windows | ModifierKeys.Alt, (_, _) => OpenCoolClipboard());

        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(300, CancellationTokenSource.Token);
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (!Clipboard.ContainsText())
                {
                    continue;
                }


                await Dispatcher.InvokeAsync(() =>
                {
                    var clipBoardText = Clipboard.GetText();
                    if (CurrentClipboard != clipBoardText && PreviousClipboardValue != clipBoardText)
                    {
                        CurrentClipboard = clipBoardText;
                        PreviousClipboardValue = clipBoardText;
                    }
                });

            }

        }, CancellationTokenSource.Token);

        Closing += (_, _) => CancellationTokenSource.Cancel();

        foreach (var action in Enum.GetNames<Actions>())
        {
            LstActions.Items.Add(action);
        }
        
        LstActions.SelectionChanged += (_, e) => LstActionsOnSelectionChanged(e.AddedItems[0]?.ToString());
        
        TxtClipboard.TextChanged += (_, _) => LstActionsOnSelectionChanged(LstActions?.SelectedItem?.ToString());

        var tray = new Tray();
        tray.myNotifyIcon.TrayLeftMouseUp += (_, _) => OpenCoolClipboard();
        
        KeyDown += (o, e) =>
        {
            var isBox = Equals(Keyboard.FocusedElement, TxtClipboard);
            if (e.Key == Key.Enter && (!isBox || (Keyboard.Modifiers & ModifierKeys.Shift) != 0))
            {
                DoAction();
                return;
            }
            
        };
        LstActions.MouseDoubleClick += (o, e) => DoAction();
        
        Hide();
    }

    private void DoAction()
    {
        Clipboard.SetText(TxtPreview.Text);
        Hide();
        if (((Keyboard.Modifiers & ModifierKeys.Control) == 0))
        {
            SendCtrlV();
        }
    } 

    private void SendCtrlV()
    {
        SendKeys.SendWait("^v");
    }
    private void LstActionsOnSelectionChanged(string? item)
    {
        if (item == null)
        {
            return;
        }

        var txt = CurrentClipboard;
        var previewTxt = Enum.Parse<Actions>(item) switch {
            Actions.Prettify => Prettify(txt),
            Actions.Escape => EscapeCsharp.Escape(txt, EscapeCsharp.LiteralType.Regular),
            Actions.EscapeQuotes => $"\"{EscapeCsharp.Escape(txt, EscapeCsharp.LiteralType.Regular)}\"",
            Actions.Unescape => UnescapeCsharp.Unescape(txt, UnescapeCsharp.LiteralType.Regular),
            Actions.UnescapeTrim => UnescapeCsharp.Unescape(txt, UnescapeCsharp.LiteralType.Regular).Trim(' ', '\t', '\r', '\n', '"'),
            _ => throw new ArgumentOutOfRangeException()
        };

        TxtPreview.Text = previewTxt;
        PreviousSelectedIndex = LstActions.SelectedIndex;
    }

    private string Prettify(string txt)
    {
        try
        {
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(txt), Formatting.Indented);    
        }
        catch (Exception e)
        {
            return txt;
        }
    }
    public void OpenCoolClipboard()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Focus();

        LstActions.SelectedIndex = PreviousSelectedIndex;
        var listBoxItem = (ListBoxItem) LstActions
            .ItemContainerGenerator
            .ContainerFromItem(LstActions.SelectedItem);

        listBoxItem.Focus();

    }
    
    public string PreviousClipboardValue { get; set; }

    public int PreviousSelectedIndex { get; set; } = 0;

    public string CurrentClipboard
    {
        get => TxtClipboard.Text;
        set
        {
            TxtClipboard.Text = value;
        }
    }

    public CancellationTokenSource CancellationTokenSource { get; set; } = new ();
}
