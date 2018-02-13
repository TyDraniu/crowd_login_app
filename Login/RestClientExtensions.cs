using System;
using System.Threading.Tasks;
using RestSharp;
using StashRest;

namespace RestSharpEx
{
    public static class RestClientExtensions
    {
        private static Task<T> SelectAsync<T>(this RestClient client, IRestRequest request, Func<IRestResponse, T> selector)
        {
            var tcs = new TaskCompletionSource<T>();
            client.ExecuteAsync(request, r =>
            {
                if (r.ErrorException == null)
                {
                    tcs.SetResult(selector(r));
                }
                else
                {
                    tcs.SetException(r.ErrorException);
                }
            });
            return tcs.Task;
        }

        private static Task<IRestResponse<Project>> SelectPAsync<T>(this RestClient client, IRestRequest request, Func<IRestResponse<Project>, IRestResponse<Project>> selector)
        {
            var tcs = new TaskCompletionSource<IRestResponse<Project>>();
            client.ExecuteAsync<Project>(request, r =>
            {
                if (r.ErrorException == null)
                {
                    tcs.SetResult(selector(r));
                }
                else
                {
                    tcs.SetException(new Exception(r.Data.errors[0].message));
                }
            });
            return tcs.Task;
        }

        public async  static Task<IRestResponse<Project>> GetResponsePAsync(this RestClient client, IRestRequest request)
        {
            return await client.SelectPAsync<Project>(request, r => r);
        }
    }
}