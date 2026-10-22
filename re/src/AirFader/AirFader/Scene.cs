using System;
using System.Text;

namespace AirFader;

public class Scene
{
	public int sceneNum;

	private string _rawName;

	public byte[] byteName = new byte[32];

	public bool isValid;

	public string rawComment;

	public string sceneNumText
	{
		get
		{
			return sceneNum.ToString();
		}
		set
		{
			sceneNum = int.Parse(value);
		}
	}

	public string sceneValidYN
	{
		get
		{
			if (!isValid)
			{
				return "No";
			}
			return "Yes";
		}
	}

	public string rawName
	{
		get
		{
			try
			{
				_rawName = Encoding.UTF8.GetString(byteName);
				_rawName = _rawName.Remove(_rawName.IndexOf("\0"));
				return _rawName;
			}
			catch (Exception)
			{
				return "ERR";
			}
		}
		set
		{
			byteName = Encoding.ASCII.GetBytes(value.PadRight(32, '\0'));
		}
	}

	public string sceneName
	{
		get
		{
			return rawName;
		}
		set
		{
			rawName = value;
		}
	}

	public string sceneComment
	{
		get
		{
			if (rawComment.IndexOf("\t") >= 0)
			{
				return rawComment.Substring(0, rawComment.IndexOf("\t"));
			}
			return rawComment;
		}
		set
		{
			rawComment = value;
		}
	}

	public Scene(int value)
	{
		sceneNum = value;
		rawName = "";
		rawComment = "";
	}
}
