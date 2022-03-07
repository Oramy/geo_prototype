using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[FilePath("Preferences/PieceManager.cfg", FilePathAttribute.Location.PreferencesFolder)]
public class PieceManager : ScriptableSingleton<PieceManager>
{
	[SerializeField]
	private long id_counter = 0;

	public long GetNewID()
	{
		return id_counter++;
	} 
}

