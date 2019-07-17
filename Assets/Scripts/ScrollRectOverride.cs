using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollRectOverride : ScrollRect, IMoveHandler, IPointerClickHandler, IScrollHandler
{
    private const float speedMultiplier = 15f;
    private float cellHeight = 0f;

    void Start()
    {
        this.cellHeight = this.transform.GetComponentInChildren<GridLayoutGroup>().cellSize.y;
    }

    void Update()
    {
        // Get vector from either left or right thumbstick
        var moveVector = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        if (moveVector.x == 0 && moveVector.y == 0)
        {
            moveVector = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        }

        // Scroll by a fixed amount proportional to thumbstick position on each frame
        // and map this to a fraction of the total viewport size:
        //   moveVector.y: The thumbstick vertical position normalized to [-1,1].
        //   Time.deltaTime: The time delta since last frame
        //   speedMultiplier: Just a multiplier to get a good scrolling speed.
        // So, moveVector.y * Time.deltaTime * speedMultiplier = the amount to scroll in "units"
        //   proportional to thumbstick position since last frame.
        // this.cellHeight / this.content.sizeDelta.y = cell height / total content height.
        float verticalIncrement = moveVector.y * Time.deltaTime * speedMultiplier * this.cellHeight / this.content.sizeDelta.y;
        this.verticalNormalizedPosition = Mathf.Clamp01(this.verticalNormalizedPosition + verticalIncrement);
    }

    public void OnPointerClick(PointerEventData e)
    {
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
    }

    void IMoveHandler.OnMove(AxisEventData e)
    {
    }

    void IScrollHandler.OnScroll(PointerEventData eventData)
    {
    }

    void OnMouseDrag()
    {
    }

    void OnMouseUp()
    {
    }

    void OnMouseDown()
    {
    }
}
