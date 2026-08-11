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

namespace ThoNohT.NohBoard.Forms
{
    using System;
    using System.Drawing;
    using System.Drawing.Text;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Extra;
    using Hooking;
    using Hooking.Interop;
    using Keyboard.ElementDefinitions;

    /// <summary>
    /// Window appearance, top-most controls and global shortcut behavior for <see cref="MainForm"/>.
    /// </summary>
    public partial class MainForm
    {
        private const int ToggleTopMostHotKeyId = 0x4E42;
        private const int ToggleWindowDimmingHotKeyId = 0x4E43;
        private const int WmHotKey = 0x0312;
        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint VkT = 0x54;
        private const uint VkO = 0x4F;

        private Button topMostButton;
        private ToolStripMenuItem mnuAlwaysOnTop;
        private ToolStripMenuItem mnuWindowDimming;
        private ToolTip appearanceToolTip;
        private KeyboardOverlayForm inactiveKeyCapsOverlay;
        private KeyboardOverlayForm inactiveKeyTextOverlay;
        private KeyboardOverlayForm pressedKeysOverlay;
        private bool renderInactiveBackgroundOnly;
        private bool changingWindowFrame;
        private bool topMostHotKeyRegistered;
        private IntPtr topMostHotKeyHandle;
        private bool windowDimmingHotKeyRegistered;
        private IntPtr windowDimmingHotKeyHandle;

        /// <summary>
        /// Registers the global top-most shortcut whenever the native window handle is created or recreated.
        /// </summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            this.topMostHotKeyRegistered = RegisterHotKey(
                this.Handle,
                ToggleTopMostHotKeyId,
                ModControl | ModAlt,
                VkT);
            this.topMostHotKeyHandle = this.topMostHotKeyRegistered ? this.Handle : IntPtr.Zero;
            this.windowDimmingHotKeyRegistered = RegisterHotKey(
                this.Handle,
                ToggleWindowDimmingHotKeyId,
                ModControl | ModAlt,
                VkO);
            this.windowDimmingHotKeyHandle = this.windowDimmingHotKeyRegistered ? this.Handle : IntPtr.Zero;
            this.UpdateTopMostControlText();
            this.UpdateWindowDimmingControlText();
        }

        /// <summary>
        /// Releases the global top-most shortcut before the native window handle is destroyed.
        /// </summary>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (this.topMostHotKeyRegistered)
                UnregisterHotKey(this.topMostHotKeyHandle, ToggleTopMostHotKeyId);
            if (this.windowDimmingHotKeyRegistered)
                UnregisterHotKey(this.windowDimmingHotKeyHandle, ToggleWindowDimmingHotKeyId);

            this.topMostHotKeyRegistered = false;
            this.topMostHotKeyHandle = IntPtr.Zero;
            this.windowDimmingHotKeyRegistered = false;
            this.windowDimmingHotKeyHandle = IntPtr.Zero;
            base.OnHandleDestroyed(e);
        }

        /// <summary>
        /// Handles the global top-most shortcut.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotKey && m.WParam.ToInt32() == ToggleTopMostHotKeyId)
            {
                this.ToggleTopMost();
                return;
            }
            if (m.Msg == WmHotKey && m.WParam.ToInt32() == ToggleWindowDimmingHotKeyId)
            {
                this.ToggleWindowDimming();
                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Adds the pin button and context-menu command to the main window.
        /// </summary>
        private void InitializeAppearanceControls()
        {
            this.appearanceToolTip = new ToolTip(this.components);
            this.topMostButton = new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = SystemColors.Control,
                FlatStyle = FlatStyle.Popup,
                Font = new Font("Segoe UI Emoji", 8.25F, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(Math.Max(0, this.ClientSize.Width - 25), 3),
                Name = "topMostButton",
                Size = new Size(22, 22),
                TabStop = false,
                Text = "\uD83D\uDCCC",
                UseVisualStyleBackColor = false,
                Visible = false
            };
            this.topMostButton.Click += (s, e) => this.ToggleTopMost();
            this.Controls.Add(this.topMostButton);

            this.mnuAlwaysOnTop = new ToolStripMenuItem
            {
                Name = "mnuAlwaysOnTop",
                Size = new Size(202, 22)
            };
            this.mnuAlwaysOnTop.Click += (s, e) => this.ToggleTopMost();
            this.MainMenu.Items.Insert(1, this.mnuAlwaysOnTop);

            this.mnuWindowDimming = new ToolStripMenuItem
            {
                Name = "mnuWindowDimming",
                Size = new Size(202, 22)
            };
            this.mnuWindowDimming.Click += (s, e) => this.ToggleWindowDimming();
            this.MainMenu.Items.Insert(2, this.mnuWindowDimming);
            this.MainMenu.Closed += (s, e) => this.menuOpen = false;

            this.inactiveKeyCapsOverlay = new KeyboardOverlayForm(this.RenderInactiveKeyCaps);
            this.inactiveKeyTextOverlay = new KeyboardOverlayForm(this.RenderInactiveKeyText);
            this.pressedKeysOverlay = new KeyboardOverlayForm(this.RenderOpaquePressedKeys);
            this.UpdateTopMostControlText();
            this.UpdateWindowDimmingControlText();
        }

        /// <summary>
        /// Applies persisted appearance settings to the main window.
        /// </summary>
        private void ApplyAppearanceSettings()
        {
            this.TopMost = GlobalSettings.Settings.AlwaysOnTop;
            this.UpdateTopMostControlText();
            this.UpdateWindowDimmingControlText();
            this.UpdateWindowAppearance(true);
        }

        /// <summary>
        /// Toggles the main window's top-most state and persists it immediately.
        /// </summary>
        private void ToggleTopMost()
        {
            if (GlobalSettings.Settings == null) return;

            GlobalSettings.Settings.AlwaysOnTop = !GlobalSettings.Settings.AlwaysOnTop;
            this.TopMost = GlobalSettings.Settings.AlwaysOnTop;
            GlobalSettings.Save();
            this.UpdateTopMostControlText();
            this.SyncAppearanceOverlays();
        }

        /// <summary>
        /// Toggles inactive-window dimming and persists it immediately.
        /// </summary>
        private void ToggleWindowDimming()
        {
            if (GlobalSettings.Settings == null) return;

            GlobalSettings.Settings.DimInactiveWindow = !GlobalSettings.Settings.DimInactiveWindow;
            GlobalSettings.Save();
            this.UpdateWindowDimmingControlText();
            this.UpdateWindowAppearance(true);
            this.Refresh();
        }

        /// <summary>
        /// Updates the pin button and menu command to reflect the current top-most state.
        /// </summary>
        private void UpdateTopMostControlText()
        {
            if (this.topMostButton == null || this.mnuAlwaysOnTop == null) return;

            var shortcut = this.topMostHotKeyRegistered ? "Ctrl+Alt+T" : "shortcut unavailable";
            var state = this.TopMost ? "On" : "Off";
            this.mnuAlwaysOnTop.Checked = this.TopMost;
            this.mnuAlwaysOnTop.Text = $"Always on top: {state}";
            this.mnuAlwaysOnTop.ShortcutKeyDisplayString = shortcut;
            this.topMostButton.BackColor = this.TopMost ? Color.LightSkyBlue : SystemColors.Control;
            this.appearanceToolTip.SetToolTip(
                this.topMostButton,
                $"Always on top: {state}. Click to toggle ({shortcut}).");
        }

        /// <summary>
        /// Updates the context-menu command for inactive-window dimming.
        /// </summary>
        private void UpdateWindowDimmingControlText()
        {
            if (this.mnuWindowDimming == null || GlobalSettings.Settings == null) return;

            var shortcut = this.windowDimmingHotKeyRegistered ? "Ctrl+Alt+O" : "shortcut unavailable";
            var enabled = GlobalSettings.Settings.DimInactiveWindow;
            this.mnuWindowDimming.Checked = enabled;
            this.mnuWindowDimming.Text = $"Inactive transparency: {(enabled ? "On" : "Off")}";
            this.mnuWindowDimming.ShortcutKeyDisplayString = shortcut;
        }

        /// <summary>
        /// Updates title-bar visibility, opacity and the pressed-key overlay.
        /// </summary>
        /// <param name="force">Whether to apply all values even if their calculated state is unchanged.</param>
        /// <returns>True when the main window appearance changed.</returns>
        private bool UpdateWindowAppearance(bool force = false)
        {
            if (GlobalSettings.Settings == null || this.IsDisposed) return false;

            // Never recreate or remove the native window frame while minimized. Doing so can detach the minimized
            // window from the normal taskbar restore path. The appearance is recalculated on the first normal tick.
            if (this.WindowState != FormWindowState.Normal)
            {
                var minimizedAppearanceChanged = this.renderInactiveBackgroundOnly;
                this.renderInactiveBackgroundOnly = false;
                if (this.topMostButton.Visible)
                {
                    this.topMostButton.Visible = false;
                    minimizedAppearanceChanged = true;
                }

                this.UpdateInactiveKeyboardOverlays(false, 1D);
                this.UpdatePressedKeysOverlay(false);
                return minimizedAppearanceChanged;
            }

            var mouseInside = this.Bounds.Contains(Cursor.Position);
            var ownedDialogVisible = this.OwnedForms.Any(f => !this.IsAppearanceOverlay(f) && f.Visible);
            var interactionActive = mouseInside
                || this.MainMenu.Visible
                || this.menuOpen
                || this.mnuToggleEditMode.Checked
                || ownedDialogVisible;
            var dimEnabled = GlobalSettings.Settings.DimInactiveWindow;
            var inactive = dimEnabled && !interactionActive;
            var inputActive = this.HasActiveVisualInput();
            var allKeysOpaque = GlobalSettings.Settings.MakeAllKeysOpaqueOnPress && inputActive;
            var titleBarVisible = !dimEnabled || interactionActive;
            var targetBackgroundOpacity = inactive
                ? GlobalSettings.Settings.InactiveOpacityPercent / 100D
                : 1D;
            var targetKeyOpacity = allKeysOpaque
                ? 1D
                : GlobalSettings.Settings.InactiveKeyOpacityPercent / 100D;
            var appearanceChanged = false;

            var targetBorder = titleBarVisible
                ? this.mnuToggleEditMode.Checked ? FormBorderStyle.Sizable : FormBorderStyle.FixedSingle
                : FormBorderStyle.None;
            if (force || this.FormBorderStyle != targetBorder)
            {
                var clientScreenLocation = this.PointToScreen(Point.Empty);
                var clientSize = this.ClientSize;
                this.changingWindowFrame = true;
                try
                {
                    this.FormBorderStyle = targetBorder;
                    this.ClientSize = clientSize;

                    // Preserve the keyboard content position instead of the outer window position. Removing the
                    // title bar otherwise shifts the client area left and up by the former frame thickness.
                    var newClientScreenLocation = this.PointToScreen(Point.Empty);
                    this.Location = new Point(
                        this.Location.X + clientScreenLocation.X - newClientScreenLocation.X,
                        this.Location.Y + clientScreenLocation.Y - newClientScreenLocation.Y);
                }
                finally
                {
                    this.changingWindowFrame = false;
                }

                appearanceChanged = true;
            }

            if (force || Math.Abs(this.Opacity - targetBackgroundOpacity) > 0.001D)
            {
                this.Opacity = targetBackgroundOpacity;
                appearanceChanged = true;
            }

            if (force || this.renderInactiveBackgroundOnly != inactive)
            {
                this.renderInactiveBackgroundOnly = inactive;
                appearanceChanged = true;
            }

            var pinVisible = titleBarVisible && mouseInside && !ownedDialogVisible;
            if (this.topMostButton.Visible != pinVisible)
            {
                this.topMostButton.Visible = pinVisible;
                appearanceChanged = true;
            }
            if (pinVisible) this.topMostButton.BringToFront();

            this.UpdateInactiveKeyboardOverlays(inactive, targetKeyOpacity);

            var showPressedKeysOverlay = inactive
                && !GlobalSettings.Settings.MakeAllKeysOpaqueOnPress
                && inputActive;
            this.UpdatePressedKeysOverlay(showPressedKeysOverlay);

            return appearanceChanged;
        }

        /// <summary>
        /// Returns whether a keyboard key, mouse button or scroll indicator is physically active.
        /// </summary>
        private bool HasActiveVisualInput()
        {
            return KeyboardState.PhysicallyPressedKeys.Any()
                || MouseState.ActiveKeys.Any()
                || MouseState.ScrollCounts.Any(count => count > 0);
        }

        /// <summary>
        /// Draws every key cap in the independently translucent inactive key layer.
        /// </summary>
        private void RenderInactiveKeyCaps(Graphics graphics)
        {
            this.RenderInactiveKeyboardLayer(graphics, false);
        }

        /// <summary>
        /// Draws every key label in a fully opaque layer above the translucent key caps.
        /// </summary>
        private void RenderInactiveKeyText(Graphics graphics)
        {
            this.RenderInactiveKeyboardLayer(graphics, true);
        }

        /// <summary>
        /// Draws either key caps or labels for the inactive keyboard layers.
        /// </summary>
        private void RenderInactiveKeyboardLayer(Graphics graphics, bool textOnly)
        {
            if (GlobalSettings.CurrentDefinition == null) return;

            KeyboardState.CheckKeyHolds(
                GlobalSettings.Settings.PressHold,
                GlobalSettings.Settings.FadeKeyPresses);
            MouseState.CheckKeyHolds(
                GlobalSettings.Settings.PressHold,
                GlobalSettings.Settings.FadeKeyPresses);
            MouseState.CheckScrollAndMovement();

            var keyboardKeys = KeyboardState.PressedKeys;
            var mouseKeys = MouseState.PressedKeys.Select(k => (int)k).ToList();
            var scrollCounts = MouseState.ScrollCounts;
            var allDefinitions = GlobalSettings.CurrentDefinition.Elements;

            foreach (var definition in allDefinitions)
            {
                if (definition is KeyboardKeyDefinition keyboardDefinition)
                {
                    var opacity = this.GetKeyboardDefinitionPressOpacity(
                        keyboardDefinition,
                        allDefinitions,
                        keyboardKeys);
                    var keyActive = opacity > 0;
                    var shiftedAtPress = this.WasPressedWithShift(keyboardDefinition);
                    var retainedShiftState = !KeyboardState.ShiftDown && shiftedAtPress;
                    var useShiftText = KeyboardState.ShiftDown || retainedShiftState;
                    if (textOnly)
                        keyboardDefinition.RenderText(
                            graphics,
                            opacity,
                            useShiftText,
                            KeyboardState.CapsActive,
                            Color.Black,
                            keyActive,
                            shiftedAtPress);
                    else
                    {
                        // Draw a complete loose cap first so the fading white highlight blends with the cap instead
                        // of the overlay's magenta transparency key.
                        keyboardDefinition.RenderKeyCap(
                            graphics,
                            0,
                            useShiftText,
                            KeyboardState.CapsActive);
                        if (opacity > 0)
                            DrawWhiteKeyHighlight(graphics, keyboardDefinition, opacity);
                    }
                }
                else if (definition is MouseKeyDefinition mouseDefinition)
                {
                    var keyCode = mouseDefinition.KeyCodes.Single();
                    var opacity = mouseKeys.Contains(keyCode)
                        ? MouseState.GetKeyPressOpacity(
                            (MouseKeyCode)keyCode,
                            GlobalSettings.Settings.PressHold,
                            GlobalSettings.Settings.FadeKeyPresses)
                        : 0;
                    if (textOnly)
                        mouseDefinition.RenderText(
                            graphics,
                            opacity,
                            KeyboardState.ShiftDown,
                            KeyboardState.CapsActive,
                            Color.Black);
                    else
                    {
                        mouseDefinition.RenderKeyCap(
                            graphics,
                            0,
                            KeyboardState.ShiftDown,
                            KeyboardState.CapsActive);
                        if (opacity > 0)
                            DrawWhiteKeyHighlight(graphics, mouseDefinition, opacity);
                    }
                }
                else if (definition is MouseScrollDefinition scrollDefinition)
                {
                    var scrollCount = scrollCounts[scrollDefinition.KeyCodes.Single()];
                    if (textOnly)
                        scrollDefinition.RenderText(graphics, scrollCount, Color.Black);
                    else
                        scrollDefinition.RenderKeyCap(graphics, scrollCount);
                }
                else if (!textOnly && definition is MouseSpeedIndicatorDefinition speedDefinition)
                {
                    speedDefinition.Render(graphics, MouseState.AverageSpeed);
                }
            }
        }

        /// <summary>
        /// Gets a keyboard definition's current pressed-state opacity, including multi-key definitions and fading.
        /// </summary>
        private float GetKeyboardDefinitionPressOpacity(
            KeyboardKeyDefinition definition,
            System.Collections.Generic.List<ElementDefinition> allDefinitions,
            System.Collections.Generic.IReadOnlyList<int> keyboardKeys)
        {
            if (!definition.KeyCodes.Any() || !definition.KeyCodes.All(keyboardKeys.Contains)) return 0;

            if (definition.KeyCodes.Count == 1
                && allDefinitions.OfType<KeyboardKeyDefinition>()
                    .Any(d => d.KeyCodes.Count > 1
                        && d.KeyCodes.All(keyboardKeys.Contains)
                        && d.KeyCodes.ContainsAll(definition.KeyCodes)))
            {
                return 0;
            }

            return definition.KeyCodes.Min(k => KeyboardState.GetKeyPressOpacity(
                k,
                GlobalSettings.Settings.PressHold,
                GlobalSettings.Settings.FadeKeyPresses));
        }

        /// <summary>
        /// Returns whether the current press of a key started while Shift was physically held.
        /// </summary>
        private bool WasPressedWithShift(KeyboardKeyDefinition definition)
        {
            return !string.Equals(definition.Text, definition.ShiftText, StringComparison.Ordinal)
                && definition.KeyCodes.Any(KeyboardState.IsShiftStateKey);
        }

        /// <summary>
        /// Draws a white key highlight at the requested opacity over an already rendered loose key cap.
        /// </summary>
        private static void DrawWhiteKeyHighlight(Graphics graphics, KeyDefinition definition, float opacity)
        {
            opacity = Math.Max(0, Math.Min(1, opacity));
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * opacity), Color.White)))
                graphics.FillPolygon(brush, definition.Boundaries.ConvertAll<Point>(point => point).ToArray());
        }

        /// <summary>
        /// Draws only physically active keys for the fully opaque overlay.
        /// </summary>
        private void RenderOpaquePressedKeys(Graphics graphics)
        {
            if (GlobalSettings.CurrentDefinition == null) return;

            var keyboardKeys = KeyboardState.PhysicallyPressedKeys;
            var mouseKeys = MouseState.ActiveKeys.Select(k => (int)k).ToList();
            var scrollCounts = MouseState.ScrollCounts;
            var allDefinitions = GlobalSettings.CurrentDefinition.Elements;

            foreach (var definition in allDefinitions)
            {
                if (definition is KeyboardKeyDefinition keyboardDefinition)
                {
                    var opacity = this.GetKeyboardDefinitionPressOpacity(
                        keyboardDefinition,
                        allDefinitions,
                        keyboardKeys);
                    if (opacity <= 0) continue;

                    var shiftedAtPress = this.WasPressedWithShift(keyboardDefinition);
                    var retainedShiftState = !KeyboardState.ShiftDown && shiftedAtPress;
                    DrawWhiteKeyHighlight(graphics, keyboardDefinition, 1);
                    keyboardDefinition.RenderText(
                        graphics,
                        1,
                        KeyboardState.ShiftDown || retainedShiftState,
                        KeyboardState.CapsActive,
                        Color.Black,
                        true,
                        shiftedAtPress);
                }
                else if (definition is MouseKeyDefinition mouseDefinition
                    && mouseKeys.Contains(mouseDefinition.KeyCodes.Single()))
                {
                    DrawWhiteKeyHighlight(graphics, mouseDefinition, 1);
                    mouseDefinition.RenderText(
                        graphics,
                        1,
                        KeyboardState.ShiftDown,
                        KeyboardState.CapsActive,
                        Color.Black);
                }
                else if (definition is MouseScrollDefinition scrollDefinition)
                {
                    var scrollCount = scrollCounts[scrollDefinition.KeyCodes.Single()];
                    if (scrollCount <= 0) continue;

                    DrawWhiteKeyHighlight(graphics, scrollDefinition, 1);
                    scrollDefinition.RenderText(graphics, scrollCount, Color.Black);
                }
            }
        }

        /// <summary>
        /// Shows, hides and refreshes the opaque pressed-key overlay.
        /// </summary>
        private void UpdatePressedKeysOverlay(bool visible)
        {
            if (!visible || this.WindowState != FormWindowState.Normal || !this.Visible)
            {
                if (this.pressedKeysOverlay.Visible) this.pressedKeysOverlay.Hide();
                return;
            }

            this.SyncAppearanceOverlays();
            if (!this.pressedKeysOverlay.Visible)
                this.pressedKeysOverlay.Show(this);

            this.pressedKeysOverlay.Refresh();
            this.pressedKeysOverlay.BringToFront();
        }

        /// <summary>
        /// Shows, hides and refreshes the inactive key-cap and label layers.
        /// </summary>
        private void UpdateInactiveKeyboardOverlays(bool visible, double keyOpacity)
        {
            if (!visible || this.WindowState != FormWindowState.Normal || !this.Visible)
            {
                if (this.inactiveKeyCapsOverlay.Visible) this.inactiveKeyCapsOverlay.Hide();
                if (this.inactiveKeyTextOverlay.Visible) this.inactiveKeyTextOverlay.Hide();
                return;
            }

            this.SyncAppearanceOverlays();
            this.inactiveKeyCapsOverlay.Opacity = keyOpacity;

            if (!this.inactiveKeyCapsOverlay.Visible)
                this.inactiveKeyCapsOverlay.Show(this);
            if (!this.inactiveKeyTextOverlay.Visible)
                this.inactiveKeyTextOverlay.Show(this);

            this.inactiveKeyCapsOverlay.Refresh();
            this.inactiveKeyTextOverlay.Refresh();
            this.inactiveKeyTextOverlay.BringToFront();
        }

        /// <summary>
        /// Aligns all appearance overlays with the main window's client area.
        /// </summary>
        private void SyncAppearanceOverlays()
        {
            var bounds = new Rectangle(this.PointToScreen(Point.Empty), this.ClientSize);
            foreach (var overlay in new[]
            {
                this.inactiveKeyCapsOverlay,
                this.inactiveKeyTextOverlay,
                this.pressedKeysOverlay
            })
            {
                if (overlay == null || overlay.IsDisposed) continue;

                overlay.TopMost = this.TopMost;
                overlay.Bounds = bounds;
            }
        }

        /// <summary>
        /// Returns whether a form belongs to the inactive appearance layer stack.
        /// </summary>
        private bool IsAppearanceOverlay(Form form)
        {
            return form == this.inactiveKeyCapsOverlay
                || form == this.inactiveKeyTextOverlay
                || form == this.pressedKeysOverlay;
        }

        /// <summary>
        /// Releases resources owned by the window appearance feature.
        /// </summary>
        private void DisposeAppearanceControls()
        {
            foreach (var overlay in new[]
            {
                this.inactiveKeyCapsOverlay,
                this.inactiveKeyTextOverlay,
                this.pressedKeysOverlay
            })
            {
                if (overlay == null) continue;

                overlay.Close();
                overlay.Dispose();
            }

            this.inactiveKeyCapsOverlay = null;
            this.inactiveKeyTextOverlay = null;
            this.pressedKeysOverlay = null;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);

        /// <summary>
        /// A click-through, non-activating color-keyed window used for an independently translucent render layer.
        /// </summary>
        private sealed class KeyboardOverlayForm : Form
        {
            private const int WmNcHitTest = 0x0084;
            private const int HtTransparent = -1;
            private const int WsExTransparent = 0x00000020;
            private const int WsExToolWindow = 0x00000080;
            private const int WsExNoActivate = 0x08000000;
            private static readonly Color TransparentColor = Color.FromArgb(255, 0, 255);
            private readonly Action<Graphics> render;

            /// <summary>
            /// Initializes the overlay with the supplied render action.
            /// </summary>
            public KeyboardOverlayForm(Action<Graphics> render)
            {
                this.render = render;
                this.BackColor = TransparentColor;
                this.TransparencyKey = TransparentColor;
                this.FormBorderStyle = FormBorderStyle.None;
                this.ShowInTaskbar = false;
                this.StartPosition = FormStartPosition.Manual;
                this.SetStyle(
                    ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.UserPaint,
                    true);
            }

            /// <summary>
            /// Gets extended window styles that prevent activation and mouse interception.
            /// </summary>
            protected override CreateParams CreateParams
            {
                get
                {
                    var parameters = base.CreateParams;
                    parameters.ExStyle |= WsExTransparent | WsExToolWindow | WsExNoActivate;
                    return parameters;
                }
            }

            /// <summary>
            /// Prevents the overlay from activating when shown.
            /// </summary>
            protected override bool ShowWithoutActivation => true;

            /// <summary>
            /// Draws the active keys over the transparent color key.
            /// </summary>
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(TransparentColor);
                e.Graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                this.render(e.Graphics);
                base.OnPaint(e);
            }

            /// <summary>
            /// Makes every part of the overlay transparent to mouse hit testing.
            /// </summary>
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WmNcHitTest)
                {
                    m.Result = (IntPtr)HtTransparent;
                    return;
                }

                base.WndProc(ref m);
            }
        }
    }
}
