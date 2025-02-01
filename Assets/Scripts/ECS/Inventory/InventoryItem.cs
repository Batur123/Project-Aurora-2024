using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryItem : MonoBehaviour, 
    IBeginDragHandler, IDragHandler, 
    IEndDragHandler, IPointerClickHandler, 
    IPointerEnterHandler, IPointerExitHandler
{
    private Canvas inventoryCanvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private InventorySlot currentSlot;

    private Vector2 originalAnchoredPosition;

    private bool isDragging = false;
    private float dragThreshold = 5f; 

    private Vector2 startDragPosition;

    private void Awake()
    {
        inventoryCanvas = UIController.Instance.GetInventoryCanvas();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetSlot(InventorySlot slot)
    {
        currentSlot = slot;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        UIController.Instance.HideTooltip();
        
        if (currentSlot == null)
        {
            return;
        }

        Vector2 localPoint;
        RectTransform canvasRect = inventoryCanvas.GetComponent<RectTransform>();
        Camera uiCamera = inventoryCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : inventoryCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, uiCamera, out localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
 
        rectTransform.SetParent(inventoryCanvas.transform, false);

        originalAnchoredPosition = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false; 
        isDragging = false;
        startDragPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        UIController.Instance.HideTooltip();
        
        float distance = Vector2.Distance(eventData.position, startDragPosition);
        if (!isDragging && distance > dragThreshold)
        {
            isDragging = true;
        }

        if (isDragging)
        {
            Vector2 localPoint;
            RectTransform canvasRect = inventoryCanvas.GetComponent<RectTransform>();

            Camera uiCamera = inventoryCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : inventoryCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, uiCamera, out localPoint))
            {
                rectTransform.anchoredPosition = localPoint;
                rectTransform.SetAsLastSibling();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (!isDragging)
        {
            rectTransform.SetParent(currentSlot.transform, false);
            rectTransform.anchoredPosition = Vector2.zero;
            return;
        }

        GameObject droppedOn = eventData.pointerCurrentRaycast.gameObject;
        InventorySlot targetSlot = null;

        if (droppedOn != null)
        {
            targetSlot = droppedOn.GetComponent<InventorySlot>();
            if (targetSlot == null)
            {
                targetSlot = droppedOn.GetComponentInParent<InventorySlot>();
            }
        }

       
        if (currentSlot != null && targetSlot != null && targetSlot != currentSlot)
        {
            var isSwapped = UIController.Instance.SwapItems(currentSlot.SlotIndex, targetSlot.SlotIndex);
            if (!isSwapped) {
                rectTransform.SetParent(currentSlot.transform, false);
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }
        else
        {
            rectTransform.SetParent(currentSlot.transform, false);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
    
    // Drop Item from Inventory to Ground
    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentSlot == null)
        {
            return;
        }
        
        // Check for right-click
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            UIController.Instance.HideTooltip();
            UIController.Instance.DropItemAtIndexToGround(currentSlot.SlotIndex);
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData) {
        if (currentSlot == null) return;
        UIController.Instance.ShowTooltip(eventData.position, currentSlot.SlotIndex);
    }

    public void OnPointerExit(PointerEventData eventData) {
        UIController.Instance.HideTooltip();
    }
}
