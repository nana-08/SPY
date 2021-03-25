﻿using UnityEngine;
using FYFY;
using UnityEngine.UI;
using FYFY_plugins.TriggerManager;
using System.Collections;
using TMPro;

public class ApplyScriptSystem : FSystem {
	private Family wallGO = FamilyManager.getFamily(new AllOfComponents(typeof(Position), typeof(BoxCollider), typeof(MeshRenderer)), new AnyOfTags("Wall"));
	private Family playerGO = FamilyManager.getFamily(new AllOfComponents(typeof(Script),typeof(Position)), new AnyOfTags("Player"));
	private Family activableConsoleGO = FamilyManager.getFamily(new AllOfComponents(typeof(Activable),typeof(Position), typeof(MeshRenderer), typeof(AudioSource), typeof(MeshFilter)));
    private Family newStep_f = FamilyManager.getFamily(new AllOfComponents(typeof(NewStep)));
	private Family playerScriptContainer_f = FamilyManager.getFamily(new AllOfComponents(typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(Image), typeof(UITypeContainer)), new AnyOfTags("ScriptConstructor"));
	private Family scriptedGO = FamilyManager.getFamily(new AllOfComponents(typeof(Script), typeof(Position), typeof(Direction), typeof(HighLight), typeof(AudioSource)));
	private Family exitGO = FamilyManager.getFamily(new AllOfComponents(typeof(Position), typeof(MeshRenderer), typeof(MeshFilter), typeof(AudioSource)), new AnyOfTags("Exit"));
    private Family endpanel_f = FamilyManager.getFamily(new AllOfComponents(typeof(Image), typeof(AudioSource)), new AnyOfTags("endpanel"));
    private Family robotcollision_f = FamilyManager.getFamily(new AllOfComponents(typeof(Triggered3D)), new AnyOfTags("Player"));
	private Family droneGO = FamilyManager.getFamily(new AllOfComponents(typeof(Script), typeof(Position), typeof(Direction), typeof(HighLight), typeof(AudioSource)), new AnyOfTags("Drone"));
	private Family doorGO = FamilyManager.getFamily(new AllOfComponents(typeof(ActivationSlot), typeof(Direction), typeof(MeshRenderer), typeof(BoxCollider), typeof(Position), typeof(AudioSource)));
	private Family redDetectorGO = FamilyManager.getFamily(new AllOfComponents(typeof(Rigidbody), typeof(Detector), typeof(Position), typeof(BoxCollider), typeof(MeshRenderer), typeof(MeshFilter)));
	private Family coinGO = FamilyManager.getFamily(new AllOfComponents(typeof(CapsuleCollider), typeof(Position), typeof(ParticleSystem), typeof(MeshRenderer)), new AnyOfTags("Coin"));
	private GameObject endPanel;
	private GameData gameData;

	public ApplyScriptSystem(){
		gameData = GameObject.Find("GameData").GetComponent<GameData>();
        newStep_f.addEntryCallback(onNewStep);
        endPanel = endpanel_f.First();
        GameObjectManager.setGameObjectState(endPanel, false);
        robotcollision_f.addEntryCallback(onNewCollision);
    }

    private void onNewCollision(GameObject robot){
        Triggered3D trigger = robot.GetComponent<Triggered3D>();
        foreach(GameObject target in trigger.Targets){
            //Check if the player collide with a detection cell
            if (target.GetComponent<Detector>() != null){
                //end level
                Debug.Log("Repéré !");
                GameObjectManager.addComponent<NewEnd>(endPanel, new { endType = NewEnd.Detected });
            }
        }
    }

	//Return the current action
    public static Action getCurrentAction(GameObject go) {
		Action action = go.GetComponent<Script>().actions[go.GetComponent<Script>().currentAction]; 
		//end when a pure action is found
		while(!(action.actionType == Action.ActionType.Forward || action.actionType == Action.ActionType.TurnLeft || action.actionType == Action.ActionType.TurnRight
				|| action.actionType == Action.ActionType.Wait || action.actionType == Action.ActionType.Activate || action.actionType == Action.ActionType.TurnBack)){
			//Case For / If
			if(action.actionType == Action.ActionType.For || action.actionType == Action.ActionType.If){
				action = action.actions[action.currentAction];
			}
		}
		return action;
	}

	// Use to process your families.
	private void onNewStep(GameObject unused) {

        foreach ( GameObject go in scriptedGO){
				
			if(!endOfScript(go)){
				Action action = getCurrentAction(go);
					
				switch (action.actionType){
					case Action.ActionType.Forward:
						ApplyForward(go);
						break;

					case Action.ActionType.TurnLeft:
						ApplyTurnLeft(go);
						break;

					case Action.ActionType.TurnRight:
						ApplyTurnRight(go);
						break;
					case Action.ActionType.TurnBack:
						ApplyTurnBack(go);
						break;
					case Action.ActionType.Wait:
						break;
					case Action.ActionType.Activate:
						foreach( GameObject actGo in activableConsoleGO){
							if(actGo.GetComponent<Position>().x == go.GetComponent<Position>().x && actGo.GetComponent<Position>().z == go.GetComponent<Position>().z){
								actGo.GetComponent<AudioSource>().Play();
								actGo.GetComponent<Activable>().isActivated = true;
							}
						}
						break;
				}
				incrementActionScript(go.GetComponent<Script>());
			}
		}
        //Check if the player is on the end of the level
        int nbEnd = 0;
        foreach (GameObject player in playerGO)
        {
            foreach (GameObject exit in exitGO)
            {
                if (gameData.nbStep == 1 && player.GetComponent<Position>().x == exit.GetComponent<Position>().x && player.GetComponent<Position>().z == exit.GetComponent<Position>().z)
                {
                    nbEnd++;
                    //end level
                    if (nbEnd >= playerGO.Count)
                    {
                        Debug.Log("Fin du niveau");
                        GameObjectManager.addComponent<NewEnd>(endPanel, new { endType = NewEnd.Win });
                    }
                }
            }
        }

        //Check Activations
        foreach (GameObject activable in activableConsoleGO)
        {
            if (activable.GetComponent<Activable>().isActivated && !activable.GetComponent<Activable>().isFullyActivated)
            {
                activate(activable);
            }
        }

        applyIfEntityType();
	}

	private void activate(GameObject go){
		go.GetComponent<Activable>().isFullyActivated = true;
		foreach(int id in go.GetComponent<Activable>().slotID){
			foreach(GameObject slotGo in doorGO){
				if(slotGo.GetComponent<ActivationSlot>().slotID == id){
					switch(slotGo.GetComponent<ActivationSlot>().type){
						case ActivationSlot.ActivationType.Destroy:
							MainLoop.instance.StartCoroutine(doorDestroy(slotGo));
							break;
					}
				}
			}
		}
	}

	private IEnumerator doorDestroy(GameObject go){

		yield return new WaitForSeconds(0.3f);

		go.GetComponent<Renderer>().enabled = false;
		go.GetComponent<AudioSource>().Play();
		
		yield return new WaitForSeconds(0.5f);
		GameObjectManager.unbind(go);
		Object.Destroy(go);
	}
	private void ApplyForward(GameObject go){
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				if(!checkObstacle(go.GetComponent<Position>().x,go.GetComponent<Position>().z + 1)){
					go.GetComponent<Position>().x = go.GetComponent<Position>().x;
					go.GetComponent<Position>().z = go.GetComponent<Position>().z + 1;
				}
				break;
			case Direction.Dir.South:
				if(!checkObstacle(go.GetComponent<Position>().x,go.GetComponent<Position>().z - 1)){
					go.GetComponent<Position>().x = go.GetComponent<Position>().x;
					go.GetComponent<Position>().z = go.GetComponent<Position>().z - 1;
				}
				break;
			case Direction.Dir.East:
				if(!checkObstacle(go.GetComponent<Position>().x + 1,go.GetComponent<Position>().z)){
					go.GetComponent<Position>().x = go.GetComponent<Position>().x + 1;
					go.GetComponent<Position>().z = go.GetComponent<Position>().z;
				}
				break;
			case Direction.Dir.West:
				if(!checkObstacle(go.GetComponent<Position>().x - 1,go.GetComponent<Position>().z)){
					go.GetComponent<Position>().x = go.GetComponent<Position>().x - 1;
					go.GetComponent<Position>().z = go.GetComponent<Position>().z;
				}
				break;
		}
	}

	private void ApplyTurnLeft(GameObject go){
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				go.GetComponent<Direction>().direction = Direction.Dir.West;
				break;
			case Direction.Dir.South:
				go.GetComponent<Direction>().direction = Direction.Dir.East;
				break;
			case Direction.Dir.East:
				go.GetComponent<Direction>().direction = Direction.Dir.North;
				break;
			case Direction.Dir.West:
				go.GetComponent<Direction>().direction = Direction.Dir.South;
				break;
		}
	}

	private void ApplyTurnRight(GameObject go){
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				go.GetComponent<Direction>().direction = Direction.Dir.East;
				break;
			case Direction.Dir.South:
				go.GetComponent<Direction>().direction = Direction.Dir.West;
				break;
			case Direction.Dir.East:
				go.GetComponent<Direction>().direction = Direction.Dir.South;
				break;
			case Direction.Dir.West:
				go.GetComponent<Direction>().direction = Direction.Dir.North;
				break;
		}
	}

	private void ApplyTurnBack(GameObject go){
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				go.GetComponent<Direction>().direction = Direction.Dir.South;
				break;
			case Direction.Dir.South:
				go.GetComponent<Direction>().direction = Direction.Dir.North;
				break;
			case Direction.Dir.East:
				go.GetComponent<Direction>().direction = Direction.Dir.West;
				break;
			case Direction.Dir.West:
				go.GetComponent<Direction>().direction = Direction.Dir.East;
				break;
		}
	}

	private bool checkObstacle(int x, int z){
		foreach( GameObject go in wallGO){
			if(go.GetComponent<Position>().x == x && go.GetComponent<Position>().z == z)
				return true;
		}
		return false;
	}

	public void applyScriptToPlayer(){
		foreach( GameObject go in playerGO){
			go.GetComponent<Script>().actions = ActionManipulator.ScriptContainerToActionList(playerScriptContainer_f.First());
			foreach(Action act in go.GetComponent<Script>().actions){
				Debug.Log("action : "+act.actionType.ToString());
				//Debug.Log(act.actions.Count);
			}
			//Debug.Log("actions = "+go.GetComponent<Script>().actions);
            go.GetComponent<Script>().currentAction = 0;
            gameData.nbStep = getNbStep(go.GetComponent<Script>());
		}

		applyIfEntityType();
		
		if(gameData.nbStep > 0){
			gameData.totalExecute++;
		}
	}

	public void applyIfEntityType(){
		//Check if If actions are valid
		int nbStepToAdd = 0;
		foreach( GameObject scripted in scriptedGO){
			int nbStepPlayer = 0;
			invalidAllIf(scripted.GetComponent<Script>());
			Action nextIf = getCurrentIf(scripted);
			while(nextIf != null && !endOfScript(scripted)){
				//Check if ok
				bool ifok = nextIf.ifNot;
				Vector2 vec = new Vector2();
				switch(getDirection(scripted.GetComponent<Direction>().direction,nextIf.ifDirection)){
					case Direction.Dir.North:
						vec = new Vector2(0,1);
						break;
					case Direction.Dir.South:
						vec = new Vector2(0,-1);
						break;
					case Direction.Dir.East:
						vec = new Vector2(1,0);
						break;
					case Direction.Dir.West:
						vec = new Vector2(-1,0);
						break;
				}
				switch(nextIf.ifEntityType){
					case 0:
						for(int i = 1; i <= nextIf.range; i++){
							foreach( GameObject go in wallGO){
								if(go.GetComponent<Position>().x == scripted.GetComponent<Position>().x + vec.x * i && go.GetComponent<Position>().z == scripted.GetComponent<Position>().z + vec.y * i){
									ifok = !nextIf.ifNot;
								}
							}
						}
						break;
					case 1:
						for(int i = 1; i <= nextIf.range; i++){
							foreach( GameObject go in doorGO){
								if(go.GetComponent<Position>().x == scripted.GetComponent<Position>().x + vec.x * i && go.GetComponent<Position>().z == scripted.GetComponent<Position>().z + vec.y * i){
									ifok = !nextIf.ifNot;
								}
							}
						}
						break;
					case 2:
						for(int i = 1; i <= nextIf.range; i++){
							foreach( GameObject go in droneGO){
								if(go.GetComponent<Position>().x == scripted.GetComponent<Position>().x + vec.x * i && go.GetComponent<Position>().z == scripted.GetComponent<Position>().z + vec.y * i){
									ifok = !nextIf.ifNot;
								}
							}
						}
						break;
					case 3:
						for(int i = 1; i <= nextIf.range; i++){
							foreach( GameObject go in playerGO){
								if(go.GetComponent<Position>().x == scripted.GetComponent<Position>().x + vec.x * i && go.GetComponent<Position>().z == scripted.GetComponent<Position>().z + vec.y * i){
									ifok = !nextIf.ifNot;
								}
							}
						}
						break;
					case 4:
						for(int i = 1; i <= nextIf.range; i++){
							foreach( GameObject go in activableConsoleGO){
								if(go.GetComponent<Position>().x == scripted.GetComponent<Position>().x + vec.x * i && go.GetComponent<Position>().z == scripted.GetComponent<Position>().z + vec.y * i){
									ifok = !nextIf.ifNot;
								}
							}
						}
						break;
					case 5:
						for(int i = 1; i <= nextIf.range; i++){
							foreach( GameObject go in redDetectorGO){
								if(go.GetComponent<Position>().x == scripted.GetComponent<Position>().x + vec.x * i && go.GetComponent<Position>().z == scripted.GetComponent<Position>().z + vec.y * i){
									ifok = !nextIf.ifNot;
								}
							}
						}
						break;
					case 6:
						for(int i = 1; i <= nextIf.range; i++){
							foreach( GameObject go in coinGO){
								if(go.GetComponent<Position>().x == scripted.GetComponent<Position>().x + vec.x * i && go.GetComponent<Position>().z == scripted.GetComponent<Position>().z + vec.y * i){
									ifok = !nextIf.ifNot;
								}
							}
						}
						break;				
				}

				if(ifok){
					nextIf.ifValid = true;
					if(scripted.tag == "Player"){
						nbStepPlayer += getNbStep(nextIf, true);
					}
				}
				else{
					nextIf.currentAction = nextIf.actions.Count-1;
					incrementActionScript(scripted.GetComponent<Script>());
				}
				nextIf = getCurrentIf(scripted);
			}

			if(nbStepPlayer > nbStepToAdd){
				nbStepToAdd = nbStepPlayer;
			}
		}
		gameData.nbStep += nbStepToAdd;

	}

	//Return true if the script is at the end
    public static bool endOfScript(GameObject go){
		return go.GetComponent<Script>().currentAction >= go.GetComponent<Script>().actions.Count;
	}

	public static Action getCurrentIf(GameObject go){
		//Debug.Log("currentif "+go.name);
		if(go.GetComponent<Script>().actions == null || go.GetComponent<Script>().currentAction >= go.GetComponent<Script>().actions.Count){
			//Debug.Log("if condition1 : "+go.GetComponent<Script>().actions);
			//Debug.Log("if condition2 : "+go.GetComponent<Script>().currentAction + " >= "+go.GetComponent<Script>().actions.Count);
			return null;
		}
		Action action = go.GetComponent<Script>().actions[go.GetComponent<Script>().currentAction]; 
		//end when a pure action is found
		while(!(action.actionType == Action.ActionType.Forward || action.actionType == Action.ActionType.TurnLeft || action.actionType == Action.ActionType.TurnRight
				|| action.actionType == Action.ActionType.Wait || action.actionType == Action.ActionType.Activate || action.actionType == Action.ActionType.TurnBack)){
			//Debug.Log("while");
			//Case For / If
			if(action.actionType == Action.ActionType.For){
				if(action.currentAction >= action.actions.Count){
					return null;
				}
				action = action.actions[action.currentAction];
			}
			if(action.actionType == Action.ActionType.If){
				if(action.currentAction == 0 && !action.ifValid){
					//Debug.Log("if &&");
					//Debug.Log(action);
					//Debug.Log(action.actions.Count);
					return action;
				}
				else{
					if(action.currentAction >= action.actions.Count){
						//Debug.Log("else if");
						return null;
					}
					action = action.actions[action.currentAction];
				}
			}
		}

		return null;
	}

	//increment the iterator of the action script
	public static void incrementActionScript(Script script){
		//Debug.Log("i = "+script.currentAction);
		Action action = script.actions[script.currentAction];
		//Debug.Log("1 -- target : " + ((action.target !=null)? action.target.name:"null"));
		if(incrementAction(action)){
			//remove highlight of previous action	
			if (script.currentAction > 0 && script.actions[script.currentAction-1].target != null &&
			script.actions[script.currentAction-1].target.GetComponent<HighLight>() != null){
				Debug.Log("remove highlight1");
				GameObjectManager.removeComponent<HighLight>(script.actions[script.currentAction-1].target);					
			}
			//add highlight to current action
			if(action.target != null && action.target.GetComponent<HighLight>() == null){
				Debug.Log("add highlight1");
				//if(action.actionType != Action.ActionType.For && action.actionType != Action.ActionType.If)
				GameObjectManager.addComponent<HighLight>(action.target);
				//if(action.actionType == Action.ActionType.For || action.actionType == Action.ActionType.If)
				//	GameObjectManager.addComponent<HighLight>(action.actions[0].target);
				//else{
					//GameObjectManager.addComponent<HighLight>(action.actions[0].target);
				//}
			}

			/*
			else if(script.currentAction > 0 && script.actions[script.currentAction-1].target != null && script.actions[script.currentAction-1].target.GetComponent<HighLight>()){
				Debug.Log("remove highlight");
				GameObjectManager.removeComponent<HighLight>(script.actions[script.currentAction].target);
			}
			*/
			Debug.Log("++");
			script.currentAction++;

			/*
			if(script.actions[script.currentAction].target != null && script.actions[script.currentAction].target.GetComponent<HighLight>() != null){
				Debug.Log("remove highlight");
				//GameObjectManager.removeComponent<HighLight>(script.actions[script.currentAction].target);
			}*/
		}
		
		if(script.currentAction >= script.actions.Count && script.repeat)
			script.currentAction = 0;
		/*
		Debug.Log("boo : "+script.currentAction+"/"+script.actions.Count);
		Debug.Log((action.target.GetComponent<HighLight>() == null)? "null": "pas null");
		//remove last highlight
		if(action.target.GetComponent<HighLight>() != null && script.currentAction == script.actions.Count){ 
			Debug.Log("BOO");
			GameObjectManager.removeComponent<HighLight>(action.target);
		}
		*/

	}

    public static bool incrementAction(Action act){
		if(act.actionType == Action.ActionType.Forward || act.actionType == Action.ActionType.TurnLeft || act.actionType == Action.ActionType.TurnRight
			|| act.actionType == Action.ActionType.Wait || act.actionType == Action.ActionType.Activate || act.actionType == Action.ActionType.TurnBack){
			/*
			if(act.target != null && act.target.GetComponent<HighLight>() != null){
				Debug.Log("target : " + act.target.name);
				Debug.Log("remove highlight2");
				GameObjectManager.removeComponent(act.target.GetComponent<HighLight>());
			}*/
			return true;
		}
		//Case For
		else if(act.actionType == Action.ActionType.For){
			if(incrementAction(act.actions[act.currentAction])){
				//if not end of for
				if(act.currentFor < act.nbFor){
					//new loop display
					act.actions[act.currentAction].target.transform.parent.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text =
					(act.currentFor +1).ToString() + " / " + act.nbFor.ToString();
				}
				//remove highlight of last action in previous loop
				if(act.currentFor > 0 && act.currentFor <= act.nbFor && act.actions.Count > 1 &&
				 act.actions[act.actions.Count-1].target != null && act.actions[act.actions.Count-1].target.GetComponent<HighLight>() != null){
					GameObjectManager.removeComponent<HighLight>(act.actions[act.actions.Count-1].target);
				}
				//remove highlight of previous action in current loop				
				if(act.currentAction > 0 && act.actions[act.currentAction-1].target != null && act.actions[act.currentAction-1].target.GetComponent<HighLight>() != null){
					GameObjectManager.removeComponent<HighLight>(act.actions[act.currentAction-1].target);
				}
				//add highlight to current action in current loop
				if(act.actions[act.currentAction].target != null && act.actions[act.currentAction].target.GetComponent<HighLight>() == null){
					GameObjectManager.addComponent<HighLight>(act.actions[act.currentAction].target);
				}
				act.currentAction++;
			}
			//another loop
			if(act.currentAction >= act.actions.Count){
				act.currentAction = 0;
				act.currentFor++;
				//End of for
				if(act.currentFor >= act.nbFor){
					act.currentAction = 0;
					act.currentFor = 0;
					/*
					Debug.Log("end for -- ");
					if(act.actions.Count > 0 && act.actions[act.actions.Count-1].target != null && act.actions[act.actions.Count-1].target.GetComponent<HighLight>() != null){
						Debug.Log("target : " + act.actions[act.actions.Count-1].target.name);
						Debug.Log("remove highlight3 end for");
						GameObjectManager.removeComponent(act.actions[act.actions.Count-1].target.GetComponent<HighLight>());
					}*/
					return true;
				}
				
				/*
				Debug.Log("---------------end loop");
				Debug.Log(act.currentAction);
				Debug.Log(act.actions[act.currentAction-1].target);
				//Debug.Log(act.actions[act.currentAction-1].target.GetComponent<HighLight>());
				//remove highlight on last action of for
				/*
				if(act.currentAction > 0 && act.actions[act.currentAction-1].target != null){
					Debug.Log("-remove highlight22");
					GameObjectManager.removeComponent<HighLight>(act.actions[act.currentAction-1].target);
				}/*
				if(act.actions[act.actions.Count-1].target != null && act.actions[act.actions.Count-1].target.GetComponent<HighLight>() != null){
					Debug.Log("TEST");
					GameObjectManager.removeComponent<HighLight>(act.actions[act.actions.Count-1].target);
				}*/
			}
		}
		else if(act.actionType == Action.ActionType.If){
			if(incrementAction(act.actions[act.currentAction]))
				act.currentAction++;
			
			if(act.currentAction >= act.actions.Count){
				act.currentAction = 0;
				if(act.actions[act.currentAction].target != null && act.actions[act.currentAction].target.GetComponent<HighLight>() != null){
					Debug.Log("target : " + act.actions[act.currentAction].target.name);
					Debug.Log("remove highlight4 end if");
					GameObjectManager.removeComponent(act.actions[act.currentAction].target.GetComponent<HighLight>());
				}
					
				return true;
			}
		}
		/*
		if(act.actions[act.currentAction].target != null && act.actions[act.currentAction].target.GetComponent<HighLight>() == null){
			Debug.Log("highlight target : " + act.actions[act.currentAction].target.name);
			Debug.Log("add highlight2");
			GameObjectManager.addComponent<HighLight>(act.actions[act.currentAction].target);
		}*/
		return false;
	}

	//Return the lenght of the script
	public static int getNbStep(Script script){
		int nb = 0;
		foreach(Action act in script.actions){
			nb += getNbStep(act);
		}

		return nb;
	}

	public static int getNbStep(Action action, bool ignoreIf = false){
		if(action.actionType == Action.ActionType.For){
			int nb = 0;
			foreach(Action act in action.actions){
				nb += getNbStep(act) * action.nbFor;
			}
			return nb;
		}
		else if(action.actionType == Action.ActionType.If && !ignoreIf){
			return 0;
		}
		else if(action.actionType == Action.ActionType.If && ignoreIf){
			int nb = 0;
			foreach(Action act in action.actions){
				nb += getNbStep(act);
			}
			return nb;
		}
		else
			return 1;
	}

	//0 Forward, 1 Backward, 2 Left, 3 Right
	public static Direction.Dir getDirection(Direction.Dir dirEntity, int relativeDir){
		if(relativeDir == 0)
			return dirEntity;
		switch(dirEntity){
			case Direction.Dir.North:
				switch(relativeDir){
					case 1:
						return Direction.Dir.South;
					case 2:
						return Direction.Dir.West;
					case 3:
						return Direction.Dir.East;
				}
				break;
			case Direction.Dir.West:
				switch(relativeDir){
					case 1:
						return Direction.Dir.East;
					case 2:
						return Direction.Dir.South;
					case 3:
						return Direction.Dir.North;
				}
				break;
			case Direction.Dir.East:
				switch(relativeDir){
					case 1:
						return Direction.Dir.West;
					case 2:
						return Direction.Dir.North;
					case 3:
						return Direction.Dir.South;
				}
				break;
			case Direction.Dir.South:
				switch(relativeDir){
					case 1:
						return Direction.Dir.North;
					case 2:
						return Direction.Dir.East;
					case 3:
						return Direction.Dir.West;
				}
				break;
		}
		return dirEntity;
	}

	public static void invalidAllIf(Script script){
		foreach(Action act in script.actions){
			if(act.actionType == Action.ActionType.If)
				act.ifValid = false;
			invalidAllIf(act);
		}
	}
	public static void invalidAllIf(Action action){
		if(action.actions != null){
			foreach(Action act in action.actions){
				if(act.actionType == Action.ActionType.If)
					act.ifValid = false;
				invalidAllIf(act);
			}
		}
	}
}


