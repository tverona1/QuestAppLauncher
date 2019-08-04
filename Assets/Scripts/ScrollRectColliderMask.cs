using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ControllerSelection;

public class ScrollRectColliderMask : MonoBehaviour
{
    // Content
    public RectTransform content;

    // Tracking space used for ray cast
    public Transform trackingSpace = null;

    // Box collider used for ray cast
    private BoxCollider boxCollider = null;

    // Whether the pointer is within bounds of the scroll rect
    private bool isInBounds = true;
    private bool isInitialized = false;

    private OVRInput.Controller activeController = OVRInput.Controller.None;

    // Start is called before the first frame update
    void Start()
    {
        this.boxCollider = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        this.activeController = OVRInputHelpers.GetControllerForButton(OVRInput.Button.PrimaryIndexTrigger, this.activeController);
        Ray pointer = OVRInputHelpers.GetSelectionRay(this.activeController, this.trackingSpace);

        RaycastHit hit;
        if (this.boxCollider.Raycast(pointer, out hit, 500))
        {
            // We got a hit in the scroll view. Check if we're already within the bounds - if so, do nothing.
            if (!isInBounds)
            {
                // We entered the scroll view, so enable box colliders on children.
                foreach (var boxCollider in this.content.gameObject.GetComponentsInChildren<BoxCollider>())
                {
                    boxCollider.enabled = true;
                }

                isInBounds = true;
            }
        }
        else if (isInBounds)
        {
            // We are outside the scroll view and were previously inside, so disable box colliders on children.
            foreach (var boxCollider in this.content.gameObject.GetComponentsInChildren<BoxCollider>())
            {
                boxCollider.enabled = false;
            }

            isInBounds = false;
        }
    }
}