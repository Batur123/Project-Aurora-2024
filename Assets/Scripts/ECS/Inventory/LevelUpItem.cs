using UnityEngine;
using UnityEngine.EventSystems;

public class LevelUpItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

    private LevelUpSlot currentSlot;

    public void SetSlot(LevelUpSlot slot)
    {
        currentSlot = slot;
    }
    
    public void OnPointerClick(PointerEventData eventData) {
        Debug.Log("Click current slot" + currentSlot.ToString());
        if (currentSlot == null) {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right) {
            //UIController.Instance.HideTooltip();
            //UIController.Instance.DropItemAtIndexToGround(currentSlot.SlotIndex);
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
