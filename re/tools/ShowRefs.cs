using System;
using System.Reflection;

internal static class ShowRefs
{
	private static int Main(string[] args)
	{
		foreach (string path in args)
		{
			try
			{
				Assembly asm = Assembly.ReflectionOnlyLoadFrom(path);
				Console.WriteLine("== " + System.IO.Path.GetFileName(path) + " ==");
				Console.WriteLine("   ImageRuntime: " + (asm.ImageRuntimeVersion ?? "(null)"));
				foreach (AssemblyName an in asm.GetReferencedAssemblies())
				{
					Console.WriteLine("   ref: " + an.FullName);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("== " + path + " == FAILED: " + ex.GetType().Name + ": " + ex.Message);
			}
		}
		return 0;
	}
}
