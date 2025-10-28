using System.DirectoryServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MiniVNCClient.WPFExample
{
    /// <summary>
    /// Borrowed from https://stackoverflow.com/a/5826175 and https://stackoverflow.com/a/37909846
    /// </summary>
    internal partial class KeyboardHelper
    {
        /// <summary>
        /// The translation to be performed. The value of this parameter depends on the value of the <i>uCode</i> parameter.
        /// </summary>
        public enum MapType : uint
        {
            /// <summary>
            /// The <i>uCode</i> parameter is a virtual-key code and is translated into a scan code. If it is a virtual-key code that does
            /// not distinguish between left- and right-hand keys, the left-hand scan code is returned. If there is no translation, the function returns 0.
            /// </summary>
            MAPVK_VK_TO_VSC = 0x0,

            /// <summary>
            /// <para>The <i>uCode</i> parameter is a scan code and is translated into a virtual-key code that does not distinguish between left- and right-hand keys.
            /// If there is no translation, the function returns 0.</para>
            /// <para><b>Windows Vista and later:</b> the high byte of the <i>uCode</i> value can contain either 0xe0 or 0xe1 to specify the extended scan code.</para>
            /// </summary>
            MAPVK_VSC_TO_VK = 0x1,

            /// <summary>
            /// The <i>uCode</i> parameter is a virtual-key code and is translated into an unshifted character value in the low order word of the return value.
            /// Dead keys (diacritics) are indicated by setting the top bit of the return value. If there is no translation, the function returns 0. See Remarks.
            /// </summary>
            MAPVK_VK_TO_CHAR = 0x2,

            /// <summary>
            /// <para>The <i>uCode</i> parameter is a scan code and is translated into a virtual-key code that distinguishes between left- and right-hand keys.
            /// If there is no translation, the function returns 0.</para>
            /// <para><b>Windows Vista and later:</b> the high byte of the <i>uCode</i> value can contain either 0xe0 or 0xe1 to specify the extended scan code.</para>
            /// </summary>
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        /// <summary>
        /// Translates the specified virtual-key code and keyboard state to the corresponding Unicode character or characters.
        /// </summary>
        /// <param name="wVirtKey">
        /// The virtual-key code to be translated. See
        /// <see href="https://learn.microsoft.com/en-us/windows/desktop/inputdev/virtual-key-codes">Virtual-Key Codes.</see>
        /// </param>
        /// <param name="wScanCode">
        /// The hardware <see href="https://learn.microsoft.com/en-us/windows/win32/inputdev/about-keyboard-input#scan-codes">scan code</see>
        /// of the key to be translated. The high-order bit of this value is set if the key is up.
        /// </param>
        /// <param name="lpKeyState">
        /// <para>A pointer to a 256-byte array that contains the current keyboard state. Each element (byte) in the array contains the state of one key.</para>
        /// <para>If the high-order bit of a byte is set, the key is down.The low bit, if set, indicates that the key is toggled on.In this function,
        /// only the toggle bit of the CAPS LOCK key is relevant.The toggle state of the NUM LOCK and SCROLL LOCK keys is ignored.
        /// See <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getkeyboardstate">GetKeyboardState</see> for more info.</para>
        /// </param>
        /// <param name="pwszBuff">
        /// The buffer that receives the translated character or characters as array of UTF-16 code units. This buffer may be returned
        /// without being null-terminated even though the variable name suggests that it is null-terminated. You can use the return value of this method to determine
        /// how many characters were written.
        /// </param>
        /// <param name="cchBuff">The size, in characters, of the buffer pointed to by the <i>pwszBuff</i> parameter.</param>
        /// <param name="wFlags">
        /// <para>The behavior of the function.</para>
        /// <para>If bit 0 is set, a menu is active. In this mode <b>Alt+Numeric keypad</b> key combinations are not handled.</para>
        /// </param>
        /// <returns>
        /// <para>The function returns one of the following values.</para>
        /// <para><b>value &lt; 0:</b> The specified virtual key is a dead key character (accent or diacritic). This value is returned regardless of the keyboard layout,
        /// even if several characters have been typed and are stored in the keyboard state. If possible, even with Unicode keyboard layouts, the function has written
        /// a spacing version of the dead-key character to the buffer specified by <i>pwszBuff</i>. For example, the function writes the character ACUTE ACCENT (U+00B4),
        /// rather than the character COMBINING ACUTE ACCENT (U+0301).</para>
        /// <para><b>0:</b>The specified virtual key has no translation for the current state of the keyboard. Nothing was written to the buffer specified by <i>pwszBuff</i>.</para>
        /// <para><b>value &gt; 0:</b> One or more UTF-16 code units were written to the buffer specified by <i>pwszBuff</i>. Returned <i>pwszBuff</i> may contain more characters
        /// than the return value specifies. When this happens, any extra characters are invalid and should be ignored.</para>
        /// </returns>
        [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            [In, Optional] byte[]? lpKeyState,
            [Out] char[] pwszBuff,
            int cchBuff,
            uint wFlags);

        /// <summary>
        /// Copies the status of the 256 virtual keys to the specified buffer.
        /// </summary>
        /// <param name="lpKeyState">The 256-byte array that receives the status data for each virtual key.</param>
        /// <returns>
        /// <para>If the function succeeds, the return value is nonzero.</para>
        /// <para>If the function fails, the return value is zero.To get extended error information, call
        /// <see href="https://learn.microsoft.com/en-us/windows/desktop/api/errhandlingapi/nf-errhandlingapi-getlasterror">GetLastError</see>.</para>
        /// </returns>
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetKeyboardState([Out] byte[] lpKeyState);

        /// <summary>
        /// Translates (maps) a virtual-key code into a scan code or character value, or translates a scan code into a virtual-key code.
        /// </summary>
        /// <param name="uCode">The <see href="https://learn.microsoft.com/en-us/windows/desktop/inputdev/virtual-key-codes">virtual key code</see>
        /// or scan code for a key. How this value is interpreted depends on the value of the <i>uMapType</i> parameter.</param>
        /// <param name="uMapType">The translation to be performed. The value of this parameter depends on the value of the <i>uCode</i> parameter.</param>
        /// <returns>The return value is either a scan code, a virtual-key code, or a character value, depending on the value of <i>uCode</i> and <i>uMapType</i>.
        /// If there is no translation, the return value is zero.</returns>
        [LibraryImport("user32.dll", EntryPoint = "MapVirtualKeyW")]
        private static partial uint MapVirtualKey(uint uCode, MapType uMapType);

        /// <summary>
        /// Returns the corresponding char (or chars) from the current keystroke
        /// </summary>
        /// <param name="key">The <see cref="Key"/> that was pressed or released</param>
        /// <returns>The resulting char of the current keystroke. May return more than one char, if the keyboard has dead keys and the second key does not combine with the first (for example, "~k").</returns>
        public static char[] GetCharFromKey(Key key)
        {
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);

            var result = new char[2];

            if (MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_CHAR) >> 31 == 0)
            {
                int resultSize = ToUnicode((uint)virtualKey, scanCode, keyboardState, result, result.Length, 0);

                if (resultSize > 0)
                {
                    return result[..resultSize];
                }
            }

            return [];
        }
    }
}
