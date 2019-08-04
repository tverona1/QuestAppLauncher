using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace QuestAppLauncher
{
    public class ScrollButtonHandler : MonoBehaviour
    {
        // Child object, used to determine size
        public GameObject prefabChild;

        // Scroll content
        public RectTransform content;

        [Tooltip("Button to go to the previous page")]
        public Button PrevButton;

        [Tooltip("Button to go to the next page")]
        public Button NextButton;

        [Tooltip("Whether tabs are horizontal (true) or vertical (false)")]
        public bool isHorizontal;

        // Size of child object (either width or height)
        private float childSize;

        // Current rect transform
        private RectTransform rectTransform;

        private const float CmToInch = 2.54f;
        private const float DragThresholdCM = 50f;

        void Start()
        {
            this.PrevButton.onClick.AddListener(() => { ScrollPrev(); });
            this.NextButton.onClick.AddListener(() => { ScrollNext(); });

            this.PrevButton.gameObject.SetActive(false);
            this.NextButton.gameObject.SetActive(false);

            this.rectTransform = GetComponent<RectTransform>();
            this.childSize = this.isHorizontal ? this.prefabChild.GetComponent<RectTransform>().sizeDelta.x :
                this.prefabChild.GetComponent<RectTransform>().sizeDelta.y;

            RefreshScrollContent(0);

            SetPhysicalDragThreshold();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void RefreshScrollContent(int numChildren)
        {
            Canvas.ForceUpdateCanvases();

            float contentSize = numChildren * this.childSize;
            var recTransformSize = this.isHorizontal ? this.rectTransform.sizeDelta.x :
                this.rectTransform.sizeDelta.y;

            if (recTransformSize >= contentSize)
            {
                // Viewport can fit the content, so hide the buttons
                this.PrevButton.gameObject.SetActive(false);
                this.NextButton.gameObject.SetActive(false);
            }
            else
            {
                this.PrevButton.gameObject.SetActive(true);
                this.NextButton.gameObject.SetActive(true);
            }
        }

        public void SetPhysicalDragThreshold()
        {
            Debug.LogFormat("Drag threshold: {0}", EventSystem.current.pixelDragThreshold);

            EventSystem.current.pixelDragThreshold = (int)(DragThresholdCM * Screen.dpi / CmToInch);
            Debug.LogFormat("Updated drag threshold: {0}", EventSystem.current.pixelDragThreshold);
        }

        public void ScrollPrev()
        {
            Vector2 newPosition;

            if (this.isHorizontal)
            {
                newPosition = new Vector2(this.content.transform.localPosition.x + this.childSize,
                this.content.transform.localPosition.y);
            }
            else
            {
                newPosition = new Vector2(this.content.transform.localPosition.x,
                this.content.transform.localPosition.y - this.childSize);
            }
            this.content.transform.localPosition = newPosition;
        }

        public void ScrollNext()
        {
            Vector2 newPosition = new Vector2(this.content.transform.localPosition.x - this.childSize, this.content.transform.localPosition.y);
            
            if (this.isHorizontal)
            {
                newPosition = new Vector2(this.content.transform.localPosition.x - this.childSize,
                    this.content.transform.localPosition.y);
            }
            else
            {
                newPosition = new Vector2(this.content.transform.localPosition.x,
                    this.content.transform.localPosition.y + this.childSize);
            }

            this.content.transform.localPosition = newPosition;
        }
    }
}
