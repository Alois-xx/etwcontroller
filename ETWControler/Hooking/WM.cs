using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWControler.Hooking
{
    /// <summary>
    /// Window Message Contstants
    /// </summary>
    class WM
    {
        public const int MOUSEFIRST =     0x0200;
        public const int MOUSEMOVE =      0x0200;
        public const int LBUTTONDOWN =    0x0201;
        public const int LBUTTONUP =      0x0202;
        public const int LBUTTONDBLCLK =  0x0203;
        public const int RBUTTONDOWN =    0x0204;
        public const int RBUTTONUP =      0x0205;
        public const int RBUTTONDBLCLK =  0x0206;
        public const int MBUTTONDOWN =    0x0207;
        public const int MBUTTONUP =      0x0208;
        public const int MBUTTONDBLCLK =  0x0209;
        public const int MBUTTONLEFT  =   0x020E;
        public const int MOUSEWHEEL = 0x020A;

        public const int KEYDOWN = 0x0100;
        public const int KEYUP = 0x0101;
        public const int SYSKEYDOWN = 0x0104;
        public const int SYSKEYUP = 0x0105;


        /// <summary>
        /// The return value is the high-order word representing the wheel-delta value. It indicates the distance that the wheel is rotated, 
        /// expressed in multiples or divisions of WHEEL_DELTA, which is 120. A positive value indicates that the wheel was rotated forward, 
        /// away from the user; a negative value indicates that the wheel was rotated backward, toward the user.
        /// </summary>
        /// <param name="wParam">Mouse hook Wparam value</param>
        /// <returns>Wheel delta value.</returns>
        public static Int16 GetWheelDDelta(int dwExtraInfo )
        {
            unchecked
            {
                return (Int16)((dwExtraInfo >> 16) & 0xffff);
            }
        }

        /// <summary>
        /// Get from the wParam value the pressed mouse button if any.
        /// </summary>
        /// <param name="wParam">Mouse key code</param>
        /// <returns>None if no button was pressed. Otherwise a MouseButton enum value.</returns>
        public static MouseButton GetMouseButton(IntPtr wParam)
        {
            MouseButton pressed = MouseButton.None;
            int intwParam;
            unchecked
            {
                intwParam = wParam.ToInt32();
            }

            switch (intwParam)
	        {
	            case LBUTTONDOWN:
                    pressed = MouseButton.LButtonDown;
		            break;
	            case MBUTTONDOWN:
		            pressed = MouseButton.MButtonDown;
		            break;
	            case RBUTTONDOWN:
                    pressed = MouseButton.RButtonDown;
		            break;
	            case LBUTTONUP:
                    pressed = MouseButton.LButtonUp;
		            break;
	            case MBUTTONUP:
                    pressed = MouseButton.MButtonUp;
		            break;
                case MBUTTONLEFT:
                    pressed = MouseButton.WheelLeft;
                    break;
	            case RBUTTONUP:
                    pressed = MouseButton.RButtonUp;
		            break;
	        }

            return pressed;
        }
    }
}
