using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ETWControler.Hooking
{
    class Hooker : IDisposable
    {
        int MouseHookHandle;
        int KeyboardHookHandle;
        HookProc MouseHookGCRootedDelegate;
        HookProc KeyboardHookGCRootedDelegate;

        public event OnMouseWheelDelegate OnMouseWheel;
        public event OnMouseMoveDelegate OnMouseMove;
        public event OnMouseButtonDelegate OnMouseButton;
        public event OnKeyDownDelegate OnKeyDown;

        public bool IsMouseHooked
        {
            get
            {
                return MouseHookHandle != 0;
            }

            set
            {
                HookMouse(value);
            }
        }

        public bool IsKeyboardHooked
        {
            get
            {
                return KeyboardHookHandle != 0;
            }

            set
            {
                HookKeyboard(value);
            }
        }

        public Hooker()
        {
           // HookEvents.RegisterItself();
            MouseHookGCRootedDelegate = MouseHook;
            KeyboardHookGCRootedDelegate = KeyboardHook;
        }

        void HookKeyboard(bool bHook)
        {
            if (KeyboardHookHandle == 0 && bHook)
            {
                using (var mainMod = Process.GetCurrentProcess().MainModule)
                    KeyboardHookHandle = HookNativeDefinitions.SetWindowsHookEx(HookNativeDefinitions.WH_KEYBOARD_LL, KeyboardHookGCRootedDelegate, HookNativeDefinitions.GetModuleHandle(mainMod.ModuleName), 0);

                //If the SetWindowsHookEx function fails.
                if (KeyboardHookHandle == 0)
                {
                    System.Windows.MessageBox.Show("SetWindowsHookEx Failed " + new Win32Exception(Marshal.GetLastWin32Error()));
                    return;
                }
            }
            else if(bHook == false)
            {
                Debug.Print("Unhook keyboard");
                HookNativeDefinitions.UnhookWindowsHookEx(KeyboardHookHandle);
                KeyboardHookHandle = 0;
            }

        }

        public void DisableHooks()
        {
            IsKeyboardHooked = false;
            IsMouseHooked = false;
            OnMouseButton = null;
            OnKeyDown = null;
            OnMouseWheel = null;
            OnMouseMove = null;
        }

        public void EnableHooks()
        {
            IsKeyboardHooked = true;
            IsMouseHooked = true;
        }

        private void HookMouse(bool bHook)
        {
            if (MouseHookHandle == 0 && bHook)
            {
                using (var mainMod = Process.GetCurrentProcess().MainModule)
                    MouseHookHandle = HookNativeDefinitions.SetWindowsHookEx(HookNativeDefinitions.WH_MOUSE_LL, MouseHookGCRootedDelegate, HookNativeDefinitions.GetModuleHandle(mainMod.ModuleName), 0);
                //If the SetWindowsHookEx function fails.
                if (MouseHookHandle == 0)
                {
                    System.Windows.MessageBox.Show("SetWindowsHookEx Failed " + new Win32Exception(Marshal.GetLastWin32Error()));
                    return;
                }
            }
            else if( bHook == false)
            {
                Debug.Print("Unhook mouse");
                HookNativeDefinitions.UnhookWindowsHookEx(MouseHookHandle);
                MouseHookHandle = 0;
            }
        }


        public int KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
           //     Debug.Print("KeyboardHook called");
                var keyboardData = (HookNativeDefinitions.KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(HookNativeDefinitions.KeyboardHookStruct));
                unchecked
                {
                    // wParam is WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP
                    int wInt = wParam.ToInt32();
                    var key = KeyInterop.KeyFromVirtualKey((int)keyboardData.vkCode);
                    if (wInt == WM.KEYDOWN || wInt == WM.SYSKEYDOWN && OnKeyDown != null)
                    {
                        if (OnKeyDown != null)
                        {
                            OnKeyDown(key);
                        }
                    }
                    else
                    {

                    }
                }

            }

            return HookNativeDefinitions.CallNextHookEx(MouseHookHandle, nCode, wParam, lParam);
        }


        public int MouseHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
            //    Debug.Print("MouseHook called");
                unchecked
                {
                    int wParami = wParam.ToInt32();

                    //Marshal the data from the callback.
                    var mouseData = (HookNativeDefinitions.MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(HookNativeDefinitions.MouseHookStruct));
                    var mouseButton = WM.GetMouseButton(wParam);
                    int wheelDelta = 0;
                    if (mouseButton == MouseButton.None && wParami == WM.MOUSEWHEEL)
                    {
                        wheelDelta = WM.GetWheelDDelta(mouseData.mouseData);
                    }

                    if (wParami == WM.MOUSEMOVE)
                    {
                        if (OnMouseMove != null)
                        {
                            OnMouseMove(mouseData.pt.x, mouseData.pt.y);
                        }
                        else
                        {
                            // mouse move is disabled ignore event
                        }
                    }
                    else if (wParami == WM.MOUSEWHEEL && OnMouseWheel != null)
                    {
                        if (OnMouseWheel != null)
                        {
                            OnMouseWheel(wheelDelta, mouseData.pt.x, mouseData.pt.y);
                        }
                    }
                    else if( OnMouseButton != null)
                    {
                        if (OnMouseButton != null)
                        {
                            OnMouseButton(mouseButton, mouseData.pt.x, mouseData.pt.y);
                        }
                    }
                }
            }

            return HookNativeDefinitions.CallNextHookEx(MouseHookHandle, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (MouseHookHandle != 0)
            {
                Debug.Print("Unhook mouse");
                HookNativeDefinitions.UnhookWindowsHookEx(MouseHookHandle);
                MouseHookHandle = 0;
            }

            if (KeyboardHookHandle != 0)
            {
                Debug.Print("Unhook Keyboard");
                HookNativeDefinitions.UnhookWindowsHookEx(KeyboardHookHandle);
                KeyboardHookHandle = 0;
            }
        }
    }

    delegate void OnMouseWheelDelegate(int wheelDetla, int x, int y);
    delegate void OnMouseMoveDelegate(int x, int y);
    delegate void OnMouseButtonDelegate(MouseButton button, int x, int y);
    delegate void OnKeyDownDelegate(Key key);
}
