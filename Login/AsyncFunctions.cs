using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Login
{
    /// <summary>
    /// Event-based Asynchronous Pattern (EAP).  
    /// For some synchronous method Xyz, the EAP provides an asynchronous counterpart XyzAsync.  
    /// Calling this method launches the asynchronous work, and when the work completes, a corresponding XyzCompleted event is raised. 
    /// </summary>
    public static class AsyncFunctions
    {
        public static Task<SOAPPrincipal> findPrincipalAsync(this SecurityServer s, AuthenticatedToken t, string u)
        {
            var tcs = CreateSource<SOAPPrincipal>(null);
            s.findPrincipalWithAttributesByNameCompleted += (send, args) => TransferCompletion(tcs, args, () => args.Result, null);
            s.findPrincipalWithAttributesByNameAsync(t, u);

            return tcs.Task;
        }

        public static Task<List<string>> findAllPrincipalsAsync(this SecurityServer s, AuthenticatedToken t)
        {
            var tcs = CreateSource<List<string>>(null);
            s.findAllPrincipalNamesCompleted += (send, args) => TransferCompletion<List<string>>(tcs, args, () => args.Result.ToList(), null);
            s.findAllPrincipalNamesAsync(t);
            
            return tcs.Task;
        }

        public static Task<List<string>> findGroupMembershipAsync(this SecurityServer s, AuthenticatedToken t, SOAPGroup group)
        {
            var tcs = CreateSource<List<string>>(null);
            s.findGroupMembershipsCompleted += (send, args) => TransferCompletion<List<string>>(tcs, args, () => args.Result.ToList(), null);
            s.findGroupMembershipsAsync(t, group.name);

            return tcs.Task;
        }

        public static Task<SOAPGroup> findGroup(this SecurityServer s, AuthenticatedToken t, string group)
        {
            var tcs = CreateSource<SOAPGroup>(null);
            s.findGroupWithAttributesByNameCompleted += (send, args) => TransferCompletion<SOAPGroup>(tcs, args, () => args.Result, null);
            s.findGroupWithAttributesByNameAsync(t, group);

            return tcs.Task;
        }

        private static TaskCompletionSource<T> CreateSource<T>(object state)
        {
            return new TaskCompletionSource<T>(
                state, 
                TaskCreationOptions.None);
        }

        private static void TransferCompletion<T>(
            TaskCompletionSource<T> tcs, 
            AsyncCompletedEventArgs e,
            Func<T> getResult, 
            Action unregisterHandler)
        {
            if (e.Cancelled)
            {
                tcs.TrySetCanceled();
            }
            else if (e.Error != null)
            {
                tcs.TrySetException(e.Error);
            }
            else
            {
                tcs.TrySetResult(getResult());
            }

            unregisterHandler?.Invoke();
        }
    }
}
