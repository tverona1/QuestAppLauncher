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

        // Width of child object
        private float childWidth;

        // Current rect transform
        private RectTransform rectTransform;

        private const float CmToInch = 2.54f;
        private const float DragThresholdCM = 50f;

        // Start is called before the first frame update
        void Start()
        {
            this.PrevButton.onClick.AddListener(() => { ScrollPrev(); });
            this.NextButton.onClick.AddListener(() => { ScrollNext(); });

            this.PrevButton.gameObject.SetActive(false);
            this.NextButton.gameObject.SetActive(false);

            this.rectTransform = GetComponent<RectTransform>();
            this.childWidth = this.prefabChild.GetComponent<RectTransform>().sizeDelta.x;

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

            float contentSize = numChildren * this.childWidth;

            if (this.rectTransform.sizeDelta.x >= contentSize)
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
            Vector2 newPosition = new Vector2(this.content.transform.localPosition.x + this.childWidth,
                this.content.transform.localPosition.y);
            this.content.transform.localPosition = newPosition;
            this.NextButton.interactable = true;
        }

        public void ScrollNext()
        {
            Vector2 newPosition = new Vector2(this.content.transform.localPosition.x - this.childWidth, this.content.transform.localPosition.y);
            this.content.transform.localPosition = newPosition;
            this.PrevButton.interactable = true;
        }
    }
}
