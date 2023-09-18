using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using BBTDApp.Services;
//using BBTDApp.Services.App.Droid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

//[assembly: Dependency(typeof(ExternalStorage))]
namespace BBTDApp.Services
{
    public interface IExternalStorage
    {
        string GetPath();
    }

    public class ExternalStorage : IExternalStorage
    {
        public string GetPath()
        {
           Context context = Android.App.Application.Context;
           var filePath = context.GetExternalFilesDir("");
           return filePath.Path;
        }
    }
}