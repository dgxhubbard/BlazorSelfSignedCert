
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorApp1
{
	public  class Ports
	{
        #region Constants

        public const string ApiPortsFilename = "apiports.json";
        public const string IdpPortsFilename = "idpports.json";


		#endregion

		#region Properties


		int _port;
		public int Port
		{  
			get
			{
				return _port;
			}
			set
			{
                _port = value;
			}
		}


		#endregion


		#region Methods

		public static bool Exists ( string filepath )
		{
			var exists = false;

			if ( string.IsNullOrEmpty ( filepath ) )
				throw new ArgumentNullException ( "filepath required" );

			var path = Path.GetDirectoryName ( filepath );
			if ( !Directory.Exists ( path ) )
				throw new InvalidOperationException ( "filepath does not exist " + filepath );

			if ( File.Exists ( filepath ) )
				exists = true;

			return exists;

		}

		public static Ports LoadPorts ( string filepath )
		{

			if ( string.IsNullOrEmpty ( filepath ) )
				throw new ArgumentNullException ( "filepath required" );

            if ( !File.Exists ( filepath ) )
                throw new InvalidOperationException ( "ports file does not exist filepath" );

            var fi = new FileInfo ( filepath );
            if ( fi.Length == 0 )
                throw new InvalidOperationException ( "certificates file is empty" );

            // not working in release build installation
            //var json = File.ReadAllText ( filepath );

            string json = " ";
            using ( var reader = new StreamReader ( filepath ) )
            {
                json = reader.ReadToEnd ();
            }

            // installer file copy is adding ","
            // which breaks deserialize
            json = json.Replace ( ",", string.Empty );

            var resp = new Ports ();
			resp = JsonSerializer.Deserialize<Ports> ( json );


			return resp;

		}

        public static string GetBasePath ()
        {
            var basePath = PathUtilities.GetInstallPath ();

            var pos = basePath.IndexOf ( "ApiSamples" );
            if ( pos != -1 )
            {
                basePath = basePath.Substring ( 0, pos );

                basePath = Path.Combine ( basePath, "Ports" );
            }


            pos = basePath.IndexOf ( "bin" );
            if ( pos != -1 )
            {
                basePath = basePath.Substring ( 0, pos );

                if ( basePath.Contains ( "Program Files" ) )
                    basePath = Path.Combine ( basePath, "Ports" );
                else
                    basePath = Path.Combine ( basePath, @"Ports" );

            }

            return basePath;
        }

        public static string GetPortsPath ()
        {
            return GetBasePath ();
        }


        public static string GetIdpPortsFilePath ()
        {
            var portsPath = GetPortsPath ();
            
            var idpCertsPath = Path.Combine ( portsPath, IdpPortsFilename );

            Debug.WriteLine ( "Idp Ports Path " + idpCertsPath );


            return idpCertsPath;

        }

        public static string GetApiPortsFilePath ()
        {
            var portsPath = GetPortsPath ();

            var apiCertsPath = Path.Combine ( portsPath, ApiPortsFilename );

            Debug.WriteLine ( "Api Ports Path " + apiCertsPath );


            return apiCertsPath;



        }




        #endregion





    }
}
