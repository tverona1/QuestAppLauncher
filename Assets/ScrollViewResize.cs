using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QuestAppLauncher
{
    public class ScrollViewResize : UIBehaviour
    {
        protected override void OnRectTransformDimensionsChange()
        {
            var rect = transform.GetComponent<RectTransform>();
            var boxCollider = transform.GetComponent<BoxCollider>();
            boxCollider.size = new Vector3(rect.rect.width, rect.rect.height, 0);

            Debug.LogFormat("Resizing box collider: {0} x {1}", boxCollider.size.x, boxCollider.size.y);
        }
    }
}