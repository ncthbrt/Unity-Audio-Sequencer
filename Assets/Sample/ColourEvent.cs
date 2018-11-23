using UnityEngine;


public class ColourEvent : MonoBehaviour
{
  public Camera MainCamera;
  private Color[] _colors = new Color[]{
      Color.red,
      Color.green,
      Color.blue,
      Color.yellow
  };

  public void OnTrigger(Sequence sequence)
  {
    int i = Random.Range(1, _colors.Length);
    MainCamera.backgroundColor = _colors[i];
    _colors[i] = _colors[0];
    _colors[0] = MainCamera.backgroundColor;
  }
}
