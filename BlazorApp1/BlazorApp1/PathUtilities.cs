using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BlazorApp1
{
    public static class PathUtilities
    {
        #region Constants

        public const string AppFiles = "AppFiles";
        public const string ReportError = "ReportError";
        public const string ReportTemp = "ReportTemp";
        public const string ReportScript = "ReportScript";
        public const string Temp = "Temp";
        public const string Bin = "bin";
        public const string ViewSettings = "ViewSettings";
        public const string Reports = "Reports";

        #endregion

        #region Properties

        public static string InstallPath
        {
            get
            {
                return GetInstallPath ();
            }
        }


        static object _classLock = new object ();
        static object ClassLock
        {
            get { return _classLock; }
        }

        #endregion

        #region Methods


        private static string GetPath ( string basePath, string extPath )
        {
            string path = Path.Combine ( basePath, extPath );

            if ( !Directory.Exists ( path ) )
                Directory.CreateDirectory ( path );

            return path;
        }

        public static string GetInstallPath ()
        {
            string basePath = null;

            var assembly = System.Reflection.Assembly.GetExecutingAssembly ();
            if ( assembly != null )
            {
                basePath = Path.GetDirectoryName ( assembly.Location );
            }
            else
                basePath = Directory.GetCurrentDirectory ();


            int pos = -1;
            if ( basePath.EndsWith ( "\\" ) )
            {
                pos = basePath.LastIndexOf ( "\\" );
                if ( pos > -1 )
                {
                    basePath = basePath.Substring ( 0, pos );
                }
            }

            // we are case sensitive on directory so just in case
            // use all lower
            if ( basePath.Contains ( "Bin" ) )
                basePath.Replace ( "Bin", "bin" );


            return basePath;
        }

        public static string GetBasePath ()
        {
            // get path to database            
            string dir = GetInstallPath ();
            string [] endings = { @"bin\Debug", @"bin\Release", @"bin" };

            foreach ( var ending in endings )
            {
                if ( dir.EndsWith ( ending ) )
                {
                    dir = dir.Replace ( ending, null );
                    break;
                }
            }

            return dir;
        }

        #endregion
    }
}
