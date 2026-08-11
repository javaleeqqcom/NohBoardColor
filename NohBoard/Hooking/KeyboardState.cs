/*
Copyright (C) 2016 by Eric Bataille <e.c.p.bataille@gmail.com>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace ThoNohT.NohBoard.Hooking
{
    using System.Collections.Generic;
    using System.Linq;
    using static Interop.Defines;
    using static Interop.FunctionImports;

    /// <summary>
    /// A class representing the current state of the keyboard. I.e. which buttons are pressed.
    /// </summary>
    public class KeyboardState : StateBase<int>
    {
        /// <summary>
        /// Keys that were newly pressed while a physical Shift key was held. They retain their shifted label until
        /// their own hold/fade lifetime ends, even after Shift itself is released.
        /// </summary>
        private static readonly HashSet<int> shiftStateKeys = new HashSet<int>();

        /// <summary>
        /// A dictionary mapping key codes to the key codes of the state keys they update.
        /// </summary>
        private static Dictionary<int, int> StateKeys = new Dictionary<int, int>
        {
            { VK_CAPITAL, 1026 },
            { VK_NUMLOCK, 1027 },
            { VK_SCROLL, 1028 }
        };

        /// <summary>
        /// Initializes the state of state keys.
        /// </summary>
        static KeyboardState()
        {
            foreach (var key in StateKeys)
            {
                // Note that during this initialization, hold is not relevant as we are only adding keys that are
                // currently active. So passing 0 is not an issue.
                if (CheckStateKey(key.Key)) AddPressedElement(key.Value, 0);
            }
        }

        /// <summary>
        /// Returns a value indicating whether any shift key is currently down.
        /// </summary>
        public static bool ShiftDown
        {
            get
            {
                lock (pressedKeys)
                    return IsPhysicallyPressed(VK_LSHIFT) || IsPhysicallyPressed(VK_RSHIFT);
            }
        }

        /// <summary>
        /// Returns a value indicating whether any ctrl key is currently down.
        /// </summary>
        public static bool CtrlDown
        {
            get { lock (pressedKeys) return pressedKeys.ContainsKey(VK_LCTRL) || pressedKeys.ContainsKey(VK_RCTRL); }
        }

        /// <summary>
        /// Returns a value indicating whether any alt key is currently down.
        /// </summary>
        public static bool AltDown
        {
            get { lock (pressedKeys) return pressedKeys.ContainsKey(VK_LALT) || pressedKeys.ContainsKey(VK_RALT); }
        }

        /// <summary>
        /// Returns a value indicating whether caps lock is currently active.
        /// </summary>
        public static bool CapsActive => CheckStateKey(VK_CAPITAL);

        /// <summary>
        /// Returns physically held keyboard keys, excluding logical state indicators such as Caps Lock.
        /// </summary>
        public static IReadOnlyList<int> PhysicallyPressedKeys
        {
            get
            {
                lock (pressedKeys)
                    return pressedKeys
                        .Where(k => !k.Value.removed && !StateKeys.Values.Contains(k.Key))
                        .Select(k => k.Key)
                        .ToList()
                        .AsReadOnly();
            }
        }

        /// <summary>
        /// Returns keys whose shifted label should remain visible after physical Shift is released.
        /// </summary>
        public static IReadOnlyList<int> ShiftStateKeys
        {
            get
            {
                lock (pressedKeys)
                {
                    shiftStateKeys.RemoveWhere(key => !pressedKeys.ContainsKey(key));
                    return shiftStateKeys.ToList().AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Returns whether a key should retain its shifted label after physical Shift is released.
        /// </summary>
        public static bool IsShiftStateKey(int keyCode)
        {
            lock (pressedKeys)
                return pressedKeys.ContainsKey(keyCode) && shiftStateKeys.Contains(keyCode);
        }

        /// <summary>
        /// Checks the state of all keys and removes the ones that are no longer pressed from the list of pressed keys.
        /// </summary>
        /// <param name="hold">The minimum time to hold keys.</param>
        public static void CheckKeys(int hold, bool fade = false)
        {
            lock (pressedKeys)
            {
                if (!pressedKeys.Any()) return;

                foreach (var key in pressedKeys.Where(t => KeyIsUp(t.Key)).Select(t => t.Key).ToList())
                {
                    RemovePressedElement(key, hold, fade);
                }

                TryStopStopwatch();
            }
        }

        /// <summary>
        /// Adds the specified mouse keycode to the list of pressed keys.
        /// </summary>
        /// <param name="keyCode">The keycode to add.</param>
        /// <param name="hold">The minimum time to hold keys.</param>
        public static void AddPressedElement(int keyCode, int hold, bool fade = false)
        {
            lock (pressedKeys)
            {
                EnsureStopwatchRunning();

                var time = keyHoldStopwatch.ElapsedMilliseconds;

                var alreadyPhysicallyPressed = pressedKeys.TryGetValue(keyCode, out var existingPress)
                    && !existingPress.removed;
                if (!alreadyPhysicallyPressed
                    && !IsShiftKey(keyCode)
                    && !StateKeys.Values.Contains(keyCode))
                {
                    if (IsPhysicallyPressed(VK_LSHIFT) || IsPhysicallyPressed(VK_RSHIFT))
                        shiftStateKeys.Add(keyCode);
                    else
                        shiftStateKeys.Remove(keyCode);
                }

                TryToggleStateKey(keyCode, hold, fade);

                if (pressedKeys.TryGetValue(keyCode, out var pressed))
                {
                    pressed.startTime = time;
                    pressed.removed = false;
                    pressedKeys[keyCode] = pressed;
                }
                else
                {
                    pressedKeys.Add(
                        keyCode,
                        new KeyPress
                        {
                            startTime = keyHoldStopwatch.ElapsedMilliseconds,
                            removed = false
                        });

                    updated = true;
                }

            }
        }

        /// <summary>
        /// Attempts to toggle a key that can have a state. If the key is valid for having a state, the current state
        /// is looked up and removed or added from the list of pressed keys.
        /// </summary>
        /// <param name="keyCode">The key code of the key to check.</param>
        /// <param name="hold">The minimum time to hold keys.</param>
        private static void TryToggleStateKey(int keyCode, int hold, bool fade)
        {
            if (!StateKeys.TryGetValue(keyCode, out var stateKey)) return;

            // The state at this moment is that before the switch.
            if (!CheckStateKey(keyCode))
            {
                AddPressedElement(stateKey, hold);
            }
            else
            {
                RemovePressedElement(stateKey, hold, fade);
            }
        }

        /// <summary>
        /// Removes the specified keycode from the list of pressed keys.
        /// </summary>
        /// <param name="keyCode">The keycode to remove.</param>
        /// <param name="hold">The minimum time to hold keys.</param>
        public static void RemovePressedElement(int keyCode, int hold, bool fade = false)
        {
            if (IsShiftKey(keyCode))
            {
                lock (pressedKeys)
                {
                    if (pressedKeys.Remove(keyCode)) updated = true;
                    TryStopStopwatch();
                }

                return;
            }

            ReleasePressedElement(keyCode, hold, fade);
        }

        /// <summary>
        /// Returns whether a tracked key is still physically down rather than retained for hold/fade rendering.
        /// </summary>
        private static bool IsPhysicallyPressed(int keyCode)
        {
            return pressedKeys.TryGetValue(keyCode, out var pressed) && !pressed.removed;
        }

        /// <summary>
        /// Returns whether a key code represents either physical Shift key.
        /// </summary>
        private static bool IsShiftKey(int keyCode)
        {
            return keyCode == VK_LSHIFT || keyCode == VK_RSHIFT;
        }

        /// <summary>
        /// Checks whether the state key is active.
        /// </summary>
        /// <param name="keyCode">The key code of the key to check the state of.</param>
        /// <returns>A value indicating whether its state is active.</returns>
        private static bool CheckStateKey(int keyCode)
        {
            return (GetKeyState(keyCode) & 0x1) != 0;
        }

        /// <summary>
        /// Checks whether the specified key is up.
        /// </summary>
        /// <param name="keyCode">The keycode to check.</param>
        /// <returns><c>true</c> if it is up, <c>false</c> otherwise.</returns>
        private static bool KeyIsUp(int keyCode)
        {
            if (StateKeys.Values.Contains(keyCode))
            {
                var actualCode = StateKeys.Single(k => k.Value == keyCode).Key;
                return !CheckStateKey(actualCode);
            }

            return GetKeyState(keyCode) >= 0;
        }
    }
}
