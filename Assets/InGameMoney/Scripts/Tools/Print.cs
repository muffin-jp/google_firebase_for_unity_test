using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace InGameMoney
{
    public class Print
    {
        private static string Green(string message)
        {
            return "<color=#BFFF00>" + message + "</color>";
        }

        private static string Blue(string message)
        {
            return "<color=#339CFF>" + message + "</color>";
        }

        private static string Red(string message)
        {
            return "<color=#EA3D55>" + message + "</color>";
        }

        private static string Pink(string message)
        {
            return "<color=#D16587>" + message + "</color>";
        }

        [Conditional("DEBUG")]
        public static void GreenLog(string text)
        {
            Debug.Log(Green(text));
        }

        [Conditional("DEBUG")]
        public static void BlueLog(string text)
        {
            Debug.Log(Blue(text));
        }

        [Conditional("DEBUG")]
        public static void RedLog(string text)
        {
            Debug.Log(Red(text));
        }

        [Conditional("DEBUG")]
        public static void PinkLog(string text)
        {
            Debug.Log(Pink(text));
        }
    }
}
