using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace TransientFaultHandling
{
    internal class HttpApiTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        private static readonly WebExceptionStatus[] WebExceptionRetryStatus = new[]
        {
            WebExceptionStatus.NameResolutionFailure,       // cannot resolve the DNS
            WebExceptionStatus.ConnectFailure,              // cannot connect to the server 
            WebExceptionStatus.ReceiveFailure,              // server was down before HttpWebRequest.GetResponse
            WebExceptionStatus.SendFailure,                 // cannot send the request to server
            WebExceptionStatus.PipelineFailure,             // connection was closed in pipeline mode
            WebExceptionStatus.RequestCanceled,
            WebExceptionStatus.ConnectionClosed,            // connection was closed
            WebExceptionStatus.KeepAliveFailure,
            WebExceptionStatus.Timeout,                     // timeout occurred in HttpWebRequest.GetResponse
            WebExceptionStatus.ProxyNameResolutionFailure,  // cannot resolve the DNS for proxy
            WebExceptionStatus.UnknownError
        };

        private static readonly HttpStatusCode[] HttpRetryStatusCodes = new[]
        {
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        };

        #region ITransientErrorDetectionStrategy implementation
        /// <summary>
        /// Determines whether the specified exception represents a transient failure that can be compensated by a retry.
        /// </summary>
        /// <param name="exception">The exception object to be verified.</param>
        /// <returns>True if the specified exception is considered as transient, otherwise false.</returns>
        public bool IsTransient(Exception exception)
        {
            // flatten the entire exception tree
            // whenever we found a retriable exception in the tree, we will assume the error is retriable
            // otherwise, we return false
            var exceptionList = FlattenException(exception);

            foreach (var ex in exceptionList)
            {
                var webException = ex as WebException;
                if (webException != null)
                {
                    if (WebExceptionRetryStatus.Contains(webException.Status))
                    {
                        return true;
                    }

                    var httpWebResponse = webException.Response as HttpWebResponse;
                    if (httpWebResponse != null && HttpRetryStatusCodes.Contains(httpWebResponse.StatusCode))
                    {
                        return true;
                    }
                }

                if (ex is TaskCanceledException)
                {
                    // when timeout occurs, HttpClient will throw TaskCanceledException
                    return true;
                }
            }

            return false;
        }

        // internal for unit testing
        internal static IEnumerable<Exception> FlattenException(Exception exception)
        {
            if (exception.InnerException == null)
            {
                yield return exception;
            }
            else
            {
                var aggregateException = exception as AggregateException;
                if (aggregateException != null)
                {
                    foreach (var ex in aggregateException.InnerExceptions)
                    {
                        // recursively append child exceptions
                        var childCollection = FlattenException(ex);
                        foreach (var child in childCollection)
                        {
                            yield return child;
                        }
                    }
                }
                else
                {
                    yield return exception;

                    // call this function recursively, since it can happen that the child is an AggregateException
                    var childCollection = FlattenException(exception.InnerException);
                    foreach (var child in childCollection)
                    {
                        yield return child;
                    }
                }
            }
        }
        #endregion
    }
}
