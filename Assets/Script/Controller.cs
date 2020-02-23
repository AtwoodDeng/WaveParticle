using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{

    public Slider SpeedSlider;
    public Slider RadiusSlider;
    public Slider HeightSlider;
    public Slider DxDzSlider;

    public Material[] materials;

    public void UpdateParameters(Material mat )
    {
        mat.SetFloat("_Speed",SpeedSlider.value);
        mat.SetFloat("_Radius", RadiusSlider.value);
        mat.SetFloat("_Height", HeightSlider.value);
        mat.SetFloat("_DxDz", Mathf.Pow(10f, DxDzSlider.value -1f));
    }
    public void Update()
    {
        foreach (var mat in materials)
        {
            UpdateParameters(mat);
        }
    }
}
