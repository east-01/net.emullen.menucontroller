using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class VersionText : MonoBehaviour
{
    void Start()
    {
        // GetComponent<TMP_Text>().text = "v" + DevSettings.GetVersionString();        
    }
}
