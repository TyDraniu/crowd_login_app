using System;
using System.Web.Services.Protocols;
using System.Windows.Forms;

namespace Login
{
    /// <summary>
    /// Simple authentication using Crowd SOAP API with C# via a Proxy Component.
    /// </summary>
    public class Authentication : IDisposable
    {
        // Instance of Proxy to SOAP API
        private SecurityServer _securityServer = new SecurityServer();

        // Sample constants - change these appropriate to your application
        // NB: This is not secure and is included here for test purposes only.
        private const string APPLICATION_NAME = "SampleApp";
        private const string APPLICATION_PWD = "SamplePwd";

        public Authentication()
        {
            // Class constructor.
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                _securityServer.Dispose();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public SecurityServer securityServer
        {
            get {return _securityServer; }
        }

        /// <summary>
        /// Authenticates a user in the sample application
        /// </summary>
        /// <param name="username">Name of the user (principal) to be authenticated</param>
        /// <param name="password">Password to validate</param>
        /// <param name="principal">If authenticated, returns the user data from the Crowd server</param>
        /// <returns>TRUE if the user was successfully authenticated, FALSE otherwise</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters")]
        public bool Authenticate(string username,
            string password,
            out SOAPPrincipal principal)
        {
            bool authenticated = false;
            principal = null;

            ApplicationAuthenticationContext appContext = new ApplicationAuthenticationContext();
            appContext.name = APPLICATION_NAME;

            // Provide the password associated with the application, as set-up in Crowd.
            PasswordCredential pwdApp = new PasswordCredential();
            pwdApp.credential = APPLICATION_PWD;
            appContext.credential = pwdApp;

            try
            {
                // Authenticate the application (will fire a SOAPException if authentication fails).
                AuthenticatedToken appToken = _securityServer.authenticateApplication(appContext);

                if (appToken != null)
                {
                    // Set-up authentication context for the principal (user)
                    UserAuthenticationContext userContext = new UserAuthenticationContext();
                    userContext.application = APPLICATION_NAME;
                    userContext.name = username;

                    // Provide the password for authenticating this principal (user)
                    PasswordCredential pwdPrincipal = new PasswordCredential();
                    pwdPrincipal.credential = password;
                    userContext.credential = pwdPrincipal;

                    // Authenticate the principal (will fire a SOAPException if authentication fails).
                    string principalToken = _securityServer.authenticatePrincipal(appToken, userContext);

                    if (!string.IsNullOrEmpty(principalToken))
                    {
                        // Find some more details about this authentication user.
                        principal = _securityServer.findPrincipalByToken(appToken, principalToken);

                        authenticated = true;
                    }
                }
            }

            catch (SoapException soapException)
            {
                // Handle Authentication/SOAP Errors here... 
                if (soapException.Detail.FirstChild.Name == "InvalidAuthenticationException")
                {
                    MessageBox.Show("Błędny login lub hasło", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // throw;
                }
                else
                {
                    MessageBox.Show(soapException.Message, soapException.Detail.FirstChild.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                #region ExceptionInfo
                // Consult soapException.Detail.FirstChild.Name for further details:

                // This may be set to one of:
                //      RemoteException
                //      InvalidAuthenticationException 
                //      InvalidAuthorizationTokenException
                //      InactiveAccountException
                //      InvalidTokenException
                #endregion

            }
            catch (Exception ex)
            {
                // Handle all other errors here...
                MessageBox.Show(ex.Message);
            }

            return authenticated;
        }

        public AuthenticatedToken Authenticate()
        {
            try
            {
                ApplicationAuthenticationContext appContext = new ApplicationAuthenticationContext();
                appContext.name = APPLICATION_NAME;

                // Provide the password associated with the application, as set-up in Crowd.
                PasswordCredential pwdApp = new PasswordCredential();
                pwdApp.credential = APPLICATION_PWD;
                appContext.credential = pwdApp;

                // Authenticate the application (will fire a SOAPException if authentication fails).
                return this._securityServer.authenticateApplication(appContext);
            }
            catch (SoapException soapException)
            {
                MessageBox.Show(soapException.Message, soapException.Detail.FirstChild.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }
}
