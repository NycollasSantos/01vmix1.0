using System;
using System.Drawing;
using System.Windows.Forms;

namespace AirFader;

public class WindowStatic
{
	public bool Ready;

	public string WindowState = "Normal";

	public Point WindowPos = new Point(10, 10);

	public Size WindowSize = new Size(100, 100);

	public int Page;

	public bool Parse(string configParam)
	{
		string[] array = configParam.Split(new char[1] { ',' }, 6);
		WindowPos = new Point(int.Parse(array[0]), int.Parse(array[1]));
		WindowSize = new Size(int.Parse(array[2]), int.Parse(array[3]));
		WindowState = array[4];
		Page = int.Parse(array[5]);
		if (!IsVisibleOnAnyScreen(new Rectangle(WindowPos, WindowSize)))
		{
			WindowPos.X = (int)Math.Round((double)Screen.PrimaryScreen.WorkingArea.Width / 2.0 - (double)WindowSize.Width / 2.0) + Screen.PrimaryScreen.WorkingArea.Left;
			WindowPos.Y = (int)Math.Round((double)Screen.PrimaryScreen.WorkingArea.Height / 2.0 - (double)WindowSize.Height / 2.0) + Screen.PrimaryScreen.WorkingArea.Top;
		}
		return true;
	}

	public static string Store(WindowStatic me)
	{
		return me.WindowPos.X + "," + me.WindowPos.Y + "," + me.WindowSize.Width + "," + me.WindowSize.Height + "," + me.WindowState + "," + me.Page;
	}

	public bool SetWindow(Form me)
	{
		switch (WindowState)
		{
		case "Normal":
			Ready = false;
			me.WindowState = FormWindowState.Normal;
			me.FormBorderStyle = FormBorderStyle.Sizable;
			me.Location = WindowPos;
			me.Size = WindowSize;
			Ready = true;
			break;
		case "Maximized":
			Ready = false;
			me.WindowState = FormWindowState.Maximized;
			me.FormBorderStyle = FormBorderStyle.Sizable;
			me.Location = WindowPos;
			me.Size = WindowSize;
			Ready = true;
			break;
		case "Fullscreen":
		{
			Ready = false;
			Screen screen = Screen.FromRectangle(me.Bounds);
			me.WindowState = FormWindowState.Normal;
			me.FormBorderStyle = FormBorderStyle.None;
			me.Bounds = screen.Bounds;
			Ready = true;
			break;
		}
		}
		Ready = true;
		return true;
	}

	private bool IsVisibleOnAnyScreen(Rectangle rect)
	{
		Screen[] allScreens = Screen.AllScreens;
		foreach (Screen screen in allScreens)
		{
			if (screen.WorkingArea.IntersectsWith(rect))
			{
				return true;
			}
		}
		return false;
	}
}
