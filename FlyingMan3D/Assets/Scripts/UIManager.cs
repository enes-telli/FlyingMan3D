using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject text;
    [SerializeField] private GameObject canvas;

    private void Awake()
    {
        Instance = this;
    }

    public void BadShot()
    {
        GameObject textGO = Instantiate(text, new Vector3(Screen.width * 0.5f, Screen.height * 0.7f, 0), Quaternion.identity);
        textGO.transform.SetParent(canvas.transform);
    }

}
