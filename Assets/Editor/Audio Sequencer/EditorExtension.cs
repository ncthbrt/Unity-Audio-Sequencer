using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
public static class EditorExtension
{
  public static int DrawBitMaskField(Rect aPosition, int aMask, System.Type aType, GUIContent aLabel)
  {
    var itemNames = System.Enum.GetNames(aType);
    var itemValues = System.Enum.GetValues(aType) as int[];

    int val = aMask;
    int maskVal = 0;
    for (int i = 0; i < itemValues.Length; i++)
    {
      if (itemValues[i] != 0)
      {
        if ((val & itemValues[i]) == itemValues[i])
          maskVal |= 1 << i;
      }
      else if (val == 0)
        maskVal |= 1 << i;
    }
    int newMaskVal = EditorGUI.MaskField(aPosition, aLabel, maskVal, itemNames);
    int changes = maskVal ^ newMaskVal;

    for (int i = 0; i < itemValues.Length; i++)
    {
      if ((changes & (1 << i)) != 0)            // has this list item changed?
      {
        if ((newMaskVal & (1 << i)) != 0)     // has it been set?
        {
          if (itemValues[i] == 0)           // special case: if "0" is set, just set the val to 0
          {
            val = 0;
            break;
          }
          else
            val |= itemValues[i];
        }
        else                                  // it has been reset
        {
          val &= ~itemValues[i];
        }
      }
    }
    return val;
  }
}

[CustomPropertyDrawer(typeof(BitMaskAttribute))]
public class EnumBitMaskPropertyDrawer : PropertyDrawer
{
  public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
  {
    label.text = label.text;
    prop.intValue = EditorExtension.DrawBitMaskField(position, prop.intValue, fieldInfo.FieldType, label);
  }
}


[CustomEditor(typeof(Sequence))]
[CanEditMultipleObjects]
public class SequenceEditor : Editor
{

  SerializedProperty sequence;
  SerializedProperty sequenceSequence;
  SerializedProperty currentStep;
  SerializedProperty currentSequence;

  void OnEnable()
  {
    sequence = serializedObject.FindProperty("sequence");
    sequenceSequence = serializedObject.FindProperty("sequenceSequence");
    currentStep = serializedObject.FindProperty("currentStep");
    currentSequence = serializedObject.FindProperty("currentSequence");
  }
  public override void OnInspectorGUI()
  {
    serializedObject.Update();
    EditorGUILayout.BeginVertical();
    var currentBeat = this.currentStep.intValue;
    var currentSequence = this.currentSequence.intValue;
    var sequenceIsActive = !Application.isPlaying || this.sequenceSequence.arraySize == 0 || this.sequenceSequence.GetArrayElementAtIndex(currentSequence).boolValue;
    var beatIsActive = Application.isPlaying && currentBeat < this.sequence.arraySize && this.sequence.GetArrayElementAtIndex(currentBeat).boolValue;
    var prev = GUI.color;
    GUI.color = beatIsActive ? Color.green : prev;
    sequence.arraySize = EditorGUILayout.IntSlider("Sequence Length", sequence.arraySize, 1, 128);
    int j = 0;
    GUI.color = prev;
    EditorGUILayout.BeginHorizontal();
    var buttonsPerRow = 4;

    EditorGUI.BeginDisabledGroup(!sequenceIsActive);
    for (var i = 0; i < sequence.arraySize; ++i)
    {
      var item = sequence.GetArrayElementAtIndex(i);
      if (i == currentBeat)
      {
        GUI.color = Color.green;
        item.boolValue = EditorGUILayout.Toggle(item.boolValue, "Button");
      }
      else
      {
        GUI.color = prev;
        item.boolValue = EditorGUILayout.Toggle(item.boolValue, "Button");
      }

      if (j >= buttonsPerRow - 1)
      {
        EditorGUILayout.EndHorizontal();
        j = 0;
        EditorGUILayout.BeginHorizontal();
      }
    }
    GUI.color = prev;
    EditorGUI.EndDisabledGroup();
    EditorGUILayout.EndHorizontal();
    EditorGUILayout.Space();

    if (sequenceSequence.arraySize > 0)
    {
      GUI.color = sequenceIsActive && Application.isPlaying ? Color.blue : prev;
      sequenceSequence.arraySize = EditorGUILayout.IntSlider("Sequence Sequence Length", sequenceSequence.arraySize, 0, 128);
      GUI.color = prev;
      EditorGUILayout.BeginHorizontal();
      for (var i = 0; i < sequenceSequence.arraySize; ++i)
      {
        var item = sequenceSequence.GetArrayElementAtIndex(i);
        if (i == currentSequence)
        {
          GUI.color = Color.blue;
          item.boolValue = EditorGUILayout.Toggle(item.boolValue, "Button");
        }
        else
        {
          GUI.color = prev;
          item.boolValue = EditorGUILayout.Toggle(item.boolValue, "Button");
        }

        if (j >= buttonsPerRow - 1)
        {
          EditorGUILayout.EndHorizontal();
          j = 0;
          EditorGUILayout.BeginHorizontal();
        }
      }
      EditorGUILayout.EndHorizontal();
    }
    else
    {
      var sequence = EditorGUILayout.Toggle("Sequence Sequence?", false);
      if (sequence)
      {
        sequenceSequence.arraySize = 1;
        sequenceSequence.GetArrayElementAtIndex(0).boolValue = false;
      }
    }
    EditorGUILayout.EndVertical();
    serializedObject.ApplyModifiedProperties();

  }

}
