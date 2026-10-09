// Ashen Hollow: reads touches, mouse and keyboard with either of Unity's input systems
using System.Collections.Generic;
using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public struct AHPointer
{
    public int id;          // finger id; -1 is the left mouse button, -2 the right one
    public Vector2 pos;     // screen pixels
}

public static class AHInput
{
    static readonly List<AHPointer> list = new List<AHPointer>();

    // every finger (or mouse button) that is down this frame
    public static List<AHPointer> Pointers()
    {
        list.Clear();
#if ENABLE_LEGACY_INPUT_MANAGER
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
            list.Add(new AHPointer { id = t.fingerId, pos = t.position });
        }
        if (Input.touchCount == 0)
        {
            if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0)) list.Add(new AHPointer { id = -1, pos = Input.mousePosition });
            if (Input.GetMouseButton(1) || Input.GetMouseButtonDown(1)) list.Add(new AHPointer { id = -2, pos = Input.mousePosition });
        }
#elif ENABLE_INPUT_SYSTEM
        bool touched = false;
        Touchscreen ts = Touchscreen.current;
        if (ts != null)
        {
            foreach (var t in ts.touches)
            {
                if (!t.press.isPressed && !t.press.wasPressedThisFrame) continue;   // a very quick tap still counts
                touched = true;
                list.Add(new AHPointer { id = t.touchId.ReadValue(), pos = t.position.ReadValue() });
            }
        }
        Mouse m = Mouse.current;
        if (!touched && m != null)
        {
            if (m.leftButton.isPressed || m.leftButton.wasPressedThisFrame) list.Add(new AHPointer { id = -1, pos = m.position.ReadValue() });
            if (m.rightButton.isPressed || m.rightButton.wasPressedThisFrame) list.Add(new AHPointer { id = -2, pos = m.position.ReadValue() });
        }
#endif
        return list;
    }

    // a held direction set by an editor test (walking the hero for snapshots); zero in play
    public static Vector2 TestMove;
    // WASD or arrow keys, for playing in the editor
    public static Vector2 Keys()
    {
        Vector2 v = TestMove;
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x += 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1;
#elif ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k != null)
        {
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1;
        }
#endif
        return v.sqrMagnitude > 1f ? v.normalized : v;
    }

    public static bool AttackKey()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.Space);
#elif ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#else
        return false;
#endif
    }

    public static bool DodgeKey()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift);
#elif ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#else
        return false;
#endif
    }

    // number keys 1 to 5 cast the spells
    public static bool SpellKey(int i)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha1 + i);
#elif ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k == null) return false;
        switch (i)
        {
            case 0: return k.digit1Key.wasPressedThisFrame;
            case 1: return k.digit2Key.wasPressedThisFrame;
            case 2: return k.digit3Key.wasPressedThisFrame;
            case 3: return k.digit4Key.wasPressedThisFrame;
            case 4: return k.digit5Key.wasPressedThisFrame;
            case 5: return k.digit6Key.wasPressedThisFrame;
        }
        return false;
#else
        return false;
#endif
    }

    // letters typed this frame (for typing a name); '\b' is backspace, '\n' is enter
    static readonly List<char> typed = new List<char>();
    static bool hooked;
    public static List<char> TypedChars()
    {
        var outp = new List<char>();
#if ENABLE_LEGACY_INPUT_MANAGER
        foreach (char c in Input.inputString) outp.Add(c == '\r' ? '\n' : c);
#elif ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null && !hooked) { hooked = true; k.onTextInput += c => { if (typed.Count < 64) typed.Add(c); }; }
        outp.AddRange(typed); typed.Clear();
        if (k != null)
        {
            if (k.backspaceKey.wasPressedThisFrame && !outp.Contains('\b')) outp.Add('\b');
            if ((k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) && !outp.Contains('\r') && !outp.Contains('\n')) outp.Add('\n');
        }
        for (int i = 0; i < outp.Count; i++) if (outp[i] == '\r') outp[i] = '\n';
#endif
        return outp;
    }

    // B opens and closes the bag
    public static bool BagKey()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.B);
#elif ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    // E talks to whoever is next to you
    public static bool TalkKey()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#elif ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    // H drinks a health potion, J a mana potion
    // M: the map
    public static bool MapKey()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.M);
#elif ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    // F: lightfoot leap
    public static bool LeapKey()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.F);
#elif ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    // R: get on or off your mount
    public static bool RideKey()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#elif ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    public static bool PotionKey(bool mana)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(mana ? KeyCode.J : KeyCode.H);
#elif ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (mana ? Keyboard.current.jKey : Keyboard.current.hKey).wasPressedThisFrame;
#else
        return false;
#endif
    }

    // Escape closes a window
    public static bool BackKey()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#elif ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    // where the mouse is (for the wheel: zoom the hero or scroll the list it is over)
    public static Vector2 MousePos()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Vector2.zero;
#endif
    }

    // mouse wheel, in notches
    public static float Wheel()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mouseScrollDelta.y;
#elif ENABLE_INPUT_SYSTEM
        // older Input System versions give 120 per notch on Windows, newer ones give 1 per notch
        if (Mouse.current == null) return 0f;
        float y = Mouse.current.scroll.ReadValue().y;
        return Mathf.Abs(y) >= 20f ? y / 120f : y;
#else
        return 0f;
#endif
    }
}
