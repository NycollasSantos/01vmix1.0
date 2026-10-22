using System;
using System.Reflection;
using System.Windows.Forms;

internal static class TestAbout
{
	[STAThread]
	private static int Main(string[] args)
	{
		string path = args.Length > 0 ? args[0] : @"C:\Users\DELL\Desktop\cloneair\build\out\AirFader.exe";
		try
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Assembly asm = Assembly.LoadFrom(path);
			Type cfgType = asm.GetType("AirFader.frmConfig", true);
			Type surfType = asm.GetType("AirFader.frmSurface16", true);

			object surface = Activator.CreateInstance(surfType);
			Form cfg = (Form)Activator.CreateInstance(cfgType, new object[] { surface });
			cfg.Show();
			Application.DoEvents();

			TabControl tabs = null;
			foreach (Control c in cfg.Controls)
			{
				TabControl tc = c as TabControl;
				if (tc != null) { tabs = tc; break; }
			}
			if (tabs == null) { Console.WriteLine("no TabControl found"); return 2; }

			TabPage about = null;
			foreach (TabPage tp in tabs.TabPages)
			{
				if (tp.Text.ToLower().Contains("about")) { about = tp; break; }
			}
			if (about == null) { Console.WriteLine("no About tab; tabs: " + string.Join(", ", names(tabs))); return 2; }
			tabs.SelectedTab = about;
			Application.DoEvents();

			string owner = "", key = "", version = "", product = "";
			bool registerVisible = true;
			foreach (Control c in about.Controls)
			{
				Label l = c as Label;
				if (l != null)
				{
					if (l.Name == "lblOwner") { owner = l.Text; }
					else if (l.Name == "lblKey") { key = l.Text; }
					else if (l.Name == "lblVersion") { version = l.Text; }
					else if (l.Name == "lblProduct") { product = l.Text; }
				}
				Button b = c as Button;
				if (b != null && b.Name == "btnRegister") { registerVisible = b.Visible; }
			}

			Console.WriteLine("lblProduct      = " + product);
			Console.WriteLine("lblVersion      = " + version);
			Console.WriteLine("lblOwner        = " + owner);
			Console.WriteLine("lblKey          = " + key);
			Console.WriteLine("btnRegister.Visible = " + registerVisible);

			bool ok = owner == "Open Source" && key == "Not required" && !registerVisible;
			Console.WriteLine(ok ? "RESULT: ABOUT-OK" : "RESULT: ABOUT-FAILED");
			cfg.Close();
			return ok ? 0 : 1;
		}
		catch (Exception ex)
		{
			Console.WriteLine("EXCEPTION: " + ex);
			return 2;
		}
	}

	private static string[] names(TabControl tabs)
	{
		string[] a = new string[tabs.TabPages.Count];
		for (int i = 0; i < a.Length; i++) { a[i] = tabs.TabPages[i].Text; }
		return a;
	}
}
