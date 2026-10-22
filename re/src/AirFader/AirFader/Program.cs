using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AirFader.Properties;

namespace AirFader;

internal static class Program
{
	private static readonly object LogLock = new object();

	private static string LogPath
	{
		get
		{
			return Path.Combine(Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"01vmix"), "Critical.Log");
		}
	}

	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

		Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
		{
			WriteCrash(e.Exception, "UI thread");
		};
		AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
		{
			WriteCrash(e.ExceptionObject as Exception, "unhandled (" + (e.ExceptionObject == null ? "null" : e.ExceptionObject.GetType().Name) + ")");
		};

		try
		{
			Loc.SetLanguage(Loc.ParseLanguage(Settings.Default.Language));
			Application.Run(new frmSurface16());
		}
		catch (Exception ex)
		{
			WriteCrash(ex, "Main");
			try
			{
				Loc.Msg(ex.ToString(), "CRITICAL ERROR - Please Report (Critical.Log)");
			}
			catch (Exception)
			{
			}
		}
	}

	private static void WriteCrash(Exception ex, string where)
	{
		if (ex == null)
		{
			return;
		}
		try
		{
			lock (LogLock)
			{
				string path = LogPath;
				string folder = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
				{
					Directory.CreateDirectory(folder);
				}
				StringBuilder sb = new StringBuilder();
				sb.AppendLine("============ BEGIN CRITICAL ERROR ============");
				sb.AppendLine("VERSION:   " + Application.ProductVersion);
				sb.AppendLine("TIMESTAMP: " + DateTime.Now.ToString());
				sb.AppendLine("LOCATION:  " + where);
				sb.AppendLine(ex.ToString());
				sb.AppendLine("============= END CRITICAL ERROR =============");
				using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
				using (StreamWriter sw = new StreamWriter(fs))
				{
					sw.Write(sb.ToString());
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
