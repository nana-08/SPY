using UnityEngine;
using System.Collections.Generic;

// identical to Activable but useful to make the difference between a console and a floor switch
public class ActivableSwitch : MonoBehaviour
{
    public int weight;
    public List<int> slotID; // target slot this component control
}
