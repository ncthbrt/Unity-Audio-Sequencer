using UnityEngine;

public class Sequence : MonoBehaviour
{
  public bool[] sequence;
  [SerializeField] private int currentStep;

  public int CurrentStep { get { return currentStep; } set { currentStep = value; } }

  public int Length { get { return sequence.Length; } }

  public bool this[int key]
  {
    get
    {
      return sequence[key];
    }
    set
    {
      sequence[key] = value;
    }
  }


}
