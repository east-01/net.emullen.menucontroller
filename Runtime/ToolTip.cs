using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/** Show a tooltip for a single input type, will observe a specific player's 
      input and pick the corresponding image. */
public class ToolTip : MonoBehaviour
{

    [Header("Settings")] public float spacing = 5f;

    [Header("Components"), SerializeField] private Image img;
    [SerializeField] private TMP_Text text;

    [Header("Images"), SerializeField] private Sprite gamepadImage;
    [SerializeField] private Sprite keyboardMouseImage;

    private Image childImage;
    private PlayerInput observedInput;

    void Awake() 
    {
        childImage = GetComponentInChildren<Image>();
        if(childImage == null) 
            throw new InvalidOperationException("Failed to find child image.");
    }

    /* Yeah, this isn't really efficient. It would be better to do with OnControlsChanged event but led
         to problems when I tried it. If efficiency really becomes an issue I'll look into it again. */
    void Update() 
    {
        if(observedInput == null || observedInput.devices.Count == 0) return;

        childImage.sprite = observedInput.devices[0] is Gamepad ? gamepadImage : keyboardMouseImage;

        float totalWidth = GetComponent<RectTransform>().sizeDelta.x;
        float imgWidth = -img.gameObject.GetComponent<RectTransform>().sizeDelta.x;

        Vector4 margins = text.margin;
        margins.x = totalWidth-imgWidth+spacing;
        text.margin = margins;
    }

    public void SetObservedInput(PlayerInput input) 
    {
        observedInput = input;
    }

}