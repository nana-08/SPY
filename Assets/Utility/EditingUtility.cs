using FYFY;
using FYFY_plugins.PointerManager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public static class EditingUtility
{

	[Serializable]
	public class RawParams
	{
		public string attribute;
		public string constraint;
		public string value;
		public string tag2;
		public string attribute2;
	}

	[Serializable]
	public class RawConstraint
	{
		public string label;
		public string tag;
		public RawParams[] parameters;
	}
	// Add an item on a drop area
	// return true if the item was added and false otherwise
	public static bool addItemOnDropArea(GameObject item, GameObject dropArea)
	{
		if (dropArea.GetComponent<DropZone>())
		{
			// if item is not a BaseElement (BasicAction or ControlElement) cancel action, undo drop
			if (!item.GetComponent<BaseElement>())
			{
				return false;
			}

			// the item is compatible with dropZone
			Transform targetContainer = null;
			int siblingIndex = 0;
			if (dropArea.transform.parent.GetComponent<BaseElement>()) // BasicAction
			{
				targetContainer = dropArea.transform.parent.parent; // target is the grandparent
				siblingIndex = dropArea.transform.parent.GetSiblingIndex();
			}
			else if (dropArea.transform.parent.parent.GetComponent<ControlElement>() && dropArea.transform.parent.GetSiblingIndex() == 1) // the dropArea of the first child of a Control block
			{
				targetContainer = dropArea.transform.parent.parent.parent; // target is the grandgrandparent
				siblingIndex = dropArea.transform.parent.parent.GetSiblingIndex();
			}
			else
			{
				Debug.LogError("Warning! Unknown case: the drop zone is not in the correct context");
				return false;
			}
			// if binded set this drop area as default
			if (GameObjectManager.isBound(dropArea) && !dropArea.GetComponent<Selected>())
				GameObjectManager.addComponent<Selected>(dropArea);
			// On associe l'element au container
			item.transform.SetParent(targetContainer);
			// On met l'�l�ment � la position voulue
			item.transform.SetSiblingIndex(siblingIndex);
		}
		else if (dropArea.GetComponent<ReplacementSlot>()) // we replace the replacementSlot by the item
		{
			ReplacementSlot repSlot = dropArea.GetComponent<ReplacementSlot>();
			// if replacement slot is for base element => insert item just before replacement slot
			if (repSlot.slotType == ReplacementSlot.SlotType.BaseElement)
			{
				// On associe l'element au container
				item.transform.SetParent(repSlot.transform.parent);
				// On met l'�l�ment � la position voulue
				item.transform.SetSiblingIndex(repSlot.transform.GetSiblingIndex()); 
				// disable empty slot
				repSlot.GetComponent<Outline>().enabled = false;

				// if binded set this empty slot as default
				if (GameObjectManager.isBound(repSlot.gameObject) && !repSlot.GetComponent<Selected>())
					GameObjectManager.addComponent<Selected>(repSlot.gameObject);
			}
			// if replacement slot is for base condition => two case fill an empty zone or replace existing condition
			else if (repSlot.slotType == ReplacementSlot.SlotType.BaseCondition)
			{
				// On associe l'element au container
				item.transform.SetParent(repSlot.transform.parent);
				// On met l'�l�ment � la position voulue
				item.transform.SetSiblingIndex(repSlot.transform.GetSiblingIndex());
				// check if the replacement slot is an empty zone (doesn't contain a condition)
				if (!repSlot.GetComponent<BaseCondition>())
				{
					// disable empty slot
					repSlot.GetComponent<Outline>().enabled = false;

					// Because this function can be call for binded GO or not
					if (GameObjectManager.isBound(repSlot.gameObject)) GameObjectManager.setGameObjectState(repSlot.transform.gameObject, false);
					else repSlot.transform.gameObject.SetActive(false);
				}
				else
				{
					// ResetBlocLimit will restore library and remove dropArea and children
					GameObjectManager.addComponent<ResetBlocLimit>(repSlot.gameObject);
				}
			}
		}
		else
		{
			Debug.LogError("Warning! Unknown case: the drop area is not a drop zone or a replacement zone");
			return false;
		}
		// We secure the scale
		item.transform.localScale = new Vector3(1, 1, 1);

		return true;
	}

	// We create an editable block from a library item (without binded it to FYFY, depending on context the object has to be binded or not)
	public static GameObject createEditableBlockFromLibrary(GameObject element, GameObject targetCanvas)
	{
		// On r�cup�re le prefab associ� � l'action de la librairie
		GameObject prefab = element.GetComponent<ElementToDrag>().actionPrefab;
		// Create a dragged GameObject
		GameObject newItem = UnityEngine.Object.Instantiate<GameObject>(prefab, element.transform);
		// Ajout d'un TooltipContent identique � celui de l'inventaire
		if (element.GetComponent<TooltipContent>()) {
			TooltipContent tooltip = newItem.AddComponent<TooltipContent>();
			tooltip.text = element.GetComponent<TooltipContent>().text;
		}
		// On l'attache au canvas pour le drag ou l'on veux
		newItem.transform.SetParent(targetCanvas.transform);
		// link with library
		if (newItem.GetComponent<LibraryItemRef>())
			newItem.GetComponent<LibraryItemRef>().linkedTo = element;
		return newItem;
	}

	// elementToDelete will be deleted then manage empty zone accordingly
	public static void manageEmptyZone(GameObject elementToDelete)
	{
		if (elementToDelete.GetComponent<BaseCondition>())
			// enable the next last child of the container
			GameObjectManager.setGameObjectState(elementToDelete.transform.parent.GetChild(elementToDelete.transform.GetSiblingIndex() + 1).gameObject, true);
	}

	// Copy an editable script to the container of an agent
	public static void fillExecutablePanel(GameObject srcScript, GameObject targetContainer, string agentTag)
	{
		// On va copier la sequence cr�� par le joueur dans le container de la fen�tre du robot
		// On commence par cr�er une copie du container ou se trouve la sequence
		GameObject containerCopy = CopyActionsFromAndInitFirstChild(srcScript, false, agentTag);
		// On copie les actions dedans 
		for (int i = 0; i < containerCopy.transform.childCount; i++)
		{
			// On ne conserve que les BaseElement et on les nettoie
			if (containerCopy.transform.GetChild(i).GetComponent<BaseElement>())
			{
				Transform child = UnityEngine.GameObject.Instantiate(containerCopy.transform.GetChild(i));

				// remove drop zones
				foreach (DropZone dropZone in child.GetComponentsInChildren<DropZone>(true))
                {
					if (GameObjectManager.isBound(dropZone.gameObject))
						GameObjectManager.unbind(dropZone.gameObject);
					dropZone.transform.SetParent(null);
					GameObject.Destroy(dropZone.gameObject);
				}
				//remove empty zones for BaseElements
				foreach (ReplacementSlot emptyZone in child.GetComponentsInChildren<ReplacementSlot>(true))
				{
					if (emptyZone.slotType == ReplacementSlot.SlotType.BaseElement) {
						if (GameObjectManager.isBound(emptyZone.gameObject))
							GameObjectManager.unbind(emptyZone.gameObject);
						emptyZone.transform.SetParent(null);
						GameObject.Destroy(emptyZone.gameObject);
					}
				}
				child.SetParent(targetContainer.transform, false);
			}
		}
		// Va linker les blocs ensemble
		// C'est � dire qu'il va d�finir pour chaque bloc, qu'elle est le suivant � ex�cuter
		computeNext(targetContainer);
		// On d�truit la copy de la sequence d'action
		UnityEngine.Object.Destroy(containerCopy);
	}

	/**
	 * On copie le container qui contient la sequence d'actions et on initialise les firstChild
	 * Param:
	 *	Container (GameObject) : Le container qui contient le script � copier
	 *	isInteractable (bool) : Si le script copi� peut contenir des �l�ments interactable (sinon l'interaction sera desactiv�)
	 *	agent (GameObject) : L'agent sur qui l'on va copier la sequence (pour d�finir la couleur)
	 * 
	 **/
	private static GameObject CopyActionsFromAndInitFirstChild(GameObject container, bool isInteractable, string agentTag)
	{
		// On va travailler avec une copy du container
		GameObject copyGO = GameObject.Instantiate(container);
		//Pour tous les �l�ment interactible, on va les d�sactiver/activer selon le param�trage
		foreach (TMP_Dropdown drop in copyGO.GetComponentsInChildren<TMP_Dropdown>(true))
		{
			drop.interactable = isInteractable;
		}
		foreach (TMP_InputField input in copyGO.GetComponentsInChildren<TMP_InputField>(true))
		{
			input.interactable = isInteractable;
		}

		// Pour chaque bloc for
		foreach (ForControl forAct in copyGO.GetComponentsInChildren<ForControl>(true))
		{
			// Si activ�, on note le nombre de tour de boucle � faire
			if (!isInteractable && !forAct.gameObject.GetComponent<WhileControl>())
			{
				try
				{
					forAct.nbFor = int.Parse(forAct.transform.GetChild(1).GetChild(1).GetComponent<TMP_InputField>().text);
				} catch{
					forAct.nbFor = 0;
				}
				forAct.transform.GetChild(1).GetChild(1).GetComponent<TMP_InputField>().text = (forAct.currentFor).ToString() + " / " + forAct.nbFor.ToString();
			}// Sinon on met tout � 0
			else if (isInteractable && !forAct.gameObject.GetComponent<WhileControl>())
			{
				forAct.currentFor = 0;
				forAct.transform.GetChild(1).GetChild(1).GetComponent<TMP_InputField>().text = forAct.nbFor.ToString();
			}
			else if (forAct is WhileControl)
			{
				// On traduit la condition en string
				((WhileControl)forAct).condition = new List<string>();
				conditionToStrings(forAct.gameObject.transform.Find("ConditionContainer").GetChild(0).gameObject, ((WhileControl)forAct).condition);

			}
			// On parcourt les �l�ments pr�sent dans le block action
			foreach (BaseElement act in forAct.GetComponentsInChildren<BaseElement>(true))
			{
				// Si ce n'est pas un bloc action alors on le note comme premier �l�ment puis on arr�te le parcourt des �l�ments
				if (!act.Equals(forAct))
				{
					forAct.firstChild = act.gameObject;
					break;
				}
			}
		}
		// Pour chaque block de boucle infini
		foreach (ForeverControl loopAct in copyGO.GetComponentsInChildren<ForeverControl>(true))
		{
			foreach (BaseElement act in loopAct.GetComponentsInChildren<BaseElement>(true))
			{
				if (!act.Equals(loopAct))
				{
					loopAct.firstChild = act.gameObject;
					break;
				}
			}
		}
		// Pour chaque block if
		foreach (IfControl ifAct in copyGO.GetComponentsInChildren<IfControl>(true))
		{
			// On traduit la condition en string
			ifAct.condition = new List<string>();
			conditionToStrings(ifAct.gameObject.transform.Find("ConditionContainer").GetChild(0).gameObject, ifAct.condition);

			GameObject thenContainer = ifAct.transform.Find("Container").gameObject;
			BaseElement firstThen = thenContainer.GetComponentInChildren<BaseElement>(true);
			if (firstThen)
				ifAct.firstChild = firstThen.gameObject;
			//Si c'est un elseAction
			if (ifAct is IfElseControl)
			{
				GameObject elseContainer = ifAct.transform.Find("ElseContainer").gameObject;
				BaseElement firstElse = elseContainer.GetComponentInChildren<BaseElement>(true);
				if (firstElse)
					((IfElseControl)ifAct).elseFirstChild = firstElse.gameObject;
			}
		}

		foreach (EventTrigger eventTrigger in copyGO.GetComponentsInChildren<EventTrigger>(true))
			Component.Destroy(eventTrigger);

		// On d�fini la couleur de l'action selon l'agent � qui appartiendra le script
		if (agentTag == "Drone") {
			foreach (BaseElement act in copyGO.GetComponentsInChildren<BaseElement>(true))
			{
				Selectable sel = act.GetComponent<Selectable>();
				sel.interactable = false;
				Color disabledColor = sel.colors.disabledColor;

				if (act.GetComponent<ControlElement>())
					foreach (Transform child in act.gameObject.transform)
					{
						Image childImg = child.GetComponent<Image>();
						if (child.name != "3DEffect" && childImg != null)
							childImg.color = disabledColor;
					}
			}
			foreach (BaseCondition act in copyGO.GetComponentsInChildren<BaseCondition>(true))
			{
				Selectable sel = act.GetComponent<Selectable>();
				sel.interactable = false;
				Color disabledColor = sel.colors.disabledColor;
				if (act.GetComponent<BaseOperator>())
					foreach (Transform child in act.gameObject.transform)
					{
						Image childImg = child.GetComponent<Image>();
						if (child.name != "3DEffect" && childImg != null)
							childImg.color = disabledColor;
					}
			}
		}

		return copyGO;
	}

	/**
	 * Nettoie le bloc de controle (On supprime les end-zones, on met les conditions sous forme d'un seul bloc)
	 * Param:
	 *	specialBlock (GameObject) : Container qu'il faut nettoyer
	 * 
	 **/
	public static void CleanControlBlock(Transform specialBlock)
	{
		// V�rifier que c'est bien un block de controle
		if (specialBlock.GetComponent<ControlElement>())
		{
			// R�cup�rer le container des actions
			Transform container = specialBlock.transform.Find("Container");
			// remove the last child, the emptyZone
			GameObject emptySlot = container.GetChild(container.childCount - 1).gameObject;
			if (GameObjectManager.isBound(emptySlot))
				GameObjectManager.unbind(emptySlot);
			emptySlot.transform.SetParent(null);
			GameObject.Destroy(emptySlot);

			// Si c'est un block if on garde le container des actions (sans le emptyslot) mais la condition est traduite dans IfAction
			if (specialBlock.GetComponent<IfElseControl>())
			{
				// get else container
				Transform elseContainer = specialBlock.transform.Find("ElseContainer");
				// remove the last child, the emptyZone
				emptySlot = elseContainer.GetChild(elseContainer.childCount - 1).gameObject;
				if (GameObjectManager.isBound(emptySlot))
					GameObjectManager.unbind(emptySlot);
				emptySlot.transform.SetParent(null);
				GameObject.Destroy(emptySlot);
				// On parcourt les blocks qui composent le ElseContainer afin de les nettoyer �galement
				foreach (Transform block in elseContainer)
					// Si c'est le cas on fait un appel r�cursif
					if (block.GetComponent<ControlElement>())
						CleanControlBlock(block);
			}

			// On parcourt les blocks qui composent le container afin de les nettoyer �galement
			foreach (Transform block in container)
				// Si c'est le cas on fait un appel r�cursif
				if (block.GetComponent<ControlElement>())
					CleanControlBlock(block);
		}
	}

	// Transforme une sequence de condition en une chaine de caract�re
	private static void conditionToStrings(GameObject condition, List<string> chaine)
	{
		// Check if condition is a BaseCondition
		if (condition.GetComponent<BaseCondition>())
		{
			// On regarde si la condition re�ue est un �l�ment ou bien un op�rator
			// Si c'est un �l�ment, on le traduit en string et on le renvoie 
			if (condition.GetComponent<BaseCaptor>())
				chaine.Add("" + condition.GetComponent<BaseCaptor>().captorType);
			else
			{
				BaseOperator bo;
				if (condition.TryGetComponent<BaseOperator>(out bo))
				{
					Transform conditionContainer = bo.transform.GetChild(1);
					// Si c'est une n�gation on met "!" puis on fait une r�cursive sur le container et on renvoie le tous traduit en string
					if (bo.operatorType == BaseOperator.OperatorType.NotOperator)
					{
						// On v�rifie qu'il y a bien un �l�ment pr�sent, son container doit contenir 3 enfants (icone, une BaseCondition et le ReplacementSlot)
						if (conditionContainer.childCount == 3)
						{
							chaine.Add("NOT");
							conditionToStrings(conditionContainer.GetComponentInChildren<BaseCondition>(true).gameObject, chaine);
						}
						else
						{
							GameObjectManager.addComponent<NewEnd>(MainLoop.instance.gameObject, new { endType = NewEnd.BadCondition });
						}
					}
					else if (bo.operatorType == BaseOperator.OperatorType.AndOperator)
					{
						// Si les c�t�s de l'op�rateur sont remplis, alors il compte 5 childs (2 ReplacementSlots, 2 BaseCondition et 1 icone), sinon cela veux dire que il manque des conditions
						if (conditionContainer.childCount == 5)
						{
							chaine.Add("(");
							conditionToStrings(conditionContainer.GetChild(0).gameObject, chaine);
							chaine.Add("AND");
							conditionToStrings(conditionContainer.GetChild(3).gameObject, chaine);
							chaine.Add(")");
						}
						else
						{
							GameObjectManager.addComponent<NewEnd>(MainLoop.instance.gameObject, new { endType = NewEnd.BadCondition });
						}
					}
					else if (bo.operatorType == BaseOperator.OperatorType.OrOperator)
					{
						// Si les c�t�s de l'op�rateur sont remplis, alors il compte 5 childs, sinon cela veux dire que il manque des conditions
						if (conditionContainer.childCount == 5)
						{
							chaine.Add("(");
							conditionToStrings(conditionContainer.GetChild(0).gameObject, chaine);
							chaine.Add("OR");
							conditionToStrings(conditionContainer.GetChild(3).gameObject, chaine);
							chaine.Add(")");
						}
						else
						{
							GameObjectManager.addComponent<NewEnd>(MainLoop.instance.gameObject, new { endType = NewEnd.BadCondition });
						}
					}
				}
				else
				{
					Debug.LogError("Unknown BaseCondition!!!");
				}
			}
		}
		else
			GameObjectManager.addComponent<NewEnd>(MainLoop.instance.gameObject, new { endType = NewEnd.BadCondition });
	}
	
	// link actions together => define next property
	// Associe � chaque bloc le bloc qui sera execut� apr�s
	public static void computeNext(GameObject container)
	{
		// parcourir tous les enfants jusqu'� l'avant dernier
		for (int i = 0; i < container.transform.childCount - 1; i++)
		{
			Transform child = container.transform.GetChild(i);
			child.GetComponent<BaseElement>().next = container.transform.GetChild(i + 1).gameObject;
		}
		// traitement de la derni�re instruction
		if (container.transform.childCount > 0)
		{
			Transform lastChild = container.transform.GetChild(container.transform.childCount - 1);
			// Pour la derni�re instruction le next d�pend du parent
			Transform parent = container.transform.parent;
			if (parent != null && parent.GetComponent<BaseElement>() != null) {
				if (parent.GetComponent<ForControl>() != null || parent.GetComponent<ForeverControl>() != null)
					lastChild.GetComponent<BaseElement>().next = parent.gameObject;
				else
					lastChild.GetComponent<BaseElement>().next = parent.GetComponent<BaseElement>().next;
			}
			// Sinon on ne fait rien et fin de la sequence
		}

		// parcourir tous les enfants jusqu'au dernier cette fois ci pour d�clencher des appel r�cursifs pour les structures de contr�le
		for (int i = 0; i < container.transform.childCount; i++)
		{
			Transform child = container.transform.GetChild(i);
			// Si le fils est un contr�le, appel r�sursif sur leurs containers
			if (child.GetComponent<ControlElement>())
			{
				computeNext(child.transform.Find("Container").gameObject);
				// Si c'est un else il ne faut pas oublier le container else
				if (child.GetComponent<IfElseControl>())
					computeNext(child.transform.Find("ElseContainer").gameObject);
			}
		}
	}

	public static void removeComments(XmlNode node)
	{
		for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
		{
			XmlNode child = node.ChildNodes[i];
			if (child.NodeType == XmlNodeType.Comment)
				node.RemoveChild(child);
			else
				removeComments(child);
		}
	}

	public static string exportBlockToString(Highlightable script, GameObject focusedArea, bool highlightCurrentAction = false)
	{
		if (script == null)
			return "";
		else
		{
			string export = "";
			if (script is BasicAction)
			{
				DropZone dz = script.GetComponentInChildren<DropZone>(true);
				if (dz != null && dz.gameObject == focusedArea)
					export += "#### ";
				if (script.GetComponent<CurrentAction>())
					export += "* ";
				export += (script as BasicAction).actionType.ToString() + ";";
			}
			else if (script is BaseCaptor)
			{
				ReplacementSlot localRS = script.GetComponent<ReplacementSlot>();
				if (localRS.gameObject == focusedArea)
					export += "##";
				export += (script as BaseCaptor).captorType.ToString();
				if (localRS.gameObject == focusedArea)
					export += "##";
			}
			else if (script is BaseOperator)
			{
				ReplacementSlot localRS = script.GetComponent<ReplacementSlot>();
				if (localRS.gameObject == focusedArea)
					export += "##";
				BaseOperator ope = script as BaseOperator;
				Transform container = script.transform.Find("Container");
				if (ope.operatorType == BaseOperator.OperatorType.NotOperator)
				{
					export += "NOT (";

					if (container.Find("EmptyConditionalSlot").GetComponent<ReplacementSlot>().gameObject == focusedArea)
						export += "####";
					else
						export += exportBlockToString(container.GetComponentInChildren<BaseCondition>(true), focusedArea);

					export += ")";
				}
				else if (ope.operatorType == BaseOperator.OperatorType.AndOperator)
				{
					export += "(";

					if (container.Find("EmptyConditionalSlot1").GetComponent<ReplacementSlot>().gameObject == focusedArea)
						export += "####";
					else
						export += exportBlockToString(container.GetChild(0).GetComponentInChildren<BaseCondition>(true), focusedArea);

					export += ") AND (";

					if (container.Find("EmptyConditionalSlot2").GetComponent<ReplacementSlot>().gameObject == focusedArea)
						export += "####";
					else
						export += exportBlockToString(container.GetChild(container.childCount - 2).GetComponentInChildren<BaseCondition>(true), focusedArea);

					export += ")";
				}
				else if (ope.operatorType == BaseOperator.OperatorType.OrOperator)
				{
					export += "(";

					if (container.Find("EmptyConditionalSlot1").GetComponent<ReplacementSlot>().gameObject == focusedArea)
						export += "####";
					else
						export += exportBlockToString(container.GetChild(0).GetComponentInChildren<BaseCondition>(true), focusedArea);

					export += ") OR (";

					if (container.Find("EmptyConditionalSlot2").GetComponent<ReplacementSlot>().gameObject == focusedArea)
						export += "####";
					else
						export += exportBlockToString(container.GetChild(container.childCount - 2).GetComponentInChildren<BaseCondition>(true), focusedArea);

					export += ")";
				}
				if (localRS.gameObject == focusedArea)
					export += "##";
			}
			else if (script is ControlElement)
			{
				DropZone dz = script.transform.Find("Header").GetComponentInChildren<DropZone>(true);
				if (dz != null && dz.gameObject == focusedArea)
					export += "#### ";
				ControlElement control = script as ControlElement;
				if (script is WhileControl)
				{
					export += "WHILE (";

					if (script.transform.Find("ConditionContainer").Find("EmptyConditionalSlot").GetComponent<ReplacementSlot>().gameObject == focusedArea)
						export += "####";
					else
						export += exportBlockToString(script.transform.Find("ConditionContainer").GetComponentInChildren<BaseCondition>(true), focusedArea);

					export += ")";
				}
				else if (script is ForControl)
				{
					export += "REPEAT (";
					if (script.gameObject == focusedArea)
						export += "##";
					export += (script as ForControl).nbFor;
					if (script.gameObject == focusedArea)
						export += "##";
					export += ")";
				}
				else if (script is ForeverControl)
					export += "FOREVER";
				else if (script is IfControl)
				{
					export += "IF (";

					if (script.transform.Find("ConditionContainer").Find("EmptyConditionalSlot").GetComponent<ReplacementSlot>().gameObject == focusedArea)
						export += "####";
					else
						export += exportBlockToString(script.transform.Find("ConditionContainer").GetComponentInChildren<BaseCondition>(true), focusedArea);

					export += ")";
				}

				export += " {";

				Transform container = script.transform.Find("Container");
				// parcourir tous les enfants et exclure les zone de drop
				for (int i = 0; i < container.childCount; i++)
					if (container.GetChild(i).GetComponent<ReplacementSlot>() == null)
						export += " " + exportBlockToString(container.GetChild(i).GetComponent<BaseElement>(), focusedArea);
				if (container.GetChild(container.childCount - 1).gameObject == focusedArea)
					export += " ####";

				export += " }";

				if (script is IfElseControl)
				{
					export += " ELSE {";

					Transform containerElse = script.transform.Find("ElseContainer");
					// parcourir tous les enfants et exclure les zone de drop
					for (int i = 0; i < containerElse.childCount; i++)
						if (containerElse.GetChild(i).GetComponent<ReplacementSlot>() == null)
							export += " " + exportBlockToString(containerElse.GetChild(i).GetComponent<BaseElement>(), focusedArea);
					if (containerElse.GetChild(containerElse.childCount - 1).gameObject == focusedArea)
						export += " ####";

					export += " }";
				}
			}
			return export;
		}
	}

	public static void readXMLDialogs(XmlNode dialogs, List<Dialog> target)
	{
		foreach (XmlNode dialogXML in dialogs.ChildNodes)
		{
			Dialog dialog = new Dialog();
			if (dialogXML.Attributes.GetNamedItem("text") != null)
				dialog.text = dialogXML.Attributes.GetNamedItem("text").Value;
			if (dialogXML.Attributes.GetNamedItem("img") != null)
				dialog.img = dialogXML.Attributes.GetNamedItem("img").Value;
			if (dialogXML.Attributes.GetNamedItem("imgHeight") != null)
				dialog.imgHeight = float.Parse(dialogXML.Attributes.GetNamedItem("imgHeight").Value);
			if (dialogXML.Attributes.GetNamedItem("camX") != null)
				dialog.camX = int.Parse(dialogXML.Attributes.GetNamedItem("camX").Value);
			if (dialogXML.Attributes.GetNamedItem("camY") != null)
				dialog.camY = int.Parse(dialogXML.Attributes.GetNamedItem("camY").Value);
			if (dialogXML.Attributes.GetNamedItem("sound") != null)
				dialog.sound = dialogXML.Attributes.GetNamedItem("sound").Value;
			if (dialogXML.Attributes.GetNamedItem("video") != null)
				dialog.video = dialogXML.Attributes.GetNamedItem("video").Value;
			if (dialogXML.Attributes.GetNamedItem("enableInteraction") != null)
				dialog.enableInteraction = int.Parse(dialogXML.Attributes.GetNamedItem("enableInteraction").Value) == 1;
			target.Add(dialog);
		}
	}

	public static IEnumerator pulseItem(GameObject newItem)
	{
		newItem.transform.localScale = new Vector3(1, 1, 1);
		float initScaleX = newItem.transform.localScale.x;
		newItem.transform.localScale = new Vector3(newItem.transform.localScale.x + 0.3f, newItem.transform.localScale.y, newItem.transform.localScale.z);
		while (newItem.transform.localScale.x > initScaleX)
		{
			newItem.transform.localScale = new Vector3(newItem.transform.localScale.x - 0.01f, newItem.transform.localScale.y, newItem.transform.localScale.z);
			yield return null;
		}
		newItem.transform.localScale = new Vector3(1, 1, 1);
	}

	public static bool isCompetencyMatchWithLevel(Competency competency, XmlDocument level)
	{
		// check all constraints of the competency
		Dictionary<string, List<XmlNode>> constraintsState = new Dictionary<string, List<XmlNode>>();
		foreach (RawConstraint constraint in competency.constraints)
		{

			if (constraintsState.ContainsKey(constraint.label))
			{
				// if a constraint with this label is defined and no XmlNode identified, useless to check this new one
				if (constraintsState[constraint.label].Count == 0)
					continue;
			}
			else
			{
				// init this constraint with all XmlNode of required tag
				List<XmlNode> tagList = new List<XmlNode>();
				foreach (XmlNode tag in level.GetElementsByTagName(constraint.tag))
					tagList.Add(tag);
				constraintsState.Add(constraint.label, tagList);
			}

			// check if this constraint is true
			List<XmlNode> tags = constraintsState[constraint.label];
			foreach (RawParams parameter in constraint.parameters)
			{
				int levelAttrValue;
				switch (parameter.constraint)
				{
					// Check if the value of an attribute of the tag is equal to a given value
					case "=":
						for (int t = tags.Count - 1; t >= 0; t--)
							if (tags[t].Attributes.GetNamedItem(parameter.attribute) == null || tags[t].Attributes.GetNamedItem(parameter.attribute).Value != parameter.value)
								tags.RemoveAt(t);
						break;
					// Check if the value of an attribute of the tag is not equal to a given value
					case "<>":
						for (int t = tags.Count - 1; t >= 0; t--)
							if (tags[t].Attributes.GetNamedItem(parameter.attribute) == null || tags[t].Attributes.GetNamedItem(parameter.attribute).Value == parameter.value)
								tags.RemoveAt(t);
						break;
					// Check if the value of an attribute of the tag is greater than a given value (for limit attribute consider -1 as infinite value)
					case ">":
						for (int t = tags.Count - 1; t >= 0; t--)
						{
							if (tags[t].Attributes.GetNamedItem(parameter.attribute) == null)
								tags.RemoveAt(t);
							else
							{
								try
								{
									levelAttrValue = int.Parse(tags[t].Attributes.GetNamedItem(parameter.attribute).Value);
									if (levelAttrValue <= int.Parse(parameter.value) && (parameter.attribute != "limit" || levelAttrValue != -1)) // because -1 means infinity for block limit
										tags.RemoveAt(t);
								}
								catch
								{
									tags.RemoveAt(t);
								}
							}
						}
						break;
					// Check if the value of an attribute of the tag is smaller than a given value (for limit attribute consider -1 as infinite value)
					case "<":
						for (int t = tags.Count - 1; t >= 0; t--)
						{
							if (tags[t].Attributes.GetNamedItem(parameter.attribute) == null)
								tags.RemoveAt(t);
							else
							{
								try
								{
									levelAttrValue = int.Parse(tags[t].Attributes.GetNamedItem(parameter.attribute).Value);
									if (levelAttrValue >= int.Parse(parameter.value) || (parameter.attribute == "limit" && levelAttrValue == -1)) // because -1 means infinity for block limit
										tags.RemoveAt(t);
								}
								catch
								{
									tags.RemoveAt(t);
								}
							}
						}
						break;
					// Check if the value of an attribute of the tag is greater than or equal a given value (for limit attribute consider -1 as infinite value)
					case ">=":
						for (int t = tags.Count - 1; t >= 0; t--)
						{
							if (tags[t].Attributes.GetNamedItem(parameter.attribute) == null)
								tags.RemoveAt(t);
							else
							{
								try
								{
									levelAttrValue = int.Parse(tags[t].Attributes.GetNamedItem(parameter.attribute).Value);
									if (levelAttrValue < int.Parse(parameter.value) && (parameter.attribute != "limit" || levelAttrValue != -1)) // because -1 means infinity for block limit
										tags.RemoveAt(t);
								}
								catch
								{
									tags.RemoveAt(t);
								}
							}
						}
						break;
					// Check if the value of an attribute of the tag is smaller than or equal a given value (for limit attribute consider -1 as infinite value)
					case "<=":
						for (int t = tags.Count - 1; t >= 0; t--)
						{
							if (tags[t].Attributes.GetNamedItem(parameter.attribute) == null)
								tags.RemoveAt(t);
							else
							{
								try
								{
									levelAttrValue = int.Parse(tags[t].Attributes.GetNamedItem(parameter.attribute).Value);
									if (levelAttrValue > int.Parse(parameter.value) || (parameter.attribute == "limit" && levelAttrValue == -1)) // because -1 means infinity for block limit
										tags.RemoveAt(t);
								}
								catch
								{
									tags.RemoveAt(t);
								}
							}
						}
						break;
					// Check if the attribute of the tag is included inside a given value
					case "include":
						for (int t = tags.Count - 1; t >= 0; t--)
						{
							if (tags[t].Attributes.GetNamedItem(parameter.attribute) == null || !parameter.value.Contains(tags[t].Attributes.GetNamedItem(parameter.attribute).Value))
								tags.RemoveAt(t);
						}
						break;
					// Check if the value of an attribute of a tag is equal to the value of an attribute of another tag
					case "sameValue":
						for (int t = tags.Count - 1; t >= 0; t--)
						{
							if (tags[t].Attributes.GetNamedItem(parameter.attribute) == null)
								tags.RemoveAt(t);
							else
							{
								bool found = false;
								foreach (XmlNode node in tags[t].OwnerDocument.GetElementsByTagName(parameter.tag2))
								{
									if (node != tags[t] && node.Attributes.GetNamedItem(parameter.attribute2) != null && node.Attributes.GetNamedItem(parameter.attribute2).Value == tags[t].Attributes.GetNamedItem(parameter.attribute).Value)
									{
										found = true;
										break;
									}
								}
								if (!found)
									tags.RemoveAt(t);
							}
						}
						break;
					// Check if a tag contains at least one child
					case "hasChild":
						for (int t = tags.Count - 1; t >= 0; t--)
						{
							if (!tags[t].HasChildNodes)
								tags.RemoveAt(t);
						}
						break;
				}
			}
		}
		// check the rule (combination of constraints)
		string rule = competency.rule;
		foreach (string key in constraintsState.Keys)
		{
			rule = rule.Replace(key, "" + constraintsState[key].Count);
		}
		DataTable dt = new DataTable();
		if (rule != "")
			return (bool)dt.Compute(rule, "");
		else
			return false;
	}

	// used for localization process to integrate inside expression some data
	public static string getFormatedText(string expression, params object[] data)
	{
		for (int i = 0; i < data.Length; i++)
			expression = expression.Replace("#" + i + "#", data[i].ToString());
		return expression;
	}

	public static string extractLocale(string content)
    {
		if (content == null) return "";
		string localKey = LocalizationSettings.Instance.GetSelectedLocale().Identifier.Code;
		if (content.Contains("[" + localKey + "]") && content.Contains("[/" + localKey + "]"))
        {
			int start = content.IndexOf("[" + localKey + "]") + localKey.Length + 2;
			int length = content.IndexOf("[/" + localKey + "]") - start;
			return content.Substring(start, length);
		}
		else
			return content;
	}
}