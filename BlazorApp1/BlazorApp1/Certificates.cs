using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


using BlazorApp1.Enums;

namespace BlazorApp1
{
    public class Certificates
    {
        #region Constants

        public const string DefaultSigningCert = "gtcertserverSigning.pfx";
        public const string DefaultEncryptionCert = "gtcertserverEncryption.pfx";
        public const string DefaultPassword = "1234";

        public const string ApiCertsFilename = "apicertificates.json";
        public const string IdpCertsFilename = "idpcertificates.json";


        #endregion

        #region Definitions

        public enum WhichCert
        {
            None = 0,

            Encryption,

            Signing



        }


        #endregion

        #region Constructors

        public Certificates ()
        {

        }

        #endregion

        #region Properties

        public CertificateType CertificateType 
        { get; set; }

        public StoreName StoreName
        { get; set; }

        public StoreLocation StoreLocation
        { get; set; }

        public string EncryptionCert
        { get; set; }

        public string EncryptionPassword
        { get; set; }


        public string SigningCert
        { get; set; }

        public string SigningPassword
        { get; set; }



        #endregion


        #region Methods


        public bool Exists ( string filepath )
        {
            var exists = false;

            if ( string.IsNullOrEmpty ( filepath ) )
                throw new ArgumentNullException ( "filepath required" );


            if ( File.Exists ( filepath ) )
                exists = true;

            return exists;

        }

        public Dictionary<WhichCert,X509Certificate2> GetCerts ()
        {
            var certs = new Dictionary<WhichCert, X509Certificate2> ();
            if ( certs == null )
                throw new NullReferenceException ( "Failed to create dictionary" );


            X509Certificate2 xEncrypt = null;
            X509Certificate2 xSigning = null;

            if ( CertificateType == CertificateType.File )
            {
                xEncrypt = new X509Certificate2 ( File.ReadAllBytes ( EncryptionCert ), EncryptionPassword );
                if ( xEncrypt == null )
                    throw new NullReferenceException ( "Failed to create cert" );

                xSigning = new X509Certificate2 ( File.ReadAllBytes ( SigningCert ), SigningPassword );
                if ( xSigning == null )
                    throw new NullReferenceException ( "Failed to create cert" );
            }
            else
            {
                var store = new X509Store ( StoreName, StoreLocation );

                store.Open ( OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly );

                var subject = EncryptionCert;
                var results = store.Certificates.Find ( X509FindType.FindBySubjectName, subject, false );

                if ( results != null && results.Count > 0 )
                    xEncrypt = results [ 0 ];

                subject = SigningCert;
                results = store.Certificates.Find ( X509FindType.FindBySubjectName, subject, false );

                if ( results != null && results.Count > 0 )
                    xSigning = results [ 0 ];

            }

            if ( xEncrypt == null )
                throw new InvalidOperationException ( "Certificate was not created" );

            if ( xSigning == null )
                throw new InvalidOperationException ( "Certificate was not created" );

            certs.Add ( WhichCert.Encryption, xEncrypt );
            certs.Add ( WhichCert.Signing, xSigning );


            return certs;
        }

        public static string ViewCerts ( string filepath )
        {
            var bldr = new StringBuilder ();


            var certificates = new Certificates ();
            if ( certificates == null )
                throw new NullReferenceException ( "FAILED to create certificate" );

            certificates.LoadCerts ( filepath );

            var items = certificates.GetCerts ();

            foreach ( var item in items )
            {

                var whichCert = item.Key;
                var x509 = item.Value;

                // Refer:
                // https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.tostring?view=net-8.0

                bldr.AppendFormat ( "{0}Subject: {1}{0}", Environment.NewLine, x509.Subject );
                bldr.AppendFormat ( "{0}Issuer: {1}{0}", Environment.NewLine, x509.Issuer );
                bldr.AppendFormat ( "{0}Version: {1}{0}", Environment.NewLine, x509.Version );
                bldr.AppendFormat ( "{0}Valid Date: {1}{0}", Environment.NewLine, x509.NotBefore );
                bldr.AppendFormat ( "{0}Expiry Date: {1}{0}", Environment.NewLine, x509.NotAfter );
                bldr.AppendFormat ( "{0}Thumbprint: {1}{0}", Environment.NewLine, x509.Thumbprint );
                bldr.AppendFormat ( "{0}Serial Number: {1}{0}", Environment.NewLine, x509.SerialNumber );
                bldr.AppendFormat ( "{0}Friendly Name: {1}{0}", Environment.NewLine, x509.PublicKey.Oid.FriendlyName );
                bldr.AppendFormat ( "{0}Public Key Format: {1}{0}", Environment.NewLine, x509.PublicKey.EncodedKeyValue.Format ( true ) );
                bldr.AppendFormat ( "{0}Raw Data Length: {1}{0}", Environment.NewLine, x509.RawData.Length );
                bldr.AppendFormat ( "{0}Certificate to string: {1}{0}", Environment.NewLine, x509.ToString ( true ) );

                bldr.AppendLine ( Environment.NewLine );

            }



            return bldr.ToString ();

        }

        public ExecutionResult SaveCerts ( string filepath )
        {
            var certsPath = Path.GetDirectoryName ( filepath );


            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter () }
            };




            if ( CertificateType == CertificateType.File )
            {
                var path = Path.GetDirectoryName ( EncryptionCert );

                // do certificates exist
                if ( string.IsNullOrEmpty ( path ) )
                {
                    var tmpCertPath = Path.Combine ( path, EncryptionCert );

                    if ( !System.IO.File.Exists ( tmpCertPath ) )
                        throw new InvalidOperationException ( "Encryption certificate does not exist: " + EncryptionCert );
                }
                else
                {
                    if ( !System.IO.File.Exists ( EncryptionCert ) )
                        throw new InvalidOperationException ( "Encryption certificate does not exist: " + EncryptionCert );
                }

                path = Path.GetDirectoryName ( SigningCert );

                if ( string.IsNullOrEmpty ( path ) )
                {
                    var tmpCertPath = Path.Combine ( path, SigningCert );

                    if ( !System.IO.File.Exists ( tmpCertPath ) )
                        throw new InvalidOperationException ( "Signing certificate does not exist: " + SigningCert );

                }
                else
                {
                    if ( !System.IO.File.Exists ( SigningCert ) )
                        throw new InvalidOperationException ( "Signing certificate does not exist: " + SigningCert );
                }


                var data =
                    new Certificates ()
                    {
                        CertificateType = CertificateType,
                        StoreName = StoreName.Disallowed,
                        StoreLocation = StoreLocation.LocalMachine,
                        EncryptionCert = EncryptionCert,
                        EncryptionPassword = EncryptionPassword,
                        SigningCert = SigningCert,
                        SigningPassword = SigningPassword,
                    };
                        


                string json = JsonSerializer.Serialize ( data, serializerOptions );
                File.WriteAllText ( filepath, json );
            }
            else
            {
                if ( string.IsNullOrEmpty ( EncryptionCert ) )
                {
                    return new ExecutionResult { Status = StatusCode.Failed, ErrorMessage = "CertificateInStoreNotFound" };
                }

                if ( string.IsNullOrEmpty ( SigningCert ) )
                {
                    return new ExecutionResult { Status = StatusCode.Failed, ErrorMessage = "CertificateInStoreNotFound" };
                }

                var data =
                    new Certificates ()
                    {
                        CertificateType = CertificateType,
                        StoreName = StoreName,
                        StoreLocation = StoreLocation,
                        EncryptionCert = EncryptionCert,
                        EncryptionPassword = EncryptionPassword,
                        SigningCert = SigningCert,
                        SigningPassword = SigningPassword,
                    };

                var store = new X509Store ( StoreName, StoreLocation );

                store.Open ( OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly );

                var subject = EncryptionCert;
                var results = store.Certificates.Find ( X509FindType.FindBySubjectName, subject, false );

                if ( results == null || results.Count == 0 )
                    throw new InvalidOperationException ( "Encryption Certificate is not in store " + StoreName + " " + StoreLocation + " " + EncryptionCert );

                subject = SigningCert;
                results = store.Certificates.Find ( X509FindType.FindBySubjectName, subject, false );

                if ( results == null || results.Count == 0 )
                    throw new InvalidOperationException ( "Signing Certificate is not in store " + StoreName + " " + StoreLocation + " " + SigningCert );

                string json = JsonSerializer.Serialize ( data, serializerOptions );
                File.WriteAllText ( filepath, json );
            }

            return new ExecutionResult () { Status = StatusCode.Success };
        }



        public void LoadCerts ( string filepath )
        {
            var certPath = Path.GetDirectoryName ( filepath );

            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
                Converters = { new JsonStringEnumConverter () }
            };


            if ( string.IsNullOrEmpty ( filepath ) )
                throw new ArgumentNullException ( "filepath required" );

            if ( !File.Exists ( filepath ) )
                throw new InvalidOperationException ( "certificates file does not exist filepath" );


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



            if ( string.IsNullOrEmpty ( json ) )
                throw new InvalidOperationException ( "JSON file is not valid: " + filepath );

            var resp = JsonSerializer.Deserialize<Certificates> ( json, serializerOptions );

            CertificateType = resp.CertificateType;
            StoreName = resp.StoreName;
            StoreLocation = resp.StoreLocation;

            EncryptionCert = resp.EncryptionCert;
            EncryptionPassword = resp.EncryptionPassword;

            SigningPassword = resp.SigningPassword;
            SigningCert = resp.SigningCert;


            if ( CertificateType == CertificateType.File )
            {
                {
                    if ( string.IsNullOrEmpty ( EncryptionCert ) )
                        throw new InvalidOperationException ( "Encryption cert not valid" );


                    var path = Path.GetDirectoryName ( EncryptionCert );

                    if ( string.IsNullOrEmpty ( path ) )
                        EncryptionCert = Path.Combine ( certPath, EncryptionCert );

                    if ( !File.Exists ( EncryptionCert ) )
                        throw new InvalidOperationException ( "Encryption cert does not exist: " + EncryptionCert );
                }


                {
                    if ( string.IsNullOrEmpty ( SigningCert ) )
                        throw new InvalidOperationException ( "Signing cert not valid" );


                    var path = Path.GetDirectoryName ( SigningCert );

                    if ( string.IsNullOrEmpty ( path ) )
                        SigningCert = Path.Combine ( certPath, SigningCert );

                    if ( !File.Exists ( SigningCert ) )
                        throw new InvalidOperationException ( "Signing cert does not exist: " + SigningCert );
                }
            }
            else
            {
                if ( string.IsNullOrEmpty ( EncryptionCert ) )
                    throw new InvalidOperationException ( "Encryption cert name not valid" );


                if ( string.IsNullOrEmpty ( SigningCert ) )
                    throw new InvalidOperationException ( "Signing cert name not valid" );

            }


            Debug.WriteLine ( "Encryption Cert: " + EncryptionCert );

            Debug.WriteLine ( "Signing Cert: " + SigningCert );


        }


        public static string GetBasePath ()
        {
            var basePath = PathUtilities.GetInstallPath ();

            var pos = basePath.IndexOf ( "ApiSamples" );
            if ( pos != -1 )
            {
                basePath = basePath.Substring ( 0, pos );

                basePath = Path.Combine ( basePath, "Certificates" );
            }

            pos = basePath.IndexOf ( "bin" );
            if ( pos != -1 )
            {
                basePath = basePath.Substring ( 0, pos );

                if ( basePath.Contains ( "Program Files" ) )
                    basePath = Path.Combine ( basePath, "Certificates" );
                else
                    basePath = Path.Combine ( basePath, @"Certificates" );

            }

            return basePath;
        }

        public static string GetCertificatePath ()
        {
            return GetBasePath ();
        }





        public static string GetIdpCertsFilePath ()
        {
            var certPath = GetCertificatePath ();

            // this code must work with web server in its dir
            // and with unit tests

            var idpCertsPath = Path.Combine ( certPath, IdpCertsFilename );

            Debug.WriteLine ( "Idp Certs Path " + idpCertsPath );


            return idpCertsPath;

        }

        public static string GetApiCertsFilePath ()
        {
            var certPath = GetCertificatePath ();

            var apiCertsPath = Path.Combine ( certPath, ApiCertsFilename );

            Debug.WriteLine ( "Api Certs Path " + apiCertsPath );


            return apiCertsPath;



        }


        #endregion





    }
}
