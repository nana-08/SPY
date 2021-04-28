using UnityEngine;
using FYFY;
using FYFY_plugins.PointerManager;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;

public class UISystem : FSystem {
	// Use this to update member variables when system pause. 
	// Advice: avoid to update your families inside this function.
	private GameObject actionContainer;
    private Family requireEndPanel = FamilyManager.getFamily(new AllOfComponents(typeof(NewEnd)), new NoneOfProperties(PropertyMatcher.PROPERTY.ACTIVE_SELF));
    private Family displayedEndPanel = FamilyManager.getFamily(new AllOfComponents(typeof(NewEnd), typeof(AudioSource)), new AllOfProperties(PropertyMatcher.PROPERTY.ACTIVE_IN_HIERARCHY));
	private Family playerScript = FamilyManager.getFamily(new AllOfComponents(typeof(UITypeContainer)), new AnyOfTags("ScriptConstructor"));
	private Family playerGO = FamilyManager.getFamily(new AllOfComponents(typeof(ScriptRef),typeof(Position)), new AnyOfTags("Player"));
	private Family editableScriptContainer = FamilyManager.getFamily(new AllOfComponents(typeof(VerticalLayoutGroup), typeof(CanvasRenderer), typeof(PointerSensitive)));
	private Family agentCanvas = FamilyManager.getFamily(new AllOfComponents(typeof(HorizontalLayoutGroup), typeof(CanvasRenderer)), new NoneOfComponents(typeof(Image)));
	private Family deleteHistory = FamilyManager.getFamily(new AllOfComponents(typeof(HistoryToDelete)));	
	private Family saveHistory = FamilyManager.getFamily(new AllOfComponents(typeof(HistoryToSave)));	
	private Family actions = FamilyManager.getFamily(new AllOfComponents(typeof(PointerSensitive), typeof(UIActionType)));
    private Family currentActions = FamilyManager.getFamily(new AllOfComponents(typeof(BasicAction),typeof(UIActionType), typeof(CurrentAction)));
	private Family actionsPanel = FamilyManager.getFamily(new AllOfComponents(typeof(HorizontalLayoutGroup), typeof(Image)));
	/*
	private Family wallGO = FamilyManager.getFamily(new AllOfComponents(typeof(Position)), new AnyOfTags("Wall"));
	private Family droneGO = FamilyManager.getFamily(new AllOfComponents(typeof(Position)), new AnyOfTags("Drone"));
	private Family doorGO = FamilyManager.getFamily(new AllOfComponents(typeof(ActivationSlot), typeof(Position)), new AnyOfTags("Door"));
	private Family redDetectorGO = FamilyManager.getFamily(new AllOfComponents(typeof(Rigidbody), typeof(Detector), typeof(Position)));
	private Family coinGO = FamilyManager.getFamily(new AllOfComponents(typeof(CapsuleCollider), typeof(Position), typeof(ParticleSystem)), new AnyOfTags("Coin"));
	private Family activableConsoleGO = FamilyManager.getFamily(new AllOfComponents(typeof(Activable),typeof(Position),typeof(AudioSource)));
	*/
	private GameData gameData;
	private GameObject dialogPanel;
	private int nDialog = 0;

	public UISystem(){
		gameData = GameObject.Find("GameData").GetComponent<GameData>();
		gameData.ButtonExec = GameObject.Find("ExecuteButton");
		gameData.ButtonReset = GameObject.Find("ResetButton");
		GameObject endPanel = GameObject.Find("EndPanel");
		GameObjectManager.setGameObjectState(endPanel, false);
		dialogPanel = GameObject.Find("DialogPanel");
		GameObjectManager.setGameObjectState(dialogPanel, false);
        requireEndPanel.addEntryCallback(displayEndPanel);
        displayedEndPanel.addEntryCallback(onDisplayedEndPanel);
		deleteHistory.addEntryCallback(destroyScript);
		saveHistory.addEntryCallback(addToHistory);
		actions.addEntryCallback(linkTo);
		//currentActions.addExitCallback(enableButtons);

		//load history
		if(gameData.actionsHistory != null){
			GameObject editableCanvas = editableScriptContainer.First();
			for(int i = 0 ; i < gameData.actionsHistory.transform.childCount ; i++){
				Transform child = UnityEngine.GameObject.Instantiate(gameData.actionsHistory.transform.GetChild(i));
				//GameObjectManager.setGameObjectParent(child.gameObject, editableCanvas, true);
				child.SetParent(editableCanvas.transform);
				GameObjectManager.bind(child.gameObject);
				GameObjectManager.refresh(editableCanvas);
			}
			addNext(gameData.actionsHistory);
			//destroy history
			destroyScript(gameData.actionsHistory);
		}	


    }

	private void enableButtons(int unused){

	}
	private void linkTo(GameObject go){
		if(go.GetComponent<UIActionType>().linkedTo == null){
			if(go.GetComponent<BasicAction>()){
				go.GetComponent<UIActionType>().linkedTo = GameObject.Find(go.GetComponent<BasicAction>().actionType.ToString());
			}			
			else if(go.GetComponent<IfAction>())
				go.GetComponent<UIActionType>().linkedTo = GameObject.Find("If");
			else if(go.GetComponent<ForAction>())
				go.GetComponent<UIActionType>().linkedTo = GameObject.Find("For");
		}
	}

    private void displayEndPanel(GameObject endPanel)
    {
        GameObjectManager.setGameObjectState(endPanel, true);
    }

    private void onDisplayedEndPanel (GameObject endPanel)
    { 
        switch (endPanel.GetComponent<NewEnd>().endType)
        {
            case 1:
                endPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Vous avez été repéré !";
                GameObjectManager.setGameObjectState(endPanel.transform.GetChild(3).gameObject, false);
                endPanel.GetComponent<AudioSource>().clip = Resources.Load("Sound/LoseSound") as AudioClip;
                endPanel.GetComponent<AudioSource>().loop = true;
                endPanel.GetComponent<AudioSource>().Play();
                break;
            case 2:
                //endPanel.transform.GetChild(0).GetComponent<Text>().text = "Bravo vous avez gagné !\n Nombre d'instructions: "+ 
                //gameData.totalActionBloc + "\nNombre d'étape: " + gameData.totalStep +"\nPièces récoltées:" + gameData.totalCoin;

                endPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Bravo vous avez gagné !\nScore: " + (10000 / (gameData.totalActionBloc + 1) + 5000 / (gameData.totalStep + 1) + 6000 / (gameData.totalExecute + 1) + 5000 * gameData.totalCoin);
                endPanel.GetComponent<AudioSource>().clip = Resources.Load("Sound/VictorySound") as AudioClip;
                endPanel.GetComponent<AudioSource>().loop = false;
                endPanel.GetComponent<AudioSource>().Play();
                //End
                if (gameData.levelToLoad >= gameData.levelList.Count - 1)
                {
                    GameObjectManager.setGameObjectState(endPanel.transform.GetChild(3).gameObject, false);
                }
                break;
        }
    }

	// Use to process your families.
	protected override void onProcess(int familiesUpdateCount) {

		//Activate DialogPanel if there is a message
		if(gameData.dialogMessage.Count > 0 && !dialogPanel.activeSelf){
			showDialogPanel();
		}

		//Desactivate Execute & ResetButton if there is a script running
		if(currentActions.Count == 0){
			//actionsPanel.First().SetActive(true);
			GameObjectManager.setGameObjectState(actionsPanel.First(), true);
			gameData.ButtonExec.GetComponent<Button>().interactable = true;
			gameData.ButtonReset.GetComponent<Button>().interactable = true;
		}

	}

	//Refresh Containers size
	private void refreshUI(){
		foreach( GameObject go in playerScript){
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)go.transform );
		}
		
	}

	//Empty the script window
	public void resetScript(){
		GameObject go = GameObject.Find("ScriptContainer");
		for(int i = 0; i < go.transform.childCount; i++){
			destroyScript(go.transform.GetChild(i).gameObject); //,true
		}
		refreshUI();
	}

	public void resetScriptNoRefund(){
		GameObject go = playerScript.First();
		//add actions to history before destroy
		/*
		List<Action> lastActions = new List<Action>();
		lastActions = ActionManipulator.ScriptContainerToActionList(go);
		foreach(Action action in lastActions){
			gameData.actionsHistory.Add(action);
		}
		*/
		//destroy script in editable canvas
		for(int i = 0; i < go.transform.childCount; i++){
			destroyScript(go.transform.GetChild(i).gameObject);
		}
		gameData.ButtonExec.GetComponent<AudioSource>().Play();
		refreshUI();
	}

	//Recursive script destroyer  bool refund = false
	private void destroyScript(GameObject go){
		//refund blocActionLimit
		//if(refund && go.gameObject.GetComponent<UIActionType>() != null){
			//GameObjectManager.removeComponent<Dropped>(go.gameObject);
			//Object.Destroy(go.GetComponent<Available>());
			//ActionManipulator.updateActionBlocLimit(gameData, go.gameObject.GetComponent<UIActionType>().type, 1);
		//}
		if(go.gameObject.GetComponent<UIActionType>() != null){
			gameData.totalActionBloc++;
		}
		
		if(go.gameObject.GetComponent<UITypeContainer>() != null){
			for(int i = 0; i < go.transform.childCount; i++){
				destroyScript(go.transform.GetChild(i).gameObject);
			}
		}
		for(int i = 0; i < go.transform.childCount;i++){
			UnityEngine.Object.Destroy(go.transform.GetChild(i).gameObject);
		}
		go.transform.DetachChildren();
		//GameObjectManager.unbind(go.gameObject);
		UnityEngine.Object.Destroy(go.gameObject);
	}

	public void showDialogPanel(){
		GameObjectManager.setGameObjectState(dialogPanel, true);
		nDialog = 0;
		dialogPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameData.dialogMessage[0];
		if(gameData.dialogMessage.Count > 1){
			setActiveOKButton(false);
			setActiveNextButton(true);
		}
		else{
			setActiveOKButton(true);
			setActiveNextButton(false);
		}
	}

	public void nextDialog(){
		nDialog++;
		dialogPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameData.dialogMessage[nDialog];
		if(nDialog + 1 < gameData.dialogMessage.Count){
			setActiveOKButton(false);
			setActiveNextButton(true);
		}
		else{
			setActiveOKButton(true);
			setActiveNextButton(false);
		}
	}

	public void setActiveOKButton(bool active){
		GameObjectManager.setGameObjectState(dialogPanel.transform.GetChild(1).gameObject, active);
	}

	public void setActiveNextButton(bool active){
		GameObjectManager.setGameObjectState(dialogPanel.transform.GetChild(2).gameObject, active);
	}

	public void closeDialogPanel(){
		nDialog = 0;
		gameData.dialogMessage = new List<string>();;
		GameObjectManager.setGameObjectState(dialogPanel, false);
	}

	public void reloadScene(){
		gameData.nbStep = 0;
		gameData.totalActionBloc = 0;
		gameData.totalStep = 0;
		gameData.totalExecute = 0;
		gameData.totalCoin = 0;
		gameData.dialogMessage = new List<string>();
		GameObjectManager.loadScene("MainScene");
		Debug.Log("reload");
	}

	public void returnToTitleScreen(){
		gameData.nbStep = 0;
		gameData.totalActionBloc = 0;
		gameData.totalStep = 0;
		gameData.totalExecute = 0;
		gameData.totalCoin = 0;
		gameData.dialogMessage = new List<string>();
		GameObjectManager.loadScene("TitleScreen");
	}

	public void nextLevel(){
		gameData.levelToLoad++;
		reloadScene();
		destroyScript(gameData.actionsHistory);
	}

	public void retry(){
		gameData.nbStep = 0;
		gameData.totalActionBloc = 0;
		gameData.totalStep = 0;
		gameData.totalExecute = 0;
		gameData.totalCoin = 0;
		
		gameData.dialogMessage = new List<string>();
		UnityEngine.Object.DontDestroyOnLoad(gameData.actionsHistory);
		GameObjectManager.loadScene("MainScene");
	}

	private void addToHistory(GameObject unused){
		//add actions to history
		if(playerGO.First().GetComponent<ScriptRef>().container.transform.childCount != 0){
			GameObject historyCopy = CopyActionsFrom(playerGO.First().GetComponent<ScriptRef>().container, true);	
			if(gameData.actionsHistory == null){
				gameData.actionsHistory = UnityEngine.Object.Instantiate(historyCopy);
			}
			else{
				for(int i = 0 ; i < historyCopy.transform.childCount ; i++){
					Transform child = UnityEngine.GameObject.Instantiate(historyCopy.transform.GetChild(i));
					child.SetParent(gameData.actionsHistory.transform);
					GameObjectManager.bind(child.gameObject);
					GameObjectManager.refresh(gameData.actionsHistory);
				}
			}
			UnityEngine.Object.Destroy(historyCopy);
			//addNext(gameData.actionsHistory);
			foreach(CurrentAction action in gameData.actionsHistory.GetComponentsInChildren<CurrentAction>()){
				//GameObjectManager.removeComponent<CurrentAction>(action.gameObject);
				UnityEngine.Object.Destroy(action);
			}
		}
		GameObjectManager.removeComponent<HistoryToSave>(MainLoop.instance.gameObject);
	}

	public void applyScriptToPlayer(){
		//save history
		GameObjectManager.addComponent<HistoryToSave>(MainLoop.instance.gameObject);

		//destroy history in container for each robot
		foreach(GameObject robot in playerGO){
			foreach(Transform child in robot.GetComponent<ScriptRef>().container.transform){
				if(child.GetComponent<BaseElement>()){
					//GameObjectManager.addComponent<HistoryToDelete>(child.gameObject);
					child.gameObject.AddComponent<HistoryToDelete>();
					GameObjectManager.refresh(child.gameObject);			
				}
			}
		}
		
		//copy editable script
		GameObject containerCopy = CopyActionsFrom(editableScriptContainer.First(), false);

		foreach(Transform notgo in agentCanvas.First().transform){
			GameObjectManager.setGameObjectState(notgo.gameObject, false);
		}

		foreach( GameObject go in playerGO){
			GameObject targetContainer = go.GetComponent<ScriptRef>().container;
			GameObjectManager.setGameObjectState(targetContainer.transform.parent.parent.gameObject, true);
			for(int i = 0 ; i < containerCopy.transform.childCount ; i++){
				Transform child = UnityEngine.GameObject.Instantiate(containerCopy.transform.GetChild(i));
				//Debug.Log("bind "+child.name);
				child.SetParent(targetContainer.transform);
				GameObjectManager.bind(child.gameObject);
				GameObjectManager.refresh(targetContainer);
			}
			addNext(targetContainer);
		}

		UnityEngine.Object.Destroy(containerCopy);

		//applyIfEntityType();
	}

    public GameObject CopyActionsFrom(GameObject container, bool isInteractable){
		GameObject copyGO = GameObject.Instantiate(container); 
		foreach(TMP_Dropdown drop in copyGO.GetComponentsInChildren<TMP_Dropdown>()){
			drop.interactable = isInteractable;
		}
		foreach(TMP_InputField input in copyGO.GetComponentsInChildren<TMP_InputField>()){
			input.interactable = isInteractable;
		}
		foreach(ForAction forAct in copyGO.GetComponentsInChildren<ForAction>()){
			
			if(!isInteractable){
				forAct.nbFor = int.Parse(forAct.transform.GetChild(0).transform.GetChild(1).GetComponent<TMP_InputField>().text);
				forAct.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text = (forAct.currentFor).ToString() + " / " + forAct.nbFor.ToString();
			}
				
			else{
				forAct.currentFor = 0;
				//Debug.Log("childoutofbounds "+forAct.gameObject.name);
				//Debug.Log("childoutofbounds "+forAct.transform.GetChild(0).gameObject.name);
				//Debug.Log("childoutofbounds "+forAct.transform.GetChild(0).GetChild(1).gameObject.name);
				forAct.gameObject.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text = forAct.nbFor.ToString();
			}

			foreach(BaseElement act in forAct.GetComponentsInChildren<BaseElement>()){
				if(!act.Equals(forAct)){
					forAct.firstChild = act.gameObject;
					break;
				}
			}
		}
		foreach(IfAction IfAct in copyGO.GetComponentsInChildren<IfAction>()){
			IfAct.ifEntityType = IfAct.transform.GetChild(0).Find("DropdownEntityType").GetComponent<TMP_Dropdown>().value;
			IfAct.ifDirection = IfAct.transform.GetChild(0).Find("DropdownDirection").GetComponent<TMP_Dropdown>().value;
			IfAct.range = int.Parse(IfAct.transform.GetChild(0).Find("InputFieldRange").GetComponent<TMP_InputField>().text);
			IfAct.ifNot = (IfAct.transform.GetChild(0).Find("DropdownIsOrIsNot").GetComponent<TMP_Dropdown>().value == 1);
			foreach(BaseElement act in IfAct.GetComponentsInChildren<BaseElement>()){
				if(!act.Equals(IfAct)){
					IfAct.firstChild = act.gameObject;
					break;
				}
			}
		}

		foreach(UITypeContainer typeContainer in copyGO.GetComponentsInChildren<UITypeContainer>()){
			typeContainer.enabled = isInteractable;
		}
		foreach(PointerSensitive pointerSensitive in copyGO.GetComponentsInChildren<PointerSensitive>()){
			pointerSensitive.enabled = isInteractable;
		}


		return copyGO;
	}
	private void addNext(GameObject container){
		int i = 1;
		//for each child, next = next child
		foreach(Transform child in container.transform){
			if(i < container.transform.childCount && child.GetComponent<BaseElement>()){
				child.GetComponent<BaseElement>().next = container.transform.GetChild(i).gameObject;
			}
			//if or for action
			if(child.GetComponent<IfAction>() || child.GetComponent<ForAction>())
				addNext(child.gameObject);
			i++;
		}
		//last child's next = parent 
		if(container.transform.childCount != 0 && container.transform.GetChild(container.transform.childCount-1).GetComponent<BaseElement>()){
			if(container.GetComponent<ForAction>())
				container.transform.GetChild(container.transform.childCount-1).GetComponent<BaseElement>().next = container;
			else if (container.GetComponent<IfAction>()){
				container.transform.GetChild(container.transform.childCount-1).GetComponent<BaseElement>().next = container.GetComponent<IfAction>().next;
			}
		}
					
	}
	
}