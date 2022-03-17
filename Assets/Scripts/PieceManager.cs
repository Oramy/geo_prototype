using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceManager
{
	private static PieceManager _instance;

	public static PieceManager instance
	{
		get
		{
			if (_instance == null)
				_instance = new PieceManager();
			return _instance;
		}
	}
		

	[SerializeField]
	private long id_counter = 0;

	public long GetNewID()
	{
		return id_counter++;
	} 
}

