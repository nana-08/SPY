using UnityEngine;
using FYFY;

public class CurrentActionExecutor_wrapper : BaseWrapper
{
	public UnityEngine.Material switchOKMaterial;
	public UnityEngine.AudioClip switchNOKAudioClip;
	public UnityEngine.AudioClip switchOKAudioClip;
	public UnityEngine.AudioClip pickUpBatteryAudioClip;
	public UnityEngine.AudioClip dropBatteryAudioClip;
	public UnityEngine.GameObject playerCurrentWeight;
	private void Start()
	{
		this.hideFlags = HideFlags.NotEditable;
		MainLoop.initAppropriateSystemField (system, "switchOKMaterial", switchOKMaterial);
		MainLoop.initAppropriateSystemField (system, "switchNOKAudioClip", switchNOKAudioClip);
		MainLoop.initAppropriateSystemField (system, "switchOKAudioClip", switchOKAudioClip);
		MainLoop.initAppropriateSystemField (system, "pickUpBatteryAudioClip", pickUpBatteryAudioClip);
		MainLoop.initAppropriateSystemField (system, "dropBatteryAudioClip", dropBatteryAudioClip);
		MainLoop.initAppropriateSystemField (system, "playerCurrentWeight", playerCurrentWeight);
	}

}
