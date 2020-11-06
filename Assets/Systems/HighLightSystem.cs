﻿using UnityEngine;
using FYFY;
using FYFY_plugins.PointerManager;

public class HighLightSystem : FSystem {
	// Use this to update member variables when system pause. 
	// Advice: avoid to update your families inside this function.

	private Family highlightedGO = FamilyManager.getFamily(new AllOfComponents(typeof(HighLight), typeof(PointerOver)));

	private GameObject highLightedItem;
	
	public HighLightSystem(){
		highlightedGO.addEntryCallback(highLightItem);
		highlightedGO.addExitCallback(unHighLightItem);
		highLightedItem = null;
	}
	protected override void onPause(int currentFrame) {
	}

	// Use this to update member variables when system resume.
	// Advice: avoid to update your families inside this function.
	protected override void onResume(int currentFrame){
	}

	// Use to process your families.
	protected override void onProcess(int familiesUpdateCount) {
	}

	public void highLightItem(GameObject go){
		highLightedItem = go;
		go.GetComponent<HighLight>().basecolor = go.GetComponent<Renderer>().material.color;
		go.GetComponent<Renderer>().material.color = Color.yellow;
	}

	public void unHighLightItem(int id){
		if(highlightedGO != null)
			highLightedItem.GetComponent<Renderer>().material.color = highLightedItem.GetComponent<HighLight>().basecolor;
	}
}