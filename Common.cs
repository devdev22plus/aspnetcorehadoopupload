using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

public static class Common
{
#if DEBUG
	public static void DebugToText(string filePath, string txt)
	{
		using (System.IO.StreamWriter file = 
            new System.IO.StreamWriter(filePath, true))
        {
            file.WriteLine(txt);
        }
	}
#endif

	public static async Task CopyToAsyncWithCallback(this Stream source, Stream destination, Action<long> callback, int bufferSize = 0x1000)
    {
        var buffer = new byte[bufferSize];
		var total = 0L;
		int amtRead;
		do
		{
			amtRead = 0;
			while(amtRead < bufferSize)
			{
				var numBytes = await source.ReadAsync(buffer,
													amtRead,
													bufferSize - amtRead);
				if(numBytes == 0)
				{
					break;
				}
				amtRead += numBytes;
			}
			total += amtRead;
			await destination.WriteAsync(buffer, 0, amtRead);
			if(callback != null)
			{
				callback(total);
			}
		} while( amtRead == bufferSize );
    }

	public static async Task<string> GetMyIpAddress()
	{
		string url = "http://ransocket.herokuapp.com/myipaddress/True";

		using(HttpClientHandler httpClientHandler = new HttpClientHandler())
		{
			httpClientHandler.AllowAutoRedirect = false;

			using (var httpClient = new HttpClient(httpClientHandler))
			{
				using (var request = new HttpRequestMessage(new HttpMethod("GET"), url))
				{
					var response = await httpClient.SendAsync(request);

					if(response.StatusCode == HttpStatusCode.OK)
					{
						string contentResponse = response.Content.ReadAsStringAsync().Result;

						if (contentResponse != null && contentResponse.Length > 0)
						{
							dynamic jsonResponse = JsonConvert.DeserializeObject(contentResponse);
							bool success = jsonResponse.success;
							if (success)
							{
								string myIpAddress = jsonResponse.myipaddress;
								return myIpAddress;
							}
						}

					}
				}
			}
		}
		
		return "";
	}


	public static string WithoutFileName(string fullPath)
	{
		string tttt = fullPath;
		return tttt.Replace(Path.GetFileName(tttt), "");
	}

	public static string ToCountTime(long inputSeconds)
	{
		TimeSpan ts = TimeSpan.FromSeconds(inputSeconds);
		int minutes = (int)Math.Floor(ts.TotalMinutes);
		int seconds = (int)Math.Floor((decimal)ts.Seconds);

		return $"{minutes:D2}:{seconds:D2}";
	}

	static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
	public static string SizeSuffix(Int64 value, int decimalPlaces = 1)
	{
		if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
		if (value < 0) { return "-" + SizeSuffix(-value); } 
		if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

		// mag is 0 for bytes, 1 for KB, 2, for MB, etc.
		int mag = (int)Math.Log(value, 1024);

		// 1L << (mag * 10) == 2 ^ (10 * mag) 
		// [i.e. the number of bytes in the unit corresponding to mag]
		decimal adjustedSize = (decimal)value / (1L << (mag * 10));

		// make adjustment when the value is large enough that
		// it would round up to 1000 or more
		if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
		{
			mag += 1;
			adjustedSize /= 1024;
		}

		return string.Format("{0:n" + decimalPlaces + "} {1}", 
			adjustedSize, 
			SizeSuffixes[mag]);
	}

	public static string RandomString(int length)
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-0123456789";
		return new string(Enumerable.Repeat(chars, length).Select(s => s[new Random().Next(s.Length)]).ToArray());
	}

	public static string GenerateVideoId()
	{
		StringBuilder builder = new StringBuilder();
		Enumerable
		.Range(65, 26)
			.Select(e => ((char)e).ToString())
			.Concat(Enumerable.Range(97, 26).Select(e => ((char)e).ToString()))
			.Concat(Enumerable.Range(0, 10).Select(e => e.ToString()))
			.OrderBy(e => Guid.NewGuid())
			.Take(20)
			.ToList().ForEach(e => builder.Append(e));
		return builder.ToString();
	}

	public static string GetDirParent(string path) 
	{
		path = path.Replace("\\", "/");
		path = path.Replace("//", "/");

		string fileName = Path.GetFileName(path);
		if ( fileName != string.Empty ) path.Replace(fileName, "");
		
		return path;
	}
	
	public static string[] ExtractDirPath(string fullPath)
	{
		List<string> list = new List<string>();
		
		string exPath = string.Empty;
		string[] paths = fullPath.Split("/");
		foreach(var i in paths)
		{
			if (i == string.Empty) continue;
			
			if(exPath == string.Empty)
			{
				if (i.IndexOf(":") == -1)
				{
					exPath = "/";
				}
			}
			else
			{
				exPath += "/";
			}
			
			exPath += i;
			
			list.Add(exPath);
			//Console.WriteLine(">" + exPath);
		}
		
		if ( list.Count() > 0 )
			if (list[0].IndexOf(":") != -1) list.RemoveAt(0);
		
		return list.ToArray();
	}


	public static bool IsValidEmailAddress(this string address) => address != null && new EmailAddressAttribute().IsValid(address);
}

public static class TagsEx
{
	public static string[] ToTags(string tags)
	{
		if (tags == null || tags.Length == 0) return null;

		string[] ts = tags.Split(",", StringSplitOptions.RemoveEmptyEntries);
		int i = 0;
		string[] newts = new string[ts.Length];
		Array.ForEach(ts, (x)=>{
			newts[i] = x.Trim();
			i++;
		});
		return newts;
	}
	
	public static string ToTags(string[] tags)
	{
		string t = string.Empty;
		if ( tags == null || tags.Length == 0) goto __END;
		
		Array.ForEach(tags, (x)=>{
			t += (t.Length == 0) ? x.Trim() : (", " + x.Trim());
		});
__END:
		return t;
	}
	
	public static bool IsTag(ref string tags, string tag)
	{
		return tags.Contains(tag); // ? indexOf
	}
	
	public static bool IsTag(ref string[] tags, string tag)
	{
		string t = ToTags(tags);
		return t.Contains(tag); // ? indexOf
	}
}

public static class PageCommon
{
	public static string m_LoadCacheName { get; private set; } = string.Empty;

	static DateTime m_LoadStart;

	public static void Start()
	{
		m_LoadStart = DateTime.UtcNow;
	}

	public static float End()
	{
		return (float)(DateTime.UtcNow - m_LoadStart).TotalSeconds;
	}

	public static void CacheInit()
	{
		m_LoadCacheName = string.Empty;
	}

	public static void AddLoadCacheName(string cacheName)
	{
		if (m_LoadCacheName == string.Empty) m_LoadCacheName = cacheName;
		else m_LoadCacheName += "," + cacheName;
	}

	public static bool GetCache<T>(IMemoryCache cache, string cacheName, out T outObject) where T : class
	{
		bool b = cache.TryGetValue(cacheName, out outObject);
		if (b)
		{
			AddLoadCacheName(cacheName);
		}
		
		return b;
	}
}
