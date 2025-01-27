﻿using UnityEngine;
using FYFY;
using System;
using TMPro;
using TinCan;

/// <summary>
/// This system executes new currentActions
/// </summary>
public class CurrentActionExecutor : FSystem {
	private Family f_wall = FamilyManager.getFamily(new AllOfComponents(typeof(Position)), new AnyOfTags("Wall", "Door"), new AnyOfProperties(PropertyMatcher.PROPERTY.ACTIVE_IN_HIERARCHY));
	private Family f_activable = FamilyManager.getFamily(new AllOfComponents(typeof(Activable),typeof(Position),typeof(AudioSource)));  // console
    private Family f_activableSwitch = FamilyManager.getFamily(new AllOfComponents(typeof(ActivableSwitch), typeof(Position), typeof(AudioSource)));    // switch
    private Family f_batteries = FamilyManager.getFamily(new AllOfComponents(typeof(PileOfBatteries),typeof(Position),typeof(AudioSource)));
    private Family f_newCurrentAction = FamilyManager.getFamily(new AllOfComponents(typeof(CurrentAction), typeof(BasicAction)));
	private Family f_agent = FamilyManager.getFamily(new AllOfComponents(typeof(ScriptRef), typeof(Position)));

	public Material switchOKMaterial;
    //public Material switchNOKMaterial;
	//public AudioClip switchScanAudioClip;
	public AudioClip switchNOKAudioClip;
	public AudioClip switchOKAudioClip;
	public AudioClip pickUpBatteryAudioClip;
	public AudioClip dropBatteryAudioClip;
	public GameObject playerCurrentWeight;

    protected override void onStart()
	{
		f_newCurrentAction.addEntryCallback(onNewCurrentAction);
		Pause = true;
	}

	protected override void onProcess(int familiesUpdateCount)
	{
		foreach (GameObject agent in f_agent)
		{
			// count inaction if a robot have no CurrentAction
			if (agent.tag == "Player" && agent.GetComponent<ScriptRef>().executableScript.GetComponentInChildren<CurrentAction>(true) == null)
				agent.GetComponent<ScriptRef>().nbOfInactions++;
			// Cancel move if target position is used by another agent
			bool conflict = true;
			while (conflict)
			{
				conflict = false;
				foreach (GameObject agent2 in f_agent)
					if (agent != agent2 && agent.tag == agent2.tag && agent.tag == "Player")
					{
						Position r1Pos = agent.GetComponent<Position>();
						Position r2Pos = agent2.GetComponent<Position>();
						// check if the two robots move on the same position => forbiden
						if (r2Pos.targetX != -1 && r2Pos.targetY != -1 && r1Pos.targetX == r2Pos.targetX && r1Pos.targetY == r2Pos.targetY)
						{
							r2Pos.targetX = -1;
							r2Pos.targetY = -1;
							conflict = true;
							GameObjectManager.addComponent<ForceMoveAnimation>(agent2);
						}
						// one robot doesn't move and the other try to move on its position => forbiden
						else if (r2Pos.targetX == -1 && r2Pos.targetY == -1 && r1Pos.targetX == r2Pos.x && r1Pos.targetY == r2Pos.y)
						{
							r1Pos.targetX = -1;
							r1Pos.targetY = -1;
							conflict = true;
							GameObjectManager.addComponent<ForceMoveAnimation>(agent);
						}
						// the two robot want to exchange their position => forbiden
						else if (r1Pos.targetX == r2Pos.x && r1Pos.targetY == r2Pos.y && r1Pos.x == r2Pos.targetX && r1Pos.y == r2Pos.targetY)
                        {
							r1Pos.targetX = -1;
							r1Pos.targetY = -1;
							r2Pos.targetX = -1;
							r2Pos.targetY = -1;
							conflict = true;
							GameObjectManager.addComponent<ForceMoveAnimation>(agent);
							GameObjectManager.addComponent<ForceMoveAnimation>(agent2);
						}

					}
			}
		}

		// Record valid movements
		foreach (GameObject robot in f_agent)
		{
			Position pos = robot.GetComponent<Position>();
			if (pos.targetX != -1 && pos.targetY != -1)
			{
				pos.x = pos.targetX;
				pos.y = pos.targetY;
				pos.targetX = -1;
				pos.targetY = -1;
			}
		}
		Pause = true;
	}

	// each time a new currentAction is added, 
	private void onNewCurrentAction(GameObject currentAction) {
		Pause = false; // activates onProcess to identify inactive robots
		
		CurrentAction ca = currentAction.GetComponent<CurrentAction>();

        Position agentPos = ca.agent.GetComponent<Position>();
        // process action depending on action type
        switch (currentAction.GetComponent<BasicAction>().actionType){
			case BasicAction.ActionType.Forward:
				ApplyForward(ca.agent);
				break;
			case BasicAction.ActionType.TurnLeft:
				ApplyTurnLeft(ca.agent);
				break;
			case BasicAction.ActionType.TurnRight:
				ApplyTurnRight(ca.agent);
				break;
			case BasicAction.ActionType.TurnBack:
				ApplyTurnBack(ca.agent);
				break;
			case BasicAction.ActionType.Wait:
                break;
			case BasicAction.ActionType.Activate:
				foreach ( GameObject actGo in f_activable){
					if(actGo.GetComponent<Position>().x == agentPos.x && actGo.GetComponent<Position>().y == agentPos.y){
						actGo.GetComponent<AudioSource>().Play();
						// toggle activable GameObject
						if (actGo.GetComponent<TurnedOn>())
							GameObjectManager.removeComponent<TurnedOn>(actGo);
						else
							GameObjectManager.addComponent<TurnedOn>(actGo);
					}
				}
				ca.agent.GetComponent<Animator>().SetTrigger("Action");
				break;
			case BasicAction.ActionType.PickBatteries:
                foreach (GameObject batteryGo in f_batteries)
                {
                    if (batteryGo.GetComponent<Position>().x == agentPos.x && batteryGo.GetComponent<Position>().y == agentPos.y)
                    {
                        batteryGo.GetComponent<AudioSource>().PlayOneShot(pickUpBatteryAudioClip);

                        int pickParameterValue = int.Parse(currentAction.GetComponentInChildren<TMP_InputField>().text);
                        playerCurrentWeight.GetComponent<TMP_Text>().text = (int.Parse(playerCurrentWeight.GetComponent<TMP_Text>().text) + pickParameterValue).ToString();

                        // ca.agent.GetComponent<Animator>().SetTrigger("PickBatteries");	// animation not really working for now
                    }
                }
                break;
            case BasicAction.ActionType.DropBatteries:
				int agentWeight = ca.agent.GetComponent<Weight>().weight;
                int playerCurrentWeightValue = int.Parse(playerCurrentWeight.GetComponent<TMP_Text>().text);
                int dropParameterValue = int.Parse(currentAction.GetComponentInChildren<TMP_InputField>().text);

                if (playerCurrentWeightValue - dropParameterValue >= agentWeight)
				{
                    foreach (GameObject batteryGo in f_batteries)
                    {
                        batteryGo.GetComponent<AudioSource>().PlayOneShot(dropBatteryAudioClip);
                        break;
                    }

                    playerCurrentWeight.GetComponent<TMP_Text>().text = (playerCurrentWeightValue - dropParameterValue).ToString();
                } 
                else
                    Debug.Log("Impossible to drop that much batteries!");
                // ca.agent.GetComponent<Animator>().SetTrigger("DropBatteries");	// animation not really working for now
                break;
			case BasicAction.ActionType.ActivateSwitch:
                foreach (GameObject switchGo in f_activableSwitch)
                {
                    if (switchGo.GetComponent<Position>().x == agentPos.x && switchGo.GetComponent<Position>().y == agentPos.y)
                    {
						//switchGo.GetComponent<AudioSource>().PlayOneShot(switchScanAudioClip);
						// check player's weight
                        if (int.Parse(playerCurrentWeight.GetComponent<TMP_Text>().text) == switchGo.GetComponent<ActivableSwitch>().weight)
						{
							switchGo.GetComponent<AudioSource>().PlayOneShot(switchOKAudioClip);
                            switchGo.GetComponentInChildren<MeshRenderer>().material = switchOKMaterial;
                            if (switchGo.GetComponent<TurnedOn>())
							{
                                GameObjectManager.removeComponent<TurnedOn>(switchGo);
							}
							else
								GameObjectManager.addComponent<TurnedOn>(switchGo);
                        }
						else
						{
                            switchGo.GetComponent<AudioSource>().PlayOneShot(switchNOKAudioClip);
                        }
					}
                }
				break;
        }
		ca.StopAllCoroutines();
		if (ca.gameObject.activeInHierarchy)
			ca.StartCoroutine(Utility.pulseItem(ca.gameObject));
		// notify agent moving
		if (ca.agent.CompareTag("Drone") && !ca.agent.GetComponent<Moved>())
			GameObjectManager.addComponent<Moved>(ca.agent);
	}

	private void ApplyForward(GameObject go){
		Position pos = go.GetComponent<Position>();
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				if (!checkObstacle(pos.x, pos.y - 1))
				{
					pos.targetX = pos.x;
					pos.targetY = pos.y - 1;
				}
				else
					GameObjectManager.addComponent<ForceMoveAnimation>(go);
				break;
			case Direction.Dir.South:
				if(!checkObstacle(pos.x,pos.y + 1)){
					pos.targetX = pos.x;
					pos.targetY = pos.y + 1;
				}
				else
					GameObjectManager.addComponent<ForceMoveAnimation>(go);
				break;
			case Direction.Dir.East:
				if(!checkObstacle(pos.x + 1, pos.y)){
					pos.targetX = pos.x + 1;
					pos.targetY = pos.y;
				}
				else
					GameObjectManager.addComponent<ForceMoveAnimation>(go);
				break;
			case Direction.Dir.West:
				if(!checkObstacle(pos.x - 1, pos.y)){
					pos.targetX = pos.x - 1;
					pos.targetY = pos.y;
				}
				else
					GameObjectManager.addComponent<ForceMoveAnimation>(go);
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
		foreach( GameObject go in f_wall){
			if(go.GetComponent<Position>().x == x && go.GetComponent<Position>().y == z)
				return true;
		}
		return false;
	}

	/*private bool checkFacingBatteries(GameObject robot)
	{
        foreach (GameObject go in f_batteries)
        {
            if (go.GetComponent<Position>().x == robot.GetComponent<Position>().x - 1 && go.GetComponent<Position>().y == robot.GetComponent<Position>().y && robot)
                return true;
        }
        return false;
    }*/
}
