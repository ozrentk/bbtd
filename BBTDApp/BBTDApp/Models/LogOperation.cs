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

namespace BBTDApp.Models
{
    public static class LogOperation
    {
        public static readonly string IMG_REQ_RECV = "IMG_REQ_RECV";
        public static readonly string IMG_GEN = "IMG_GEN";
        public static readonly string BC_DATA_RECV = "BC_DATA_RECV";
        public static readonly string BC_NOTIFY = "BC_NOTIFY";
        public static readonly string UI_BC_NOTIFIED = "UI_BC_NOTIFIED";
        public static readonly string LOAD_TIMEOUT = "LOAD_TIMEOUT";
        public static readonly string IMG_REQ = "IMG_REQ";
        public static readonly string IMG_RECV = "IMG_RECV";
        public static readonly string BC_READ = "BC_READ";
        public static readonly string BC_SENT = "BC_SENT";
        public static readonly string APP_OFFSET = "APP_OFFSET";
    }
}