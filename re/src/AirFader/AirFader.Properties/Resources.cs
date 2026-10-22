using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace AirFader.Properties;

[CompilerGenerated]
[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[DebuggerNonUserCode]
internal class Resources
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (object.ReferenceEquals(resourceMan, null))
			{
				ResourceManager resourceManager = new ResourceManager("AirFader.Properties.Resources", typeof(Resources).Assembly);
				resourceMan = resourceManager;
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	internal static Bitmap af_300x213_trans
	{
		get
		{
			object @object = ResourceManager.GetObject("af_300x213_trans", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Bitmap af_300x213_trans_beta
	{
		get
		{
			object @object = ResourceManager.GetObject("af_300x213_trans_beta", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Bitmap af_320x275
	{
		get
		{
			object @object = ResourceManager.GetObject("af_320x275", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Bitmap af_320x275_beta
	{
		get
		{
			object @object = ResourceManager.GetObject("af_320x275_beta", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Bitmap af_320x275_trans
	{
		get
		{
			object @object = ResourceManager.GetObject("af_320x275_trans", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Bitmap af_logo2
	{
		get
		{
			object @object = ResourceManager.GetObject("af_logo2", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal static Bitmap thumb_trans
	{
		get
		{
			object @object = ResourceManager.GetObject("thumb_trans", resourceCulture);
			return (Bitmap)@object;
		}
	}

	internal Resources()
	{
	}
}
