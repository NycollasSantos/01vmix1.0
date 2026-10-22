using System;
using System.Reflection;

internal static class TestLicense
{
	private static int Main(string[] args)
	{
		string path = args.Length > 0 ? args[0] : @"C:\Users\DELL\Desktop\cloneair\build\out\AirFader.exe";
		try
		{
			Assembly asm = Assembly.LoadFrom(path);
			Type t = asm.GetType("AirFader.MixEngine", true);

			object lic = t.GetField("LICENSING_ENABLED", BindingFlags.Public | BindingFlags.Static)
				.GetRawConstantValue();
			object limit = t.GetField("UNLOCKED_CLIENT_LIMIT", BindingFlags.Public | BindingFlags.Static)
				.GetRawConstantValue();
			Console.WriteLine("LICENSING_ENABLED    = " + lic);
			Console.WriteLine("UNLOCKED_CLIENT_LIMIT= " + limit);

			// Touching a static field runs the static constructor.
			FieldInfo clients = t.GetField("licenseClients", BindingFlags.Public | BindingFlags.Static);
			Console.WriteLine("licenseClients       = " + clients.GetValue(null));

			MethodInfo check = t.GetMethod("checkSerialV2", new Type[] { typeof(string), typeof(string), typeof(bool) });
			object res = check.Invoke(null, new object[] { "Anyone", "NOT-A-KEY", true });
			Console.WriteLine("checkSerialV2(bogus) = " + res);
			Console.WriteLine("licenseClients after = " + clients.GetValue(null));

			MethodInfo getKey = t.GetMethod("getLicenseKey", BindingFlags.Public | BindingFlags.Static);
			MethodInfo getUser = t.GetMethod("getLicenseUser", BindingFlags.Public | BindingFlags.Static);
			Console.WriteLine("getLicenseKey()      = " + getKey.Invoke(null, null));
			Console.WriteLine("getLicenseUser()     = " + getUser.Invoke(null, null));

			bool ok = (bool)lic == false && (int)limit == 99 && (int)clients.GetValue(null) == 99 && (bool)res;
			Console.WriteLine(ok ? "RESULT: UNLOCKED-OK" : "RESULT: FAILED");
			return ok ? 0 : 1;
		}
		catch (Exception ex)
		{
			Console.WriteLine("EXCEPTION: " + ex);
			return 2;
		}
	}
}
