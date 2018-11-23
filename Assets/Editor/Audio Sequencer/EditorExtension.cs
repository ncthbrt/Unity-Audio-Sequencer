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
  SerializedProperty currentStep;

  void OnEnable()
  {
    sequence = serializedObject.FindProperty("sequence");
    currentStep = serializedObject.FindProperty("currentStep");
  }
  public override void OnInspectorGUI()
  {
    serializedObject.Update();
    sequence.arraySize = EditorGUILayout.IntSlider("Sequence Length", sequence.arraySize, 1, 128);
    int j = 0;
    var currentBeat = this.currentStep.intValue;

    EditorGUILayout.BeginVertical();
    EditorGUILayout.BeginHorizontal();
    var buttonsPerRow = 4;
    var prev = GUI.color;
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
        EditorGUILayout.EndVertical();
        j = 0;
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
      }
    }
    EditorGUILayout.EndHorizontal();
    EditorGUILayout.EndVertical();
    serializedObject.ApplyModifiedProperties();
  }

}
