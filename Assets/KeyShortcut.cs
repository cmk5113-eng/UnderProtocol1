using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class NumberButtonShortcut : MonoBehaviour
{
    [SerializeField] private Button[] buttons = new Button[12];

    private readonly Key[] numberKeys =
    {
    Key.Digit1, Key.Digit2, Key.Digit3,
    Key.Digit4, Key.Digit5, Key.Digit6,
    Key.Digit7, Key.Digit8, Key.Digit9,
    Key.Digit0,
    Key.Backquote, // ` 키
    Key.Minus     // - 키
};

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        for (int i = 0; i < buttons.Length && i < numberKeys.Length; i++)
        {
            // 숫자키를 누른 순간에만 실행
            if (!keyboard[numberKeys[i]].wasPressedThisFrame)
                continue;

            Button button = buttons[i];

            // 버튼이 없거나 비활성 상태면 실행하지 않음
            if (button == null ||
                !button.isActiveAndEnabled ||
                !button.IsInteractable())
                continue;

            button.onClick.Invoke();
            return;
        }
    }
}