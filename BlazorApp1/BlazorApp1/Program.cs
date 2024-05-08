
using System.Security.Cryptography;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Logging;


using BlazorApp1.Client.Pages;
using BlazorApp1.Components;
using BlazorApp1.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Authentication;



namespace BlazorApp1
{
    public class Program
    {
        public static void Main ( string [] args )
        {
            X509Certificate2 apiCert = null;
            X509Certificate2 idpCert = null;
            X509Store store = null;

            Ports apiPorts = null;
            Certificates apiCertificates = null;


            var useSetup = true;

            try
            {

                // set url to listen on
                // using apiports.json for port
                // this is so user can specify port


                if ( useSetup )
                {
                    var apiPortsPath = Ports.GetApiPortsFilePath ();

                    Debug.WriteLine ( "ports.json path: " + apiPortsPath );


                    if ( !Ports.Exists ( apiPortsPath ) )
                        throw new InvalidOperationException ( "FAILED no ports file" );

                    apiPorts = Ports.LoadPorts ( apiPortsPath );



                    Debug.WriteLine ( "ApiPort " + apiPorts.Port );

                    // get certificate to use
                    // using certificate.json for info
                    // this so user can use their own certificate

                    var apiCertsPath = Certificates.GetApiCertsFilePath ();

                    apiCertificates = new Certificates ();
                    if ( apiCertificates == null )
                        throw new NullReferenceException ( "FAILED to create certificates" );


                    if ( !apiCertificates.Exists ( apiCertsPath ) )
                        throw new InvalidOperationException ( "FAILED no api certificates file" );

                    apiCertificates.LoadCerts ( apiCertsPath );

                    Debug.WriteLine ( "Encrypton Cert " + apiCertificates.EncryptionCert );
                    Debug.WriteLine ( "Signing Cert " + apiCertificates.SigningCert );
                }


                var builder = WebApplication.CreateBuilder ( args );




                if ( useSetup )
                {
                    var port = apiPorts.Port;

                    var ipAddress = IPAddress.Parse ( "127.0.0.1" );


                    /*
                    if ( apiCertificates.CertificateType == CertificateType.File )
                    {
                        var certPath = apiCertificates.EncryptionCert;
                        var certPassword = apiCertificates.EncryptionPassword;

                        Debug.WriteLine ( "Using file certificate " + certPath );


                        apiCert = new X509Certificate2 ( certPath, certPassword );
                        if ( apiCert == null )
                            throw new NullReferenceException ( "FAILED to get cert from path " + certPath );

                        Debug.WriteLine ( "X509Certificate2 created from file" );

                    }

                    else
                    {
                        var storeName = StoreName.Root; //apiCertificates.StoreName;
                        var storeLocation = StoreLocation.LocalMachine; //apiCertificates.StoreLocation;
                        var subject = "AAA Certificate";     //apiCertificates.EncryptionCert;

                        Debug.WriteLine ( "Using stored certificate StoreName: " + storeName + " StoreLocation: " + storeLocation );
                        store = new X509Store ( storeName, storeLocation );

                        store.Open ( OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly );

                        var results = store.Certificates.Find ( X509FindType.FindBySubjectName, subject, false );

                        if ( results != null && results.Count > 0 )
                            apiCert = results [ 0 ];

                        if ( apiCert == null )
                            throw new NullReferenceException ( "FAILED to get cert from store" );

                        Debug.WriteLine ( "X509Certificate2 created from store" );


                    }
                    

                    // ERR_SSL_PROTOCOL_ERROR
                    
                    builder.WebHost.ConfigureKestrel (
                        options =>
                        {
                            options.ConfigureHttpsDefaults ( options =>
                            {

                                options.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                                options.ServerCertificate = apiCert;

                                options.AllowAnyClientCertificate ();
                                options.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                            } );

                            options.Listen (
                                ipAddress, port );


                        } );
                        
                    */

                    
                    // this setup works with self signed cert
                    // but will show as insecure
                    // not sure what tls defaults are
                    
                    builder.WebHost.ConfigureKestrel (
                        options =>
                        {
                            var port = apiPorts.Port;

                            if ( apiCertificates.CertificateType == CertificateType.File )
                            {
                                var pfxFilePath = apiCertificates.EncryptionCert;
                                var pfxPassword = apiCertificates.EncryptionPassword;

                                options.Listen (
                                    ipAddress, port,
                                    listenOptions =>
                                    {
                                        // Configure Kestrel to use a certificate from a local .PFX file for hosting HTTPS
                                        listenOptions.UseHttps ( pfxFilePath, pfxPassword );
                                    } );
                            }
                            else
                            {
                                var storeName = apiCertificates.StoreName;
                                var storeLocation = apiCertificates.StoreLocation;
                                var subject = apiCertificates.EncryptionCert;

                                options.Listen (
                                    ipAddress, port,
                                    listenOptions =>
                                    {
                                        // Configure Kestrel to use a certificate from a local .PFX file for hosting HTTPS
                                        listenOptions.UseHttps ( storeName, subject, false, storeLocation );
                                    } );
                            }
                        } );
                       
                    


                }


                // Add services to the container.
                builder.Services.AddRazorComponents ()
                    .AddInteractiveServerComponents ()
                    .AddInteractiveWebAssemblyComponents ();

                builder.Services.AddLogging ( loggingBuilder => loggingBuilder
                    .AddConsole ()
                    .AddDebug ()
                    .SetMinimumLevel ( LogLevel.Trace ) );


                var app = builder.Build ();

                // Configure the HTTP request pipeline.
                if ( app.Environment.IsDevelopment () )
                {
                    app.UseWebAssemblyDebugging ();
                }
                else
                {
                    app.UseExceptionHandler ( "/Error" );
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts ();
                }

                app.UseHttpsRedirection ();

                app.UseStaticFiles ();
                app.UseAntiforgery ();

                app.MapRazorComponents<App> ()
                    .AddInteractiveServerRenderMode ()
                    .AddInteractiveWebAssemblyRenderMode ()
                    .AddAdditionalAssemblies ( typeof ( Client._Imports ).Assembly );

                app.Run ();
            }
            catch ( AggregateException aex )
            {
                Debug.WriteLine ( "Exception in Gt.WebApi" + aex.ToString () );
            }
            catch ( Exception ex )
            {
                Debug.WriteLine ( "Exception in Gt.WebApi", ex );
            }
            finally
            {

                if ( store != null )
                {
                    store.Close ();
                    store.Dispose ();
                }

                if ( apiCert != null )
                {
                    apiCert.Reset ();
                    apiCert.Dispose ();
                }

                if ( idpCert != null )
                {
                    idpCert.Reset ();
                    idpCert.Dispose ();
                }

            }

        }

        private static bool ClientCertificateValidation ( X509Certificate2 arg1, X509Chain arg2, SslPolicyErrors arg3 )
        {
            return true;
        }
    }
}
