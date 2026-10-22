using System;
using System.Net;

namespace AirFader;

internal class MyWebClient : WebClient
{
	public int timeOutSec = 1;

	protected override WebRequest GetWebRequest(Uri uri)
	{
		WebRequest webRequest = base.GetWebRequest(uri);
		webRequest.Timeout = timeOutSec * 1000;
		return webRequest;
	}
}
