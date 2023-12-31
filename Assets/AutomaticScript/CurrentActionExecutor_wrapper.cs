using UnityEngine;
using FYFY;

public class CurrentActionExecutor_wrapper : BaseWrapper
{
	public UnityEngine.Material switchOKMaterial;
	public UnityEngine.Material switchBrokenMaterial;
	public UnityEngine.AudioClip switchLoadingAudioClip;
	public UnityEngine.AudioClip switchBrokenAudioClip;
	private void Start()
	{
		this.hideFlags = HideFlags.NotEditable;
		MainLoop.initAppropriateSystemField (system, "switchOKMaterial", switchOKMaterial);
		MainLoop.initAppropriateSystemField (system, "switchBrokenMaterial", switchBrokenMaterial);
		MainLoop.initAppropriateSystemField (system, "switchLoadingAudioClip", switchLoadingAudioClip);
		MainLoop.initAppropriateSystemField (system, "switchBrokenAudioClip", switchBrokenAudioClip);
	}

}
