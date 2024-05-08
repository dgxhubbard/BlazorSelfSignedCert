using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace GenerateCert
{
    /// <summary>
    /// Refer to:
    /// https://www.humankode.com/asp-net-core/develop-locally-with-https-self-signed-certificates-and-asp-net-core/
    /// https://stackoverflow.com/questions/42786986/how-to-create-a-valid-self-signed-x509certificate2-programmatically-not-loadin/50138133#50138133
    /// https://learn.microsoft.com/en-us/powershell/module/pki/new-selfsignedcertificate?view=windowsserver2022-ps
    /// https://www.codeproject.com/Articles/5315010/How-to-Use-Certificates-in-ASP-NET-Core
    /// https://medium.com/@niteshsinghal85/certificate-based-authentication-in-asp-net-core-web-api-aad37a33d448
    /// 
    /// </summary>

    internal class Program
	{
        #region Constants

        public const string Ext = ".pfx";
        public const string DefaultFriendlyName = "AAA My Cert";
        public const string DefaultCnName = "AAA My Cert";
        public const string DefaultPassword = "1234";
        public const int DefaultYears = 10;
        public const string DefaultFileName = "aaacert";


        #endregion

        #region Properties


        public static string _fileName = "";
        public static string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
            }
        }

        public static string _friendlyName = "";
        public static string FriendlyName
        {
            get { return _friendlyName; }
            set
            {
                _friendlyName = value;
            }
        }



        public static string _cnName = DefaultCnName;
        public static string CnName
        {
            get { return _cnName; }
            set
            {
                _cnName = value;
            }
        }

        public static string _password = DefaultPassword;
        public static string Password
        {
            get { return _password; }
            set
            {
                _password = value;
            }
        }

        public static int _years = DefaultYears;
        public static int Years
        {
            get { return _years; }
            set
            {
                _years = value;
            }
        }





        #endregion




        static void Main ( string [] arguments )
		{
            var args = arguments.ToList ();

            args.Reverse ();


            var argStk = new Stack<string> ();

            args.ForEach ( l => argStk.Push ( l ) );

            var done = false;


            while ( !done )           
            {

                if ( argStk.Count == 0 )
                    break;

                var arg = argStk.Pop ();
                if ( string.IsNullOrEmpty ( arg ) )
                    continue;
            
                arg = arg.Replace ( "-", string.Empty );
                if ( string.IsNullOrEmpty ( arg ) )
                    continue;

                string tmp;
                switch ( arg ) 
                {

                    case "?":

                        ShowArgs ();
                        Environment.Exit ( 0 );
                        

                        break;

                    case "file":

                        tmp = argStk.Pop ().Trim ();
                        if ( !string.IsNullOrEmpty ( tmp ) )
                        {
                            if ( !tmp.Contains ( "." ) )
                                FileName = tmp;
                            else
                            {
                                Console.WriteLine ( "File cannot contain extension" );
                                ShowArgs ();
                                Environment.Exit ( 0 );
                            }
                        }

                        break;

                    case "fn":

                        tmp = argStk.Pop ().Trim ();
                        if ( !string.IsNullOrEmpty ( tmp ) )
                            FriendlyName = tmp;
                        break;

                    case "cn":

                        tmp = argStk.Pop ().Trim ();
                        if ( !string.IsNullOrEmpty ( tmp ) )
                            CnName = tmp;
                        break;

                    case "pw":

                        tmp = argStk.Pop ().Trim ();
                        if ( !string.IsNullOrEmpty ( tmp ) )
                            Password = tmp;
                        break;


                    case "y":

                        tmp = argStk.Pop ().Trim ();
                        if ( !string.IsNullOrEmpty ( tmp ) )
                        {
                            var years = DefaultYears;
                            if ( int.TryParse ( tmp, out years ) )
                            {
                                if ( years > 0 )
                                    Years = years;
                            }


                        }
                            
                        break;



                }

            }

            
			var cert = CreateCertificate ();

			// Create PFX (PKCS #12) with private key
			File.WriteAllBytes ( FileName + ".pfx", cert.Export ( X509ContentType.Pfx, Password ) );


			// Create Base 64 encoded CER (public key only)
			File.WriteAllText ( 

				FileName + ".cer",
				"-----BEGIN CERTIFICATE-----\r\n"
				+ Convert.ToBase64String ( cert.Export ( X509ContentType.Cert ), Base64FormattingOptions.InsertLineBreaks )
				+ "\r\n-----END CERTIFICATE-----" );

            Console.WriteLine ( "Press any key to exit..." );
            Console.ReadKey ();
        }


        private static void ShowArgs ()
        {

            Console.WriteLine ( "-file" + " file name, this can be just cert name or directory and cert name \"C\\MyDir\\MyCert\" (NO extension with cert name)" );
            Console.WriteLine ( "-fn  " + " friendly name" );
            Console.WriteLine ( "-cn  " + " common name" );
            Console.WriteLine ( "-pw  " + " password" );
            Console.WriteLine ( "-y   " + " how many years certificate is valid for" );

        }



        private static X509Certificate2 CreateCertificate ()
        {
            using var algorithm = RSA.Create ( keySizeInBits: 2048 );

            var subject = new X500DistinguishedName ( "CN=" + CnName );

            var sanBuilder = new SubjectAlternativeNameBuilder ();

            sanBuilder.AddIpAddress ( IPAddress.Loopback );
            sanBuilder.AddIpAddress ( IPAddress.IPv6Loopback );
            sanBuilder.AddDnsName ( "localhost" );
            sanBuilder.AddDnsName ( Environment.MachineName );



            var request = 
                new CertificateRequest ( subject, algorithm, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1 );


            request.CertificateExtensions.Add ( sanBuilder.Build () );


            request.CertificateExtensions.Add (
                new X509KeyUsageExtension (
                        X509KeyUsageFlags.DigitalSignature |
                        X509KeyUsageFlags.DataEncipherment |
                        X509KeyUsageFlags.KeyEncipherment,
                    critical: true ) );

            var certificate = request.CreateSelfSigned ( DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears ( Years ) );


            certificate = RSACertificateExtensions.CopyWithPrivateKey ( certificate, algorithm );


            bool isWindows = System.Runtime.InteropServices.RuntimeInformation
                          .IsOSPlatform ( OSPlatform.Windows );

            if ( isWindows )
                certificate.FriendlyName = FriendlyName;


            return certificate;
        }



	}

}
