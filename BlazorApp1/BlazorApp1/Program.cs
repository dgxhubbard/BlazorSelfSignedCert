using BlazorApp1.Client.Pages;
using BlazorApp1.Components;

using System.Security.Cryptography;
using System.Diagnostics;
using BlazorApp1.Enums;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using static System.Formats.Asn1.AsnWriter;
using System.Net;
using System.Security.Cryptography.X509Certificates;

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
                        var storeName = apiCertificates.StoreName;
                        var storeLocation = apiCertificates.StoreLocation;
                        var subject = apiCertificates.EncryptionCert;

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

                    var httpsConnectionAdapterOptions = new HttpsConnectionAdapterOptions
                    {
                        SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                        ClientCertificateMode = ClientCertificateMode.AllowCertificate,
                        ServerCertificate = apiCert

                    };

                    builder.WebHost.ConfigureKestrel (
                        options =>
                        {
                            options.ConfigureEndpointDefaults (
                                listenOptions =>
                                    listenOptions.UseHttps ( httpsConnectionAdapterOptions ) );


                            options.Listen (
                                ipAddress, port );


                        } );
                }


                // Add services to the container.
                builder.Services.AddRazorComponents ()
                    .AddInteractiveServerComponents ()
                    .AddInteractiveWebAssemblyComponents ();

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
    }
}
