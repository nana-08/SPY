using UnityEngine;

public class BasicAction : BaseElement {
	// Advice: FYFY component aims to contain only public members (according to Entity-Component-System paradigm).
    public enum ActionType { Forward, TurnLeft, TurnRight, Wait, Activate, PickBatteries, DropBatteries, TurnBack };
    public ActionType actionType;
}