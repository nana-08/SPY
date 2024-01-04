using UnityEngine;
using FYFY;

public class CurrentActionExecutor_wrapper : BaseWrapper
{
	public UnityEngine.Material switchRightMaterial;
	public UnityEngine.Material switchWrongMaterial;
	public UnityEngine.AudioClip switchLoadingAudioClip;
	public UnityEngine.AudioClip switchWrongAudioClip;
	public UnityEngine.GameObject playerWeight;
	private void Start()
	{
		this.hideFlags = HideFlags.NotEditable;
		MainLoop.initAppropriateSystemField (system, "switchRightMaterial", switchRightMaterial);
		MainLoop.initAppropriateSystemField (system, "switchWrongMaterial", switchWrongMaterial);
		MainLoop.initAppropriateSystemField (system, "switchLoadingAudioClip", switchLoadingAudioClip);
		MainLoop.initAppropriateSystemField (system, "switchWrongAudioClip", switchWrongAudioClip);
		MainLoop.initAppropriateSystemField (system, "playerWeight", playerWeight);
	}

}
