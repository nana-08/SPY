using UnityEngine;
using FYFY;

public class CurrentActionManager_wrapper : BaseWrapper
{
	public UnityEngine.GameObject playerCurrentWeight;
	private void Start()
	{
		this.hideFlags = HideFlags.NotEditable;
		MainLoop.initAppropriateSystemField (system, "playerCurrentWeight", playerCurrentWeight);
	}

}
