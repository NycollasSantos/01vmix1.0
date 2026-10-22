using System;
using System.IO;
using System.Reflection;

internal static class ExtractRes
{
	private static int Main(string[] args)
	{
		if (args.Length < 2)
		{
			Console.WriteLine("usage: ExtractRes <assembly.exe> <outDir>");
			return 2;
		}
		Assembly asm = Assembly.LoadFrom(args[0]);
		string outDir = args[1];
		Directory.CreateDirectory(outDir);
		string[] names = asm.GetManifestResourceNames();
		foreach (string name in names)
		{
			using (Stream s = asm.GetManifestResourceStream(name))
			using (FileStream fs = File.Create(Path.Combine(outDir, name)))
			{
				s.CopyTo(fs);
			}
			Console.WriteLine("  {0,-50} {1} bytes", name, new FileInfo(Path.Combine(outDir, name)).Length);
		}
		Console.WriteLine("Extracted " + names.Length + " resource(s).");
		return 0;
	}
}
