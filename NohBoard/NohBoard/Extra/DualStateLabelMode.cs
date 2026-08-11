/*
Copyright (C) 2016 by Eric Bataille <e.c.p.bataille@gmail.com>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.
*/

namespace ThoNohT.NohBoard.Extra
{
    /// <summary>
    /// Lists the ways keys with different normal and Shift labels can be displayed.
    /// </summary>
    public enum DualStateLabelMode
    {
        /// <summary>
        /// Show both labels on idle non-letter keys like a physical keyboard cap, then keep only the activated label
        /// in its original position while the key is pressed or fading.
        /// </summary>
        KeyboardKeyCaps,

        /// <summary>
        /// Preserve the original NohBoard behavior: show one centered label and switch it with Shift.
        /// </summary>
        OriginalNohBoard
    }
}
