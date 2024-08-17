using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DashcamButton : Button {
    public override void OnSubmit(UnityEngine.EventSystems.BaseEventData eventData) {
        // Do nothing. This effectively ignores spacebar presses for this button.
    }
}
