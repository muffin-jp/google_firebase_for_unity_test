using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class JsonTool
{
	public static void Write<T>(string path, T value)
	{
		try {
			using (StreamWriter writer = new StreamWriter(path, false)){
				writer.Write(JsonUtility.ToJson(value));
				writer.Flush();
				writer.Close();
			}
		}
		catch (System.Exception e) {
			Debug.Log(e.Message);
		}
	}

	public static T Read<T>(string path)
	{
		string strTmp = System.IO.File.ReadAllText(path);
		return JsonUtility.FromJson<T>(strTmp);
	}

	public static string CombineStreamingAssetsPath(string filename)
	{
#if UNITY_EDITOR
		return System.IO.Path.Combine(Application.streamingAssetsPath, filename);
#else
		return System.IO.Path.Combine(Application.persistentDataPath, filename);
#endif
	}
}