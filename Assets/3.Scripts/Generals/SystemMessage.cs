using UnityEngine;

public static class SystemMessage
{
	public static string FileNameNotFound(string fileName) 
		=> $"<Error Code 0x00000cc6> FileName \"{fileName}\" Has Not Founded";

	public static string ObjectNameNotFound(string fileName)
	=> $"<Error Code 0x00000cc7> ObjectName \"{fileName}\" Has Not Founded";
}
