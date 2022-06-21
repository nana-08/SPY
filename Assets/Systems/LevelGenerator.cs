using UnityEngine;
using FYFY;
using System.Collections.Generic;
using System.Xml;
using UnityEngine.UI;
using TMPro;
using FYFY_plugins.PointerManager;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// Read XML file and load level
/// </summary>
public class LevelGenerator : FSystem {

	// Famille contenant les agents editables
	private Family levelGO = FamilyManager.getFamily(new AnyOfComponents(typeof(Position), typeof(CurrentAction)));
	private List<List<int>> map;
	private GameData gameData;
	private int nbAgent = 0; // Nombre d'agent cr�er
	public GameObject camera;
	public GameObject editableCanvas;// Le container qui contient les Viewport/script container
	public GameObject scriptContainer;
	public TMP_Text levelName;
	public GameObject canvas;

	protected override void onStart()
    {
		GameObject gameDataGO = GameObject.Find("GameData");
		if (gameDataGO == null)
			GameObjectManager.loadScene("TitleScreen");
		else
		{
			gameData = gameDataGO.GetComponent<GameData>();
			gameData.Level = GameObject.Find("Level");
			XmlDocument doc = new XmlDocument();
			if (Application.platform == RuntimePlatform.WebGLPlayer)
			{
				MainLoop.instance.StartCoroutine(GetLevelWebRequest(doc));
				doc.LoadXml(gameData.levelList[gameData.levelToLoad.Item1][gameData.levelToLoad.Item2]);
				XmlToLevel(doc);
			}
			else
			{
				doc.Load(gameData.levelList[gameData.levelToLoad.Item1][gameData.levelToLoad.Item2]);
				XmlToLevel(doc);
			}
			levelName.text = Path.GetFileNameWithoutExtension(gameData.levelList[gameData.levelToLoad.Item1][gameData.levelToLoad.Item2]);
		}
	}

	IEnumerator GetLevelWebRequest(XmlDocument doc)
	{
		UnityWebRequest www = UnityWebRequest.Get(gameData.levelList[gameData.levelToLoad.Item1][gameData.levelToLoad.Item2]);
		yield return www.SendWebRequest();

		if (www.result != UnityWebRequest.Result.Success)
			Debug.Log(www.error);
		else
		{
			doc.LoadXml(www.downloadHandler.text);
			XmlToLevel(doc);
		}
	}

	private void generateMap(){
		for(int i = 0; i< map.Count; i++){
			for(int j = 0; j < map[i].Count; j++){
				switch (map[i][j]){
					case -1: // void
						break;
					case 0: // Path
						createCell(i,j);
						break;
					case 1: // Wall
						createCell(i,j);
						createWall(i,j);
						break;
					case 2: // Spawn
						createCell(i,j);
						createSpawnExit(i,j,true);
						break;
					case 3: // Exit
						createCell(i,j);
						createSpawnExit(i,j,false);
						break;
				}
			}
		}
	}

	// Creer une entit� agent ou robot et y associe un panel container
	private GameObject createEntity(int i, int j, Direction.Dir direction, string type, List<GameObject> script = null){
		GameObject entity = null;
		switch(type){
			case "player": // Robot
				entity = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Robot Kyle") as GameObject, gameData.Level.transform.position + new Vector3(i*3,1.5f,j*3), Quaternion.Euler(0,0,0), gameData.Level.transform);
				break;
			case "enemy": // Enemy
				entity = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Drone") as GameObject, gameData.Level.transform.position + new Vector3(i*3,5f,j*3), Quaternion.Euler(0,0,0), gameData.Level.transform);
				break;
		}
		// Charger l'agent aux bonnes coordon�es dans la bonne direction
		entity.GetComponent<Position>().x = i;
		entity.GetComponent<Position>().z = j;
		entity.GetComponent<Direction>().direction = direction;
		
		//add new container to entity
		ScriptRef scriptref = entity.GetComponent<ScriptRef>();
		GameObject containerParent = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Container") as GameObject);
		// Associer � l'agent l'UI container
		scriptref.uiContainer = containerParent;
		// Associer � l'agent le script container
		scriptref.scriptContainer = containerParent.transform.Find("Container").Find("Viewport").Find("ScriptContainer").gameObject;
		containerParent.transform.SetParent(scriptContainer.gameObject.transform);
		// Association de l'agent au script de gestion des fonctions
		containerParent.GetComponentInChildren<EditAgentSystemBridge>().agent = entity;

		// On va charger l'image et le nom de l'agent selon l'agent (robot, enemie etc...)
		if (entity.tag == "Player")
		{
			nbAgent++;
			// On nomme l'agent
			AgentEdit agentEdit = entity.GetComponent<AgentEdit>();
			agentEdit.agentName = "Script" + nbAgent;

			// Si l'agent est en mode Locked ou Synchro, on cr�e une zone de programmation d�di�e
			if (agentEdit.editState == AgentEdit.EditMode.Locked || agentEdit.editState == AgentEdit.EditMode.Synch)
			{
				// On cr�e une zone editable associ�e � l'agent
				GameObject scriptContainer = Object.Instantiate<GameObject>(Resources.Load("Prefabs/ViewportScriptContainer") as GameObject);
				GameObjectManager.bind(scriptContainer);
				scriptContainer.transform.SetParent(editableCanvas.transform.Find("EditableContainers"), false);
				// Count this new script
				editableCanvas.GetComponent<EditableCanvacComponent>().nbViewportContainer += 1;
				// ask to refresh Container
				MainLoop.instance.StartCoroutine(DragDropSystem.instance.forceUIRefresh((RectTransform)editableCanvas.transform));
				// On d�finie son nom � celui de l'agent
				scriptContainer.GetComponentInChildren<UIRootContainer>().associedAgentName = agentEdit.agentName;

				// On affiche le bon nom sur le container
				scriptContainer.GetComponentInChildren<TMP_InputField>().text = agentEdit.agentName;
				// Si on est en mode Lock, on bloque l'�dition et on interdit de supprimer le script
				if (agentEdit.editState == AgentEdit.EditMode.Locked)
				{
					scriptContainer.GetComponentInChildren<TMP_InputField>().interactable = false;
					scriptContainer.transform.Find("ScriptContainer").Find("Header").Find("CloseButton").GetComponent<Button>().interactable = false;
				}
			}

			// Chargement de l'ic�ne de l'agent sur la localisation
			containerParent.transform.Find("Header").Find("locateButton").GetComponentInChildren<Image>().sprite = Resources.Load("UI Images/robotIcon", typeof(Sprite)) as Sprite;
			// Affichage du nom de l'agent
			containerParent.transform.Find("Header").Find("agentName").GetComponent<TMP_InputField>().text = entity.GetComponent<AgentEdit>().agentName;
			// Si on autorise le changement de nom on d�v�rouille la possibilit� d'�crire dans la zone de nom du robot
			if (entity.GetComponent<AgentEdit>().editState != AgentEdit.EditMode.Locked)
			{
				containerParent.transform.Find("Header").Find("agentName").GetComponent<TMP_InputField>().interactable = true;
			}
		}
		else if (entity.tag == "Drone")
		{
			// Chargement de l'ic�ne de l'agent sur la localisation
			containerParent.transform.Find("Header").Find("locateButton").GetComponentInChildren<Image>().sprite = Resources.Load("UI Images/droneIcon", typeof(Sprite)) as Sprite;
			// Affichage du nom de l'agent
			containerParent.transform.Find("Header").Find("agentName").GetComponent<TMP_InputField>().text = "Drone";
		}

		AgentColor ac = MainLoop.instance.GetComponent<AgentColor>();
		scriptref.uiContainer.transform.Find("Container").GetComponent<Image>().color = (type == "player" ? ac.playerBackground : ac.droneBackground);

		if(script != null){
			if (type == "player" && editableCanvas.transform.childCount == 1){ //player & empty script (1 child for position bar)
				for(int k = 0 ; k < script.Count ; k++){
					script[k].transform.SetParent(editableCanvas.transform); //add actions to editable container
					GameObjectManager.bind(script[k]);
					GameObjectManager.refresh(editableCanvas);
				}
				foreach(BaseElement act in editableCanvas.GetComponentsInChildren<BaseElement>()){
					GameObjectManager.addComponent<Dropped>(act.gameObject);
				}
				LayoutRebuilder.ForceRebuildLayoutImmediate(editableCanvas.GetComponent<RectTransform>());
			}
			else if(type == "enemy"){
				GameObject targetContainer = entity.GetComponent<ScriptRef>().scriptContainer;
				foreach (GameObject go in script){
					go.transform.SetParent(targetContainer.transform); //add actions to container
					List<GameObject> basicActionGO = getBasicActionGO(go);
					foreach (GameObject baGO in basicActionGO)
					{
						baGO.GetComponent<Image>().color = MainLoop.instance.GetComponent<AgentColor>().droneAction;
						if (baGO.GetComponent<Selectable>() != null)
							baGO.GetComponent<Selectable>().interactable = false;
					}
				}
				computeNext(scriptref.scriptContainer);				
			}			
		}
		containerParent.SetActive(false);
		GameObjectManager.bind(containerParent);
		GameObjectManager.bind(entity);
		return entity;
	}

	private List<GameObject> getBasicActionGO(GameObject go){
		List<GameObject> res = new List<GameObject>();
		if(go.GetComponent<BasicAction>())
			res.Add(go);
		foreach(Transform child in go.transform){
			if(child.GetComponent<BasicAction>())
				res.Add(child.gameObject);
			else {
					List<GameObject> childGO = getBasicActionGO(child.gameObject); 
					foreach(GameObject cgo in childGO){
						res.Add(cgo);
					}
				}		
		}
		return res;
	}

	private void createDoor(int i, int j, Direction.Dir orientation, int slotID){
		GameObject door = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Door") as GameObject, gameData.Level.transform.position + new Vector3(i*3,3,j*3), Quaternion.Euler(0,0,0), gameData.Level.transform);

		door.GetComponent<ActivationSlot>().slotID = slotID;
		door.GetComponent<Position>().x = i;
		door.GetComponent<Position>().z = j;
		door.GetComponent<Direction>().direction = orientation;
		GameObjectManager.bind(door);
	}

	private void createActivable(int i, int j, List<int> slotIDs, Direction.Dir orientation)
	{
		GameObject activable = Object.Instantiate<GameObject>(Resources.Load("Prefabs/ActivableConsole") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 3, j * 3), Quaternion.Euler(0, 0, 0), gameData.Level.transform);

		activable.GetComponent<Activable>().slotID = slotIDs;
		activable.GetComponent<Position>().x = i;
		activable.GetComponent<Position>().z = j;
		activable.GetComponent<Direction>().direction = orientation;
		GameObjectManager.bind(activable);
	}

	private void createSpawnExit(int i, int j, bool type){
		GameObject spawnExit;
		if(type)
			spawnExit = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/TeleporterSpawn") as GameObject, gameData.Level.transform.position + new Vector3(i*3,1.5f,j*3), Quaternion.Euler(-90,0,0), gameData.Level.transform);
		else
			spawnExit = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/TeleporterExit") as GameObject, gameData.Level.transform.position + new Vector3(i*3,1.5f,j*3), Quaternion.Euler(-90,0,0), gameData.Level.transform);

		spawnExit.GetComponent<Position>().x = i;
		spawnExit.GetComponent<Position>().z = j;
		GameObjectManager.bind(spawnExit);
	}

	private void createCoin(int i, int j){
		GameObject coin = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Coin") as GameObject, gameData.Level.transform.position + new Vector3(i*3,3,j*3), Quaternion.Euler(90,0,0), gameData.Level.transform);
		coin.GetComponent<Position>().x = i;
		coin.GetComponent<Position>().z = j;
		GameObjectManager.bind(coin);
	}

	private void createCell(int i, int j){
		GameObject cell = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Cell") as GameObject, gameData.Level.transform.position + new Vector3(i*3,0,j*3), Quaternion.Euler(0,0,0), gameData.Level.transform);
		GameObjectManager.bind(cell);
	}

	private void createWall(int i, int j){
		GameObject wall = Object.Instantiate<GameObject>(Resources.Load ("Prefabs/Wall") as GameObject, gameData.Level.transform.position + new Vector3(i*3,3,j*3), Quaternion.Euler(0,0,0), gameData.Level.transform);
		wall.GetComponent<Position>().x = i;
		wall.GetComponent<Position>().z = j;
		GameObjectManager.bind(wall);
	}

	private void eraseMap(){
		foreach( GameObject go in levelGO){
			GameObjectManager.unbind(go.gameObject);
			Object.Destroy(go.gameObject);
		}
	}

	public void XmlToLevel(XmlDocument doc)
	{

		gameData.totalActionBloc = 0;
		gameData.totalStep = 0;
		gameData.totalExecute = 0;
		gameData.totalCoin = 0;
		gameData.levelToLoadScore = null;
		gameData.dialogMessage = new List<(string, string)>();
		gameData.actionBlocLimit = new Dictionary<string, int>();
		map = new List<List<int>>();

		XmlNode root = doc.ChildNodes[1];
		foreach(XmlNode child in root.ChildNodes){
			switch(child.Name){
				case "map":
					readXMLMap(child);
					break;
				case "dialogs":
					string src = null;
					//optional xml attribute
					if(child.Attributes["img"] !=null)
						src = child.Attributes.GetNamedItem("img").Value;
					gameData.dialogMessage.Add((child.Attributes.GetNamedItem("dialog").Value, src));
					break;
				case "actionBlocLimit":
					readXMLLimits(child);
					break;
				case "competence":
					readXMLCompetence(child);
					break;
				case "coin":
					createCoin(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value));
					break;
				case "activable":
					readXMLActivable(child);
					break;
				case "door":
					createDoor(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value),
					(Direction.Dir)int.Parse(child.Attributes.GetNamedItem("direction").Value), int.Parse(child.Attributes.GetNamedItem("slot").Value));
					break;
				case "player":
					createEntity(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value),
					(Direction.Dir)int.Parse(child.Attributes.GetNamedItem("direction").Value),"player", readXMLScript(child.ChildNodes[0], true));
					break;
				
				case "enemy":
					GameObject enemy = createEntity(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value),
					(Direction.Dir)int.Parse(child.Attributes.GetNamedItem("direction").Value),"enemy", readXMLScript(child.ChildNodes[0]));
					enemy.GetComponent<DetectRange>().range = int.Parse(child.Attributes.GetNamedItem("range").Value);
					enemy.GetComponent<DetectRange>().selfRange = bool.Parse(child.Attributes.GetNamedItem("selfRange").Value);
					enemy.GetComponent<DetectRange>().type = (DetectRange.Type)int.Parse(child.Attributes.GetNamedItem("typeRange").Value);
					break;
				
				case "score":
					gameData.levelToLoadScore = new int[2];
					gameData.levelToLoadScore[0] = int.Parse(child.Attributes.GetNamedItem("threeStars").Value);
					gameData.levelToLoadScore[1] = int.Parse(child.Attributes.GetNamedItem("twoStars").Value);
					break;
			}
		}

		eraseMap();
		generateMap();
        GameObjectManager.addComponent<GameLoaded>(MainLoop.instance.gameObject);
	}

	private void readXMLMap(XmlNode mapNode){
		foreach(XmlNode lineNode in mapNode.ChildNodes){
			List<int> line = new List<int>();
			foreach(XmlNode rowNode in lineNode.ChildNodes){
				line.Add(int.Parse(rowNode.Attributes.GetNamedItem("value").Value));
			}
			map.Add(line);
		}
	}

	private void readXMLCompetence(XmlNode competenceNode)
    {
		for(int i = 0; i < competenceNode.ChildNodes.Count; i++)
        {
			if(competenceNode.ChildNodes[i].Attributes.GetNamedItem("presence").Value == "1"){
				gameData.GetComponent<CompetenceInLevel>().competencPossible[competenceNode.ChildNodes[i].Attributes.GetNamedItem("name").Value] = true;
            }
        }
    }

	private void readXMLLimits(XmlNode limitsNode){
		string actionName = null;
		foreach(XmlNode limitNode in limitsNode.ChildNodes){
			//gameData.actionBlocLimit.Add(int.Parse(limitNode.Attributes.GetNamedItem("limit").Value));
			actionName = limitNode.Attributes.GetNamedItem("actionType").Value;
			if (!gameData.actionBlocLimit.ContainsKey(actionName)){
				gameData.actionBlocLimit[actionName] = int.Parse(limitNode.Attributes.GetNamedItem("limit").Value);
			}
		}
	}

	private void readXMLActivable(XmlNode activableNode){
		List<int> slotsID = new List<int>();

		foreach(XmlNode child in activableNode.ChildNodes){
			slotsID.Add(int.Parse(child.Attributes.GetNamedItem("slot").Value));
		}

		createActivable(int.Parse(activableNode.Attributes.GetNamedItem("posX").Value), int.Parse(activableNode.Attributes.GetNamedItem("posZ").Value),
		 slotsID, (Direction.Dir)int.Parse(activableNode.Attributes.GetNamedItem("direction").Value));
	}

	private List<GameObject> readXMLScript(XmlNode scriptNode, bool editable = false)
	{
		if(scriptNode != null){
			List<GameObject> script = new List<GameObject>();
			foreach(XmlNode actionNode in scriptNode.ChildNodes){
				script.Add(readXMLAction(actionNode, editable));
			}
			return script;			
		}
		return null;
	}

	// Transforme le noeux d'action XML en gameObject action/�l�ment/op�rator
	private GameObject readXMLAction(XmlNode actionNode, bool editable = false){
		GameObject obj = null;
		BaseElement action = null;
		GameObject prefab = null;
		Transform conditionContainer = null;
		Transform firstContainerBloc = null;
		Transform secondeContainerBloc = null;
		bool firstchild;
		string actionKey = actionNode.Attributes.GetNamedItem("actionType").Value;
		switch(actionKey){
			case "If" :
				prefab = Resources.Load ("Prefabs/ScriptElements/IfDetectBloc") as GameObject;
				obj = Object.Instantiate (prefab);
				conditionContainer = obj.transform.Find("ConditionContainer");
				firstContainerBloc = obj.transform.Find("Container");
				obj.GetComponent<LibraryItemRef>().linkedTo = GameObject.Find("If");
				action = obj.GetComponent<IfControl>();

				// On ajoute les �l�ments enfant dans les bons container
				foreach(XmlNode containerNode in actionNode.ChildNodes)
                {
					// Ajout des conditions
					if (containerNode.Attributes.GetNamedItem("container").Value == "Condition")
                    {
						if (containerNode.HasChildNodes)
						{
							GameObject emptyZone = conditionContainer.GetChild(0).gameObject;
							foreach (XmlNode eleNode in containerNode.ChildNodes)
							{
								GameObject child = (readXMLElement(eleNode, editable));
								child.transform.SetParent(conditionContainer);
								child.transform.SetSiblingIndex(emptyZone.transform.GetSiblingIndex());
							}
							emptyZone.transform.SetParent(null);
							GameObject.Destroy(emptyZone);
							GameObjectManager.refresh(obj);

							((IfControl)action).condition = new List<string>();
							ConditionManagement.instance.convertionConditionSequence(conditionContainer.GetChild(0).gameObject, ((IfControl)action).condition);
						}
                    }
					else if (containerNode.Attributes.GetNamedItem("container").Value == "IfContainer")
                    {
						if (containerNode.HasChildNodes)
						{
							GameObject emptyZone = firstContainerBloc.GetChild(1).gameObject;
							firstchild = false;
							foreach (XmlNode eleNode in containerNode.ChildNodes)
							{
								GameObject child = (readXMLAction(eleNode, editable));
								child.transform.SetParent(firstContainerBloc);
								child.transform.SetSiblingIndex(emptyZone.transform.GetSiblingIndex());
								if (!firstchild)
								{
									firstchild = true;
									((IfControl)action).firstChild = child;
								}
							}
							emptyZone.transform.SetParent(null);
							GameObject.Destroy(emptyZone);
							GameObjectManager.refresh(obj);
						}
					}
				}
				break;

			case "IfElse":
				prefab = Resources.Load("Prefabs/ScriptElements/IfElseDetectBloc") as GameObject;
				obj = Object.Instantiate(prefab);
				conditionContainer = obj.transform.Find("ConditionContainer");
				firstContainerBloc = obj.transform.Find("Container");
				secondeContainerBloc = obj.transform.Find("ElseContainer");
				obj.GetComponent<LibraryItemRef>().linkedTo = GameObject.Find("IfElse");
				action = obj.GetComponent<IfControl>();

				// On ajoute les �l�ments enfant dans les bons container
				foreach (XmlNode containerNode in actionNode.ChildNodes)
				{
					// Ajout des conditions
					if (containerNode.Attributes.GetNamedItem("container").Value == "Condition")
					{
						if (containerNode.HasChildNodes)
						{
							GameObject emptyZone = conditionContainer.GetChild(0).gameObject;
							foreach (XmlNode eleNode in containerNode.ChildNodes)
							{
								GameObject child = (readXMLElement(eleNode, editable));
								child.transform.SetParent(conditionContainer);
								child.transform.SetSiblingIndex(emptyZone.transform.GetSiblingIndex());
							}
							emptyZone.transform.SetParent(null);
							GameObject.Destroy(emptyZone);
							GameObjectManager.refresh(obj);

							((IfControl)action).condition = new List<string>();
							ConditionManagement.instance.convertionConditionSequence(conditionContainer.GetChild(0).gameObject, ((IfControl)action).condition);
						}
					}
					else if (containerNode.Attributes.GetNamedItem("container").Value == "IfContainer")
					{
						if (containerNode.HasChildNodes)
						{
							GameObject emptyZone = firstContainerBloc.GetChild(1).gameObject;
							firstchild = false;
							foreach (XmlNode eleNode in containerNode.ChildNodes)
							{
								GameObject child = (readXMLAction(eleNode, editable));
								child.transform.SetParent(firstContainerBloc);
								child.transform.SetSiblingIndex(emptyZone.transform.GetSiblingIndex());
								if (!firstchild)
								{
									firstchild = true;
									((IfControl)action).firstChild = child;
								}
							}
							emptyZone.transform.SetParent(null);
							GameObject.Destroy(emptyZone);
							GameObjectManager.refresh(obj);
							
						}
					}
					else if (containerNode.Attributes.GetNamedItem("container").Value == "ElseContainer")
                    {
						if (containerNode.HasChildNodes)
						{
							GameObject emptyZone = secondeContainerBloc.GetChild(1).gameObject;
							firstchild = false;
							foreach (XmlNode eleNode in containerNode.ChildNodes)
							{
								GameObject child = (readXMLAction(eleNode, editable));
								child.transform.SetParent(secondeContainerBloc);
								child.transform.SetSiblingIndex(emptyZone.transform.GetSiblingIndex());
								if (!firstchild)
								{
									firstchild = true;
									((IfElseControl)action).elseFirstChild = child;
								}
							}
							emptyZone.transform.SetParent(null);
							GameObject.Destroy(emptyZone);
							GameObjectManager.refresh(obj);
						}
					}
				}
				break;

			case "For":
				prefab = Resources.Load ("Prefabs/ScriptElements/ForBloc") as GameObject;
				obj = Object.Instantiate (prefab);
				obj.GetComponent<LibraryItemRef>().linkedTo = GameObject.Find("For");
				action = obj.GetComponent<ForControl>();
				firstContainerBloc = obj.transform.Find("Container");

				((ForControl)action).nbFor = int.Parse(actionNode.Attributes.GetNamedItem("nbFor").Value);
				obj.transform.GetComponentInChildren<TMP_InputField>().text = ((ForControl)action).nbFor.ToString();

				obj.transform.GetComponentInChildren<TMP_InputField>().interactable = editable;

				if (actionNode.HasChildNodes)
				{
					GameObject emptyZone = firstContainerBloc.GetChild(1).gameObject;
					firstchild = false;
					foreach (XmlNode eleNode in actionNode.ChildNodes)
					{
						GameObject child = (readXMLAction(eleNode, editable));
						child.transform.SetParent(firstContainerBloc);
						child.transform.SetSiblingIndex(emptyZone.transform.GetSiblingIndex());
						if (!firstchild)
						{
							firstchild = true;
							((ForControl)action).firstChild = child;
						}
					}
					emptyZone.transform.SetParent(null);
					GameObject.Destroy(emptyZone);
					GameObjectManager.refresh(obj);
				}
				break;

			case "While":
				prefab = Resources.Load("Prefabs/ScriptElements/WhileBloc") as GameObject;
				obj = Object.Instantiate(prefab);
				obj.GetComponent<LibraryItemRef>().linkedTo = GameObject.Find("For");
				action = obj.GetComponent<WhileControl>();
				firstContainerBloc = obj.transform.Find("Container");
				conditionContainer = obj.transform.Find("ConditionContainer");

				// On ajoute les �l�ments enfant dans les bons container
				foreach (XmlNode containerNode in actionNode.ChildNodes)
				{
					// Ajout des conditions
					if (containerNode.Attributes.GetNamedItem("container").Value == "Condition")
					{
						if (containerNode.HasChildNodes)
						{
							GameObject emptyZone = conditionContainer.GetChild(0).gameObject;
							foreach (XmlNode eleNode in containerNode.ChildNodes)
							{
								GameObject child = (readXMLElement(eleNode, editable));
								child.transform.SetParent(conditionContainer);
								child.transform.SetSiblingIndex(emptyZone.transform.GetSiblingIndex());
							}
							emptyZone.transform.SetParent(null);
							GameObject.Destroy(emptyZone);
							GameObjectManager.refresh(obj);

							((IfControl)action).condition = new List<string>();
							ConditionManagement.instance.convertionConditionSequence(conditionContainer.transform.GetChild(0).gameObject, ((IfControl)action).condition);
						
							}
					}
					else if (containerNode.Attributes.GetNamedItem("container").Value == "IfContainer")
					{
						if (containerNode.HasChildNodes)
						{
							GameObject emptyZone = firstContainerBloc.GetChild(1).gameObject;
							firstchild = false;
							foreach (XmlNode eleNode in containerNode.ChildNodes)
							{
								GameObject child = (readXMLAction(eleNode, editable));
								child.transform.SetParent(firstContainerBloc);
								child.transform.SetSiblingIndex(emptyZone.transform.GetSiblingIndex());
								if (!firstchild)
								{
									firstchild = true;
									((IfControl)action).firstChild = child;
								}
							}
							emptyZone.transform.SetParent(null);
							GameObject.Destroy(emptyZone);
							GameObjectManager.refresh(obj);
							
						}
					}
				}
				break;

			case "Forever":
				prefab = Resources.Load ("Prefabs/ScriptElements/InfiniteLoop") as GameObject;
				obj = Object.Instantiate (prefab);
				obj.GetComponent<LibraryItemRef>().linkedTo = null;
				action = obj.GetComponent<ForeverControl>();

				//add children
				firstchild = false;
				if(actionNode.HasChildNodes){
					foreach(XmlNode actNode in actionNode.ChildNodes){
						GameObject child = (readXMLAction(actNode, editable));
						child.transform.SetParent(action.transform.Find("Container"));
						if(!firstchild){
							firstchild = true;
							((ForeverControl)action).firstChild = child;
						}
					}	
				}
				break;

			default:
				prefab = Resources.Load("Prefabs/ScriptElements/" + actionKey + "ActionBloc") as GameObject;
				obj = Object.Instantiate(prefab);
				obj.GetComponent<LibraryItemRef>().linkedTo = GameObject.Find(actionKey);
				break;
		}

		if(!editable)
			Object.Destroy(obj.GetComponent<PointerSensitive>());

		return obj;
	}

	// Transforme le noeux d'action XML en gameObject �l�ment/op�rator
	private GameObject readXMLElement(XmlNode actionNode, bool editable = false) {
		GameObject obj = null;
		GameObject prefab = null;
		Debug.Log("Element name : " + actionNode.Attributes.GetNamedItem("actionType").Value);
		string actionKey = actionNode.Attributes.GetNamedItem("actionType").Value;
		switch (actionKey)
		{
			case "AndOperator":
				prefab = Resources.Load("Prefabs/ScriptElements/" + actionKey + "Element") as GameObject;
				obj = Object.Instantiate(prefab);
				if (actionNode.HasChildNodes)
				{
					GameObject endZone = null;
					foreach (XmlNode andNode in actionNode.ChildNodes)
					{
						if (andNode.Attributes.GetNamedItem("element").Value == "1")
						{
							GameObject child = (readXMLElement(andNode, editable));
							endZone = obj.transform.GetChild(1).gameObject;
							child.transform.SetParent(obj.transform);
							child.transform.SetSiblingIndex(endZone.transform.GetSiblingIndex());
							endZone.SetActive(false);
						}
						else if (andNode.Attributes.GetNamedItem("element").Value == "2")
						{
							GameObject child = (readXMLElement(andNode, editable));
							endZone = obj.transform.GetChild(4).gameObject;
							child.transform.SetParent(obj.transform);
							child.transform.SetSiblingIndex(endZone.transform.GetSiblingIndex());
							endZone.SetActive(false);
						}
					}
				}
				break;

			case "OrOperator":
				prefab = Resources.Load("Prefabs/ScriptElements/" + actionKey + "Element") as GameObject;
				obj = Object.Instantiate(prefab);
				if (actionNode.HasChildNodes)
				{
					GameObject endZone = null;
					foreach (XmlNode orNode in actionNode.ChildNodes)
                    {
						if(orNode.Attributes.GetNamedItem("element").Value == "1")
                        {
							GameObject child = (readXMLElement(orNode, editable));
							endZone = obj.transform.GetChild(1).gameObject;
							child.transform.SetParent(obj.transform);
							child.transform.SetSiblingIndex(endZone.transform.GetSiblingIndex());
							endZone.SetActive(false);
						}
						else if (orNode.Attributes.GetNamedItem("element").Value == "2")
                        {
							GameObject child = (readXMLElement(orNode, editable));
							endZone = obj.transform.GetChild(4).gameObject;
							child.transform.SetParent(obj.transform);
							child.transform.SetSiblingIndex(endZone.transform.GetSiblingIndex());
							endZone.SetActive(false);
						}
                    }
				}
				break;

			case "NotOperator":
				prefab = Resources.Load("Prefabs/ScriptElements/" + actionKey + "Element") as GameObject;
				obj = Object.Instantiate(prefab);
				if (actionNode.HasChildNodes)
				{
					GameObject child = (readXMLElement(actionNode.FirstChild, editable));
					GameObject endZone = obj.transform.GetChild(1).gameObject;
					child.transform.SetParent(obj.transform);
					child.transform.SetSiblingIndex(endZone.transform.GetSiblingIndex());
					endZone.SetActive(false);
				}
				break;

			default:
				prefab = Resources.Load("Prefabs/ScriptElements/" + actionKey + "Element") as GameObject;
				obj = Object.Instantiate(prefab);
				break;
		}

		return obj;
	}

	// link actions together => define next property
	// Associe � chaque bloc le bloc qui sera execut� apr�s
	public static void computeNext(GameObject container){
		for (int i = 0 ; i < container.transform.childCount ; i++){
			Transform child = container.transform.GetChild(i);
			// Si l'action est une action basique et n'est pas la derni�re
			if (i < container.transform.childCount && child.GetComponent<BaseElement>()){
				// Si le bloc appartient � un for, il faut que le dernier �l�ment ait comme next le block for
				if ((container.transform.parent.GetComponent<ForeverControl>() || container.transform.parent.GetComponent<ForControl>()) && i == container.transform.childCount - 1)
				{
					child.GetComponent<BaseElement>().next = container.transform.parent.gameObject;
					i = container.transform.childCount;
				}// Si le bloc appartient � un if et qu'il est le dernier block de la partie action
				else if (container.transform.parent.GetComponent<IfControl>() && i == container.transform.childCount - 1) {
					// On regarde si il reste des �l�ments dans le container parent
					// Si oui on met l'�l�ment suivant en next
					// Sinon on ne fait rien et fin de la sequence
					if(container.transform.parent.parent.childCount - 1 > container.transform.parent.GetSiblingIndex())
                    {
						child.GetComponent<BaseElement>().next = container.transform.parent.parent.GetChild(container.transform.parent.GetSiblingIndex() + 1).gameObject;
					}
                    else
                    {
						// Exception, si le container parent parent est un for, on le met en next
						if (container.transform.parent.parent.parent.GetComponent<ForControl>() || container.transform.parent.parent.parent.GetComponent<ForeverControl>())
                        {
							child.GetComponent<BaseElement>().next = container.transform.parent.parent.parent.gameObject;
						}
                    }
				}// Sinon l'associer au block suivant
				else if (i != container.transform.childCount - 1)
				{
					child.GetComponent<BaseElement>().next = container.transform.GetChild(i + 1).gameObject;
				}
			}// Sinon si c'est la derni�re et une action basique
			else if(i == container.transform.childCount-1 && child.GetComponent<BaseElement>() && container.GetComponent<BaseElement>()){
				if(container.GetComponent<ForControl>() || container.GetComponent<ForeverControl>())
					child.GetComponent<BaseElement>().next = container;
				else if(container.GetComponent<IfControl>())
					child.GetComponent<BaseElement>().next = container.GetComponent<BaseElement>().next;
			}
			// Si autre action que les actions basique
			// Alors r�cursive de la fonction sur leur container
			if(child.GetComponent<IfControl>() || child.GetComponent<ForControl>())
            {
				computeNext(child.transform.Find("Container").gameObject);
                // Si c'est un esle il ne faut pas oublier le container else
                if (child.GetComponent<IfElseControl>())
                {
					computeNext(child.transform.Find("ElseContainer").gameObject);
				}
			}
			else if (child.GetComponent<ForeverControl>())
            {
				computeNext(child.transform.Find("Container").gameObject);
			}
		}
	}
}
