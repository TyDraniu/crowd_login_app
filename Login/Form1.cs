using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using RestSharp;
using StashRest;
using System.Reflection;

namespace Login
{
    public partial class Form1 : Form
    {
        private RestClient client;
        private Stopwatch timer;
        private IObservable<List<PValue>> pro1 = null;
        private IObservable<TimeSpan> clock;

        public Form1()
        {
            InitializeComponent();

            SynchronizationContextScheduler UIThread = new SynchronizationContextScheduler(SynchronizationContext.Current);
            NewThreadScheduler NewThread = new NewThreadScheduler();

            client = new RestClient(Stash.baseUrl);
            timer = new Stopwatch();

            var keyPressed = Observable
                .FromEventPattern<KeyEventArgs>(textBox2, nameof(Control.KeyDown))
                .Where(x => x.EventArgs.KeyCode == Keys.Enter)
                .Subscribe(x => this.textBoxDown(x.Sender, x.EventArgs));

            pro1 = Observable
                .FromAsync(ct => Stash.GetProjectsAsync(client, 25, ct))
                .SubscribeOn(NewThread);

            clock = Observable
                .Interval(TimeSpan.FromSeconds(0.1))
                .Select(_ => timer.Elapsed)
                .TakeWhile(_ => timer.IsRunning)
                .ObserveOn(UIThread);
        }

        private void textBoxDown(object sender, EventArgs e)
        {
            button1.Select();
            button1_Click(sender, e);
        }

        private Action<TimeSpan> OnNext(Stopwatch t)
        {
            if (!t.IsRunning)
            {
                t.Start();
            }

            return next =>
            {
                this.Text = next.ToString(@"mm\:ss\:ff");
            };
        }

        private Action TimerCompleted()
        {
            return () => timer.Reset();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters")]
        private async void button1_Click(object sender, EventArgs e)
        {
            var sub = clock.Subscribe(OnNext(timer), TimerCompleted());

            await Task.Factory.StartNew(() =>
            {
                SOAPPrincipal SOAPuser = new SOAPPrincipal();

                using (Authentication a = new Authentication())
                {
                    if (a.Authenticate(textBox1.Text, textBox2.Text, out SOAPuser))
                    {
                        string name = SOAPuser.attributes.Single(x => x.name == "displayName").values[0];

                        timer.Stop();
                        MessageBox.Show($"Witaj \n{name}");
                            
                    }
                }
            });
        }


        #region StashButton_Click synchronicznie
        ////private void textBox2_KeyDown(object sender, KeyEventArgs e)
        ////{
        ////    if (e.KeyCode == Keys.Enter)
        ////    {
        ////        button1.Select();
        ////        button1_Click(sender, e);
        ////    }
        ////}

        ////[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters")]
        ////private void StashButton_Click(object sender, EventArgs e)
        ////{
        ////    try
        ////    {
        ////        string user = textBox1.Text;
        ////        string password = textBox2.Text;
        ////        string answer = null;
        ////        Cursor.Current = Cursors.WaitCursor;

        ////        List<PValue> pro = GetProjects(user, password, 25);

        ////        foreach (PValue v in pro)
        ////        {
        ////            answer += v.name + " (" + v.key + ")" + ' ';
        ////        }
        ////        Cursor.Current = Cursors.Default;
        ////        MessageBox.Show(answer);

        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        MessageBox.Show(ex.Message);
        ////    }
        ////}
        #endregion


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters")]
        private void StashButton_Click(object sender, EventArgs e)
        {
            try
            {
                string user = textBox1.Text;
                string password = textBox2.Text;
                string answer = null;
                
                client.Authenticator = new HttpBasicAuthenticator(user, password);

                var sub1 = clock.Subscribe(OnNext(timer), TimerCompleted());

                IDisposable sub = pro1
                    .Subscribe(v =>
                    {
                        v.ForEach(q => answer += $"{q.name} ({q.key}); ");
                    },
                    () =>
                    {
                        timer.Stop();
                        MessageBox.Show(answer);
                        sub1.Dispose();
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters")]
        private async void button2_Click(object sender, EventArgs e)
        {
            string temat = " ";
            timer.Start();

            var sub = clock.Subscribe(next =>
            {
                this.Text = next.ToString(@"mm\:ss\:ff") + temat;
            },
                this.TimerCompleted()
            );

            ///Brak commitów przez tyle dni
            const int INACTVITY = 1000;

            string line = string.Empty;
            string answer = string.Empty;
            string user = textBox1.Text;
            string password = textBox2.Text;

            try
            {
                RestClient client = new RestClient(Stash.baseUrl);
                client.Authenticator = new HttpBasicAuthenticator(user, password);

                List<PValue> projekty = await Task.Factory.StartNew(() => Stash.GetProjectsAsync(client, 25));

                foreach (PValue p in projekty)
                {
                    /// Pobieramy listę repozytoriów dla danego projektu
                    List<PValue> repos = await Task.Factory.StartNew(() => Stash.GetReposAsync(client, p.key));

                    IRestResponse<Project> commit_response = null;
                    foreach (PValue r in repos)
                    {
                        temat = " " + p.name;
                        /// Commity 
                        commit_response = await Task.Factory.StartNew(() => Stash.GetCommitsAsync(client, p.key, r.slug));

                        if (commit_response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            if (Stash.IsInactive(commit_response.Data.values, INACTVITY))
                            {
                                line = p.name + " (" + p.key + "): " + r.name + " [" + Stash.MaxDate(commit_response.Data.values).ToShortDateString() + "] " + '\n';
                            }
                        }
                        else
                        {
                            line = p.name + " (" + p.key + "): " + r.name + " (Empty)\n";
                        }
                    }

                    if (!string.IsNullOrEmpty(line))
                    {
                        answer += line;
                        line = string.Empty;
                    }
                }

                timer.Stop();

                MessageBox.Show(answer);
                answer = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #region  Task<SOAPPrincipal> findPrincipal()
        //private Task<SOAPPrincipal> findPrincipal(SecurityServer s, AuthenticatedToken token, string user)
        //{
        //    return Task.Run(() =>
        //    {
        //        SOAPPrincipal u = new SOAPPrincipal();

        //        u = s.findPrincipalWithAttributesByName(token, user);
        //        return u;
        //    });
        //}
        #endregion

        private async void button3_Click(object sender, EventArgs e)
        {
            timer.Start();
            using (Authentication a = new Authentication())
            {
                AuthenticatedToken token = a.Authenticate();
                try
                {
                    SecurityServer s = a.securityServer;

                    List<string> users = await s.findAllPrincipalsAsync(token);
                    List<string> notOurUsers = new List<string>();

                    foreach (string user in users)
                    {
                        SOAPPrincipal SOAPuser = await s.findPrincipalAsync(token, user);
                        if (SOAPuser.active)
                        {
                            bool ourMember = SOAPuser.attributes.Select(x => x.name).Contains("lastAuthenticated");

                            if (!ourMember)
                            {
                                notOurUsers.Add(user);
                            }
                        }
                    }

                    notOurUsers.Sort();

                    MessageBox.Show(string.Join(" ", notOurUsers), timer.Elapsed.ToString());
                    timer.Reset();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            timer.Start();
            using (Authentication a = new Authentication())
            {
                AuthenticatedToken token = a.Authenticate();
                try
                {
                    SecurityServer s = a.securityServer;

                    List<string> users = await s.findAllPrincipalsAsync(token);
                    Dictionary<string, int> UsersWithWrongPassword = new Dictionary<string, int>();


                    foreach (string user in users)
                    {
                        this.Text = timer.Elapsed.ToString(@"mm\:ss\:ff");
                        SOAPPrincipal SOAPuser = await s.findPrincipalAsync(token, user);
                        if (SOAPuser.active)
                        {
                            try
                            {
                                string zleHasla = SOAPuser.attributes.SingleOrDefault(x => x.name == "invalidPasswordAttempts")?.values[0];

                                int wrPass;
                                int.TryParse(zleHasla, out wrPass);
                                if (wrPass > 2)
                                {
                                    UsersWithWrongPassword.Add(SOAPuser.attributes.SingleOrDefault(x => x.name == "displayName")?.values[0], wrPass);
                                }
                            }
                            catch (Exception uex)
                            {
                                MessageBox.Show(uex.Message + '\n' + SOAPuser.name);
                            }
                        }
                    }

                    MessageBox.Show(string.Join("\n", UsersWithWrongPassword.OrderBy(x => x.Key)), timer.Elapsed.ToString());
                    this.Text = "Form1";
                    timer.Reset();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            List<string> grupy = new List<string>();

            grupy.Add("jira-users");
            grupy.Add("jira-restricted-users");
            grupy.Add("confluence-users");
            grupy.Add("confluence-restricted-users");
            grupy.Add("stash-users");

            timer.Start();
            using (Authentication a = new Authentication())
            {
                AuthenticatedToken token = a.Authenticate();
                try
                {
                    SecurityServer s = a.securityServer;

                    List<string> Allusers = await s.findAllPrincipalsAsync(token);
                    List<string> tempUsers = new List<string>();

                    foreach (string g in grupy)
                    {
                        SOAPGroup group = await s.findGroup(token, g);

                        foreach (var m in group.members)
                        {
                            Allusers.Remove(m);
                            this.Text = timer.Elapsed.ToString(@"mm\:ss\:ff");
                        }
                    }

                    tempUsers.AddRange(Allusers);

                    foreach (var u in tempUsers)
                    {
                        SOAPPrincipal SOAPuser = await s.findPrincipalAsync(token, u);
                        if (!SOAPuser.active)
                        {
                            Allusers.Remove(SOAPuser.name);
                            this.Text = timer.Elapsed.ToString(@"mm\:ss\:ff");
                        }
                    }

                    Allusers.Sort();

                    MessageBox.Show(string.Join(" ", Allusers), timer.Elapsed.ToString());
                    this.Text = "Form1";
                    timer.Reset();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            List<string> grupy = new List<string>();

            grupy.Add("jira-users");
            grupy.Add("jira-restricted-users");

            timer.Start();
            using (Authentication a = new Authentication())
            {
                AuthenticatedToken token = a.Authenticate();
                try
                {
                    SecurityServer s = a.securityServer;

                    List<string> JiraUsers = new List<string>();
                    List<string> RestrictedUsers = new List<string>();
                    List<string> CommonUsers = new List<string>(); 

                    SOAPGroup Jiragroup = await s.findGroup(token, grupy[0]);
                    SOAPGroup Restrictedgroup = await s.findGroup(token, grupy[1]);

                    JiraUsers.AddRange(Jiragroup.members.ToList());
                    RestrictedUsers.AddRange(Restrictedgroup.members.ToList());

                    CommonUsers = JiraUsers.FindAll(x => RestrictedUsers.Contains(x));

                    List<SOAPPrincipal> CommonPrincipals = new List<SOAPPrincipal>();
                    foreach (var user in CommonUsers)
                    {
                        SOAPPrincipal SOAPuser = await s.findPrincipalAsync(token, user);
                        if (SOAPuser.active)
                        {
                            CommonPrincipals.Add(SOAPuser);
                        }
                    }

                    MessageBox.Show(string.Join(" ", CommonPrincipals.Select(x => x.name)), timer.Elapsed.ToString());
                    timer.Reset();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            Console.Write("x");
        }
    }
}
