using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace HP_Driver_Tool.Models
{
    [SuppressUnmanagedCodeSecurity]
    public static class ConsoleManager
    {
        private const string Kernel32_DllName = "kernel32.dll";
        private const short STD_OUTPUT_HANDLE = -11;

        [DllImport(Kernel32_DllName)]
        private static extern bool AllocConsole();

        [DllImport(Kernel32_DllName)]
        private static extern bool FreeConsole();

        [DllImport(Kernel32_DllName)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport(Kernel32_DllName)]
        private static extern int GetConsoleOutputCP();


        [DllImport(Kernel32_DllName, SetLastError = true)]
        static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, short wAttribute);
        [DllImport(Kernel32_DllName, SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        public static bool HasConsole
        {
            get { return GetConsoleWindow() != IntPtr.Zero; }
        }

        /// <summary>
        /// Creates a new console instance if the process is not attached to a console already.
        /// </summary>
        public static void Show()
        {
            //#if DEBUG
            if (!HasConsole)
            {
                AllocConsole();
                InvalidateOutAndError();
                SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), (short)10);
            }
            //#endif
        }

        /// <summary>
        /// If the process has a console attached to it, it will be detached and no longer visible. Writing to the System.Console is still possible, but no output will be shown.
        /// </summary>
        public static void Hide()
        {
            //#if DEBUG
            if (HasConsole)
            {
                SetOutAndErrorNull();
                FreeConsole();
            }
            //#endif
        }

        public static void Toggle()
        {
            if (HasConsole)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        static void InvalidateOutAndError()
        {
            Type type = typeof(Console);

            System.Reflection.FieldInfo _out = type.GetField("_out",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            System.Reflection.FieldInfo _in = type.GetField("_in",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            System.Reflection.FieldInfo _error = type.GetField("_error",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            System.Reflection.MethodInfo _InitializeStdOutError = type.GetMethod("InitializeStdOutError",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Debug.Assert(_out != null);
            Debug.Assert(_in != null);
            Debug.Assert(_error != null);

            Debug.Assert(_InitializeStdOutError != null);

            _in.SetValue(null, null);
            _out.SetValue(null, null);
            _error.SetValue(null, null);

            _InitializeStdOutError.Invoke(null, new object[] { true });
        }

        static void SetOutAndErrorNull()
        {
            Console.SetIn(TextReader.Null);
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
        }
    }
}
