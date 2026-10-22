using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;

internal static class Harness
{
	private static int _count;

	[STAThread]
	private static int Main(string[] args)
	{
		AppDomain.CurrentDomain.FirstChanceException += delegate(object sender, FirstChanceExceptionEventArgs e)
		{
			_count++;
			if (_count <= 6)
			{
				Console.WriteLine("--- FIRST CHANCE EXCEPTION #" + _count + " ---");
				Console.WriteLine(e.Exception.ToString());
				Console.WriteLine();
			}
		};

		if (args.Length < 1)
		{
			Console.WriteLine("usage: Harness <assembly.exe>");
			return 2;
		}

		try
		{
			Assembly asm = Assembly.LoadFrom(args[0]);
			Console.WriteLine("Assembly loaded: " + asm.FullName);
			foreach (AssemblyName an in asm.GetReferencedAssemblies())
			{
				Console.WriteLine("  ref: " + an.FullName);
			}

			Type program = asm.GetType("AirFader.Program", throwOnError: false);
			if (program == null)
			{
				Console.WriteLine("AirFader.Program not found");
				return 3;
			}
			MethodInfo main = program.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			if (main == null)
			{
				Console.WriteLine("Main not found");
				return 4;
			}
			Console.WriteLine("Invoking entry point...");
			main.Invoke(null, null);
			return 0;
		}
		catch (TargetInvocationException tie)
		{
			Console.WriteLine("=== TARGET INVOCATION EXCEPTION ===");
			Console.WriteLine(tie.InnerException?.ToString() ?? tie.ToString());
			return 1;
		}
		catch (Exception ex)
		{
			Console.WriteLine("=== HARNESS EXCEPTION ===");
			Console.WriteLine(ex.ToString());
			return 1;
		}
	}
}
