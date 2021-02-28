
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace Effyiex {

  public partial class NativeAccess {

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RectStruct lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, out RectStruct lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RectStruct {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string className,  string  windowTitle);

    [DllImport("user32.dll")]
    public static extern long SetWindowLongA(IntPtr hWnd, int nIndex, long dwNewLong);

    [DllImport("user32.dll")]
    public static extern long GetWindowLongA(IntPtr hWnd, int nIndex);

  }

  public class CrosshairCanvas : Form {

    public static Image Texture { get; private set; }

    public static List<string[]> Options = new List<string[]>();

    public static void FromFiles() {
      Texture = Image.FromFile(Application.StartupPath + @"\crosshair.png");
      string[] ini = System.IO.File.ReadAllLines(Application.StartupPath + @"\crosshair.ini");
      foreach(string line in ini) {
        string[] splitted = line.Split('=');
        for(int i = 0; i < splitted.Length; i++)
        splitted[i] = splitted[i].Trim();
        if(2 <= splitted.Length) Options.Add(splitted);
      }
    }

    public static string FromOption(string pointer, string notnull) {
      foreach(string[] Option in Options)
      if(Option[0].Equals(pointer)) return Option[1];
      return notnull;
    }

    public int ScaledSize;

    public CrosshairCanvas() {
      this.DoubleBuffered = true;
      this.FormBorderStyle = FormBorderStyle.None;
      this.ShowInTaskbar = false;
      this.TransparencyKey = ColorTranslator.FromHtml(FromOption("TransparencyKey", "#000000"));
      this.BackColor = TransparencyKey;
      this.ScaledSize = int.Parse(FromOption("Scale", "32"));
      this.Size = ScaledSize < 64 ? new Size(64, 64) : new Size(ScaledSize, ScaledSize);
      this.Paint += new PaintEventHandler(Render);
      this.Enabled = false;
      NativeAccess.SetWindowLongA(Handle, -20, NativeAccess.GetWindowLongA(Handle, -20) | 0x80000 | 0x20);
    }

    public void Update(bool visible, NativeAccess.RectStruct parent) {
      int x = parent.Left + (parent.Right - parent.Left - Width) / 2;
      int y = parent.Top + (parent.Bottom - parent.Top - Height) / 2;
      // Canvas.Invalidate();
      this.Location = new Point(x, y);
      this.TopMost = true;
      if(this.Visible != visible) this.Visible = visible;
    }

    private void Render(object sender, PaintEventArgs args) {
      args.Graphics.DrawImage(Texture, (Width - ScaledSize) / 2, (Height - ScaledSize) / 2, ScaledSize, ScaledSize);
    }

  }

  public class CustomCrosshair {

    public static readonly CustomCrosshair Client = new CustomCrosshair();

    [STAThread]
    public static void Main(string[] args) {
      Client.Initialize();
    }

    private Thread Thread;
    private CrosshairCanvas Canvas;
    private GlobalKeyboardHook KeyHook;

    public IntPtr GameHandle { get; private set; }
    public bool Running { get; set; }
    public bool Visible { get; set; }
    public Keys ToggleBind { get; private set; }

    public bool IsForeground {
      get {
        return NativeAccess.GetForegroundWindow() == GameHandle;
      }
    }

    protected IntPtr SearchGameHandle(string title) {
      Console.WriteLine("Press any key fetch the Game-Process.");
      Console.ReadKey();
      Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
      IntPtr Temp = NativeAccess.FindWindow(null, title);
      if(Temp != IntPtr.Zero) return Temp;
      else {
        Console.WriteLine("ERROR: Couldn't find: \"" + title + '\"');
        return SearchGameHandle(title);
      }
    }

    protected Keys AwaitToggleBind() {
      Console.WriteLine("Press a bind, which should toggle the Crosshair.");
      Keys Key = AwaitKey();
      Console.WriteLine("Pressed-Key: " + Key.ToString());
      if(Key != Keys.None) return Key;
      else {
        Console.WriteLine("ERROR: Not a valid Key");
        return AwaitToggleBind();
      }
    }

    private void Initialize() {
      if(Running) return;
      Console.Title = "Custom Crosshair by Effyiex.";
      KeyHook = new GlobalKeyboardHook();
      KeyHook.KeyDown += new KeyEventHandler(OnNativeKeyPress);
      CrosshairCanvas.FromFiles();
      Canvas = new CrosshairCanvas();
      Visible = false;
      Running = true;
      Thread = new Thread(OnThreadLayer);
      Thread.Start();
      Cursor.Hide();
      Application.Run(Canvas);
    }

    private void OnThreadLayer() {
      GameHandle = SearchGameHandle("Counter-Strike: Global Offensive");
      ToggleBind = AwaitToggleBind();
      while(Running) {
        Thread.Sleep(128);
        NativeAccess.RectStruct parent;
        NativeAccess.RectStruct client;
        NativeAccess.GetWindowRect(GameHandle, out parent);
        NativeAccess.GetClientRect(GameHandle, out client);
        parent.Left += ((parent.Right - parent.Left) - (client.Right - client.Left)) / 2;
        parent.Top += ((parent.Bottom - parent.Top) - (client.Bottom - client.Top)) / 2;
        Canvas.Update(IsForeground && Visible, parent);
      }
    }

    private Keys LastPressed;

    private void OnNativeKeyPress(object sender, KeyEventArgs args) {
      if(args.KeyCode == ToggleBind) {
        this.Visible = !Visible;
        Console.WriteLine("Toggled Crosshair to: " + (Visible ? "ON" : "OFF"));
      } else LastPressed = args.KeyCode;
    }

    private Keys AwaitKey() {
      this.LastPressed = Keys.None;
      while(LastPressed == Keys.None) Thread.Sleep(1);
      return this.LastPressed;
    }

  }

}
