using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;

internal static class TestRes
{
	private static int Main(string[] args)
	{
		Assembly asm = Assembly.LoadFrom(args[0]);

		Console.WriteLine("== 1. raw manifest stream ==");
		string[] allNames = asm.GetManifestResourceNames();
		Console.WriteLine("  names: " + allNames.Length);
		foreach (string n in allNames)
			Console.WriteLine("    [" + n + "]");
		foreach (string n in allNames)
		{
			try
			{
				using (Stream s2 = asm.GetManifestResourceStream(n))
					Console.WriteLine("    lookup '" + n + "' => " + (s2 == null ? "NULL" : s2.Length + " bytes"));
			}
			catch (Exception ex)
			{
				Console.WriteLine("    lookup '" + n + "' => " + ex.GetType().Name);
			}
		}
		using (Stream s = asm.GetManifestResourceStream("AirFader.Properties.Resources.resources"))
		{
			Console.WriteLine("  GetManifestResourceStream('AirFader.Properties.Resources.resources') => " +
				(s == null ? "NULL" : s.Length + " bytes"));
			if (s != null)
			{
				try
				{
					using (ResourceReader rr = new ResourceReader(s))
					{
						int n = 0;
						foreach (System.Collections.DictionaryEntry e in rr)
						{
							n++;
							if (n <= 3) Console.WriteLine("    entry: " + e.Key + " -> " + e.Value.GetType().Name);
						}
						Console.WriteLine("    total entries: " + n);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("    ResourceReader FAILED: " + ex.GetType().Name + ": " + ex.Message);
				}
			}
		}

		Console.WriteLine();
		Console.WriteLine("== 2. ResourceManager(baseName, assembly) ==");
		try
		{
			ResourceManager rm = new ResourceManager("AirFader.Properties.Resources", asm);
			object o = rm.GetObject("af_logo2");
			Console.WriteLine("  GetObject('af_logo2') => " + (o == null ? "NULL" : o.GetType().FullName));
		}
		catch (Exception ex)
		{
			Console.WriteLine("  FAILED: " + ex.GetType().Name + ": " + ex.Message);
		}

		Console.WriteLine();
		Console.WriteLine("== 3. ResourceManager via Type ==");
		try
		{
			Type rt = asm.GetType("AirFader.Properties.Resources");
			Console.WriteLine("  type: " + (rt == null ? "NULL" : rt.FullName));
			if (rt != null)
			{
				ResourceManager rm = new ResourceManager(rt);
				object o = rm.GetObject("af_logo2");
				Console.WriteLine("  GetObject('af_logo2') => " + (o == null ? "NULL" : o.GetType().FullName));
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("  FAILED: " + ex.GetType().Name + ": " + ex.Message);
		}

		Console.WriteLine();
		Console.WriteLine("== 4. ComponentResourceManager(typeof(frmConfirm)) ==");
		try
		{
			Type t = asm.GetType("AirFader.frmConfirm");
			Console.WriteLine("  type: " + (t == null ? "NULL" : t.FullName));
			ComponentResourceManager crm = new ComponentResourceManager(t);
			object o = crm.GetObject("$this.Icon");
			Console.WriteLine("  GetObject('$this.Icon') => " + (o == null ? "NULL" : o.GetType().FullName));
		}
		catch (Exception ex)
		{
			Console.WriteLine("  FAILED: " + ex.GetType().Name + ": " + ex.Message);
		}

		Console.WriteLine();
		Console.WriteLine("== 5. assembly identity / location ==");
		Console.WriteLine("  FullName   : " + asm.FullName);
		Console.WriteLine("  Location   : " + asm.Location);
		Console.WriteLine("  EntryPoint : " + asm.EntryPoint);
		Console.WriteLine("  ImageRuntime: " + (asm.ImageRuntimeVersion ?? "(null)"));
		return 0;
	}
}
