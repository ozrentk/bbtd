using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BBTDApp.NLogEx
{
    public static class LogLevel
    {
        public static readonly int Trace = 0;
        public static readonly int Debug = 1;
        public static readonly int Info = 2;
        public static readonly int Warn = 3;
        public static readonly int Error = 4;
        public static readonly int Fatal = 5;
        public static readonly int Off = 6;
    }
}