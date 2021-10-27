using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator : MonoBehaviour
{
    [SerializeField] private GameObject needle;
    [SerializeField] private PlayerController player;

    private float startPos = -50f, endPos = -130f, desiredPos;
    private float speed;
    private bool up;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            float launchFactor = CreateLaunchForce();
            
            StartCoroutine(player.ApplyLaunchForce(launchFactor));

            enabled = false;
        }
        else
        {
            if (up)
            {
                speed += Time.deltaTime * 150f;
                if (speed > 179f) { up = false; }
            }
            else
            {
                speed -= Time.deltaTime * 150f;
                if (speed < 1f) { up = true; }
            }

            desiredPos = startPos - endPos;
            float temp = speed / 180;
            needle.transform.localEulerAngles = new Vector3(startPos - temp * desiredPos, 0, 0);
        }
        
    }

    private float CreateLaunchForce()
    {
        speed = Mathf.Abs(90f - speed);

        if (speed > 70)
        {
            UIManager.Instance.Invoke("BadShot", 0.5f);
            GameManager.Instance.Invoke("GameOver", 2.5f);
            Debug.Log("red");
            return 0.1f;
        }
        else if (speed > 50)
        {
            Debug.Log("orange");
            return 0.65f;
        }
        else if (speed > 30)
        {
            Debug.Log("yellow");
            return 0.75f;
        }
        else if (speed > 10)
        {
            Debug.Log("light green");
            return 0.85f;
        }
        else
        {
            Debug.Log("green");
            return 1.0f;
        }
    }

}
