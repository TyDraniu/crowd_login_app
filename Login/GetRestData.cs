using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using StashRest;
using RestSharp;
using RestSharpEx;
using System.Threading;

namespace Login
{
    public class Stash
    {
        public const string baseUrl = "http://stashurl";

        /// <summary>
        /// Zwraca datę ostatniego commitu na repozytorium
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static DateTime MaxDate(List<PValue> list)
        {
            double maxTimestamp = list.Max(l => l.authorTimestamp);
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(maxTimestamp).ToLocalTime();
            return dtDateTime;
        }

        public static bool IsInactive(List<PValue> repo, int days)
        {
            double maxTimestamp = repo.Max(l => l.authorTimestamp);
            TimeSpan t = DateTime.Now.AddDays(-days) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            double epoch = t.TotalMilliseconds;
            return maxTimestamp < epoch;
        }

        private static List<PValue> GetProjects(string login, string pass, int limit)
        {
            var client = new RestClient(baseUrl);
            client.Authenticator = new HttpBasicAuthenticator(login, pass);

            var request = new RestRequest("rest/api/1.0/projects", Method.GET);
            request.AddParameter("limit", limit);
            request.AddParameter("start", 0);
            IRestResponse<Project> response = null;

            List<PValue> projects = new List<PValue>();
            do
            {
                response = client.Execute<Project>(request);
                projects.AddRange(response.Data.values);
                request.Parameters.Single(p => p.Name == "start").Value = (response.Data.start + response.Data.limit).ToString(CultureInfo.InvariantCulture);
            }
            while (!response.Data.isLastPage);

            return projects;
        }

        public static List<PValue> GetProjectsAsync(RestClient client, int limit)
        {
            var request = new RestRequest("rest/api/1.0/projects", Method.GET);
            request.AddParameter("limit", limit);
            request.AddParameter("start", 0);
            IRestResponse<Project> response = null;

            List<PValue> projects = new List<PValue>();
            do
            {
                var task = client.GetResponsePAsync(request);
                response = task.Result;

                projects.AddRange(response.Data.values);
                request.Parameters.Single(p => p.Name == "start").Value = (response.Data.start + response.Data.limit).ToString(CultureInfo.InvariantCulture);
            }
            while (!response.Data.isLastPage);

            return  projects;
        }

        public static Task<List<PValue>> GetProjectsAsync(RestClient client, int limit, CancellationToken ct)
        {
            return Task.Factory.StartNew(() =>
            {
                var request = new RestRequest("rest/api/1.0/projects", Method.GET);
                request.AddParameter("limit", limit);
                request.AddParameter("start", 0);
                IRestResponse<Project> response = null;

                List<PValue> projects = new List<PValue>();
                do
                {
                    var task = client.GetResponsePAsync(request);
                    response = task.Result;

                    projects.AddRange(response.Data.values);
                    request.Parameters.Single(p => p.Name == "start").Value = (response.Data.start + response.Data.limit).ToString(CultureInfo.InvariantCulture);
                }
                while (!response.Data.isLastPage && !ct.IsCancellationRequested);

                return projects;
            });
        }


        public static List<PValue> GetReposAsync(RestClient client, string project)
        {
            var repo_request = new RestRequest("rest/api/1.0/projects/{projectKey}/repos", Method.GET);
            repo_request.AddParameter("projectKey", project, ParameterType.UrlSegment);
            repo_request.AddParameter("limit", 30);
            IRestResponse<Project> response = null;

            List<PValue> repos = new List<PValue>();
            var task = client.GetResponsePAsync(repo_request);
            response = task.Result;
            repos = response.Data.values;

            if (response.Data.errors != null)
            {
                throw new Exception(response.Data.errors[0].message);
            }

            return repos;
        }

        public static IRestResponse<Project> GetCommitsAsync(RestClient client, string project, string repo_slug)
        {
            RestRequest commit_request = new RestRequest("rest/api/1.0/projects/{projectKey}/repos/{repositorySlug}/commits", Method.GET);
            commit_request.AddParameter("projectKey", project, ParameterType.UrlSegment);
            commit_request.AddParameter("repositorySlug", repo_slug, ParameterType.UrlSegment);

            return client.GetResponsePAsync(commit_request).Result;
        }
    }
}
