using System;

namespace Shared
{
    public static class Debug
    {
        public static void Log(String message)
        {
#if WINDOWS_UWP
            System.Diagnostics.Debug.WriteLine(message);
#endif
#if ENABLE_WINMD_SUPPORT
            UnityEngine.Debug.Log(message);
#endif
        }

        public static void LogWarning(String message)
        {
#if WINDOWS_UWP
            System.Diagnostics.Debug.WriteLine(message);
#endif
#if ENABLE_WINMD_SUPPORT
            UnityEngine.Debug.LogWarning(message);
#endif
        }

        /// <summary>
        /// A variant of Debug.Log that logs an error message to the console.
        ///
        /// For Unity:
        /// When you select the message in the console a connection to the context object will be drawn.
        /// This is very useful if you want know on which object an error occurs.
        /// 
        /// When the message is a string, rich text markup can be used to add emphasis.
        /// See the manual page about rich text for details of the different markup tags available.
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        public static void LogError(String message)
        {
#if WINDOWS_UWP
            System.Diagnostics.Debug.Fail(message);
#endif
#if ENABLE_WINMD_SUPPORT
            UnityEngine.Debug.LogError(message);
#endif
        }

        /// <summary>
        /// Logs a formatted message to the Console.
        ///
        /// For formatting details, see the MSDN documentation on Composite Formatting.
        /// Rich text markup can be used to add emphasis.
        /// See the manual page about rich text for details of the different markup tags available.
        /// 
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        public static void LogFormat(string format, params object[] args)
        {
#if WINDOWS_UWP
            System.Diagnostics.Debug.WriteLine(format, args);
#endif
#if ENABLE_WINMD_SUPPORT
            UnityEngine.Debug.LogFormat(format, args);
#endif 
        }


        /// <summary>
        /// Logs a formatted error message to the Unity console.
        /// 
        /// For formatting details, see the MSDN documentation on Composite Formatting.
        /// Rich text markup can be used to add emphasis.
        /// See the manual page about rich text for details of the different markup tags available.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        public static void LogErrorFormat(string format, string args)
        {
#if WINDOWS_UWP
            System.Diagnostics.Debug.WriteLine(format, args);
#endif
#if ENABLE_WINMD_SUPPORT
            UnityEngine.Debug.LogErrorFormat(format, args);
#endif 
        }

        public static void LogException(Exception exception)
        {
#if WINDOWS_UWP
            System.Diagnostics.Debug.WriteLine(exception.Message);
#endif
#if ENABLE_WINMD_SUPPORT
            UnityEngine.Debug.LogException(exception);
#endif 
        }
    }
}
