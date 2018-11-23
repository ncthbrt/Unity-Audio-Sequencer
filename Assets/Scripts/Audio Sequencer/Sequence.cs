using UnityEngine;

public class Sequence : MonoBehaviour
{
  public bool[] sequence;

  public bool[] sequenceSequence;

  [SerializeField] private int currentStep;
  [SerializeField] private int currentSequence;

  public int CurrentStep
  {
    get { return currentStep; }
  }

  public int CurrentSequence
  {
    get { return currentSequence; }
  }

  public void GoTo(int currentSequence, int currentStep)
  {
    this.currentStep = currentStep;
    this.currentSequence = currentSequence;
  }

  public void Reset()
  {
    this.currentSequence = 0;
    this.currentStep = 0;
  }

  public int Length { get { return sequence.Length; } }
  public int NumberOfSequences { get { return sequenceSequence.Length; } }

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

  public bool this[int sequence, int step]
  {
    get
    {
      return (sequenceSequence.Length == 0 || sequenceSequence[sequence]) && this.sequence[step];
    }
  }

  public bool ShouldTrigger { get { return this[CurrentSequence, CurrentStep]; } }

  public void IncrementStep()
  {
    ++currentStep;
    if (currentStep >= sequence.Length)
    {
      currentStep = 0;
      ++currentSequence;
      if (currentSequence >= sequenceSequence.Length)
      {
        currentSequence = 0;
      }
    }
  }
}
