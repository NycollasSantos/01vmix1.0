using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

// Dumps every visible text of the 01vmix UI in English and in pt-BR and reports
// which texts are still untranslated.
internal static class TestLoc
{
	[STAThread]
	private static int Main(string[] args)
	{
		string path = args.Length > 0 ? args[0] : @"C:\Users\DELL\Desktop\cloneair\build\out\01vmix.exe";
		try
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Assembly asm = Assembly.LoadFrom(path);

			Type locType = asm.GetType("AirFader.Loc", true);
			Type langType = asm.GetType("AirFader.UiLanguage", true);
			MethodInfo setLang = locType.GetMethod("SetLanguage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			object ptBr = Enum.Parse(langType, "PortugueseBrazil");

			Console.WriteLine("### ENGLISH ###");
			Dictionary<string, string> english = DumpAll(asm, setLang, langType, Enum.Parse(langType, "English"));

			Console.WriteLine();
			Console.WriteLine("### PORTUGUES (BR) ###");
			Dictionary<string, string> translated = DumpAll(asm, setLang, langType, ptBr);

			Console.WriteLine();
			Console.WriteLine("### STILL ENGLISH (same text in both languages) ###");
			List<string> still = new List<string>();
			foreach (KeyValuePair<string, string> p in english)
			{
				string t;
				if (!translated.TryGetValue(p.Key, out t)) { continue; }
				if (!string.IsNullOrEmpty(t) && t == p.Value && LooksTranslatable(t))
				{
					still.Add(p.Key + "  =  " + t);
				}
			}
			foreach (string s in still) { Console.WriteLine("  " + s); }
			Console.WriteLine("untranslated: " + still.Count);
			return 0;
		}
		catch (Exception ex)
		{
			Console.WriteLine("EXCEPTION: " + ex);
			return 2;
		}
	}

	private static bool LooksTranslatable(string text)
	{
		if (text.Length == 0) { return false; }
		// pure numbers / punctuation / identifiers are not translated on purpose
		foreach (char c in text)
		{
			if (char.IsLetter(c)) { return true; }
		}
		return false;
	}

	private static Dictionary<string, string> DumpAll(Assembly asm, MethodInfo setLang, Type langType, object language)
	{
		setLang.Invoke(null, new object[] { language });

		Dictionary<string, string> map = new Dictionary<string, string>();
		Type[] order = new Type[]
		{
			asm.GetType("AirFader.frmSurface16", true),
			asm.GetType("AirFader.frmConfig", true),
			asm.GetType("AirFader.frmScene", true),
			asm.GetType("AirFader.frmEFX", true),
			asm.GetType("AirFader.frmGraphicEQ", true),
			asm.GetType("AirFader.frmKeyboard", true),
			asm.GetType("AirFader.frmTrial", true),
			asm.GetType("AirFader.frmUpdate", true),
			asm.GetType("AirFader.frmDebug", true),
			asm.GetType("AirFader.frmFancyBox", true),
			asm.GetType("AirFader.frmConfirm", true)
		};

		foreach (Type t in order)
		{
			object form = null;
			try
			{
				ConstructorInfo ctor = t.GetConstructor(new Type[] { asm.GetType("AirFader.frmSurface16", true) });
				if (ctor != null)
				{
					object surface = Activator.CreateInstance(asm.GetType("AirFader.frmSurface16", true));
					form = ctor.Invoke(new object[] { surface });
				}
				else
				{
					form = Activator.CreateInstance(t);
				}
				Form f = form as Form;
				if (f != null)
				{
					f.Show();
					Application.DoEvents();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("  [" + t.Name + "] cannot create: " + ex.GetBaseException().Message);
			}

			if (form == null) { continue; }
			try
			{
				Console.WriteLine("--- " + t.Name + " (title: " + ((Form)form).Text + ") ---");
				Collect(form as Control, t.Name, map);
			}
			finally
			{
				try { ((Form)form).Close(); ((Form)form).Dispose(); } catch (Exception) { }
			}
		}
		return map;
	}

	private static void Collect(Control c, string prefix, Dictionary<string, string> map)
	{
		string text = c.Text;
		if (!string.IsNullOrEmpty(text))
		{
			string key = prefix + "." + c.Name;
			map[key] = text;
			Console.WriteLine("  " + key + "  =  " + text.Replace("\r\n", " | ").Replace("\n", " | "));
		}
		foreach (Control child in c.Controls)
		{
			Collect(child, prefix + "." + c.Name, map);
		}
	}
}
