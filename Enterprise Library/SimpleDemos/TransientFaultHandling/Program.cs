using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Data.Configuration;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;


namespace TransientFaultHandling
{
    class Program
    {
        static readonly CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(@"DefaultEndpointsProtocol=https;AccountName=jackms;AccountKey=q9l5S479iayA1dfzdV+K0sjNI/bK5aQLrZ5GSkxk6R8wnSaz0dgPuNQnZ5AxUbmq+foi0g7mdNaXIBlVOlQ0qw==");
        static readonly CloudQueueClient Queue = StorageAccount.CreateCloudQueueClient();
        static readonly CloudBlobClient Blob = StorageAccount.CreateCloudBlobClient();

        static void Main()
        {
            // Load policies from the configuration file.
            // SystemConfigurationSource is defined in 
            // Microsoft.Practices.EnterpriseLibrary.Common.
            var settings = RetryPolicyConfigurationSettings.GetRetryPolicySettings(
                new SystemConfigurationSource());
            
            // Initialize the RetryPolicyFactory with a RetryManager built 
            // from the settings in the configuration file.
            RetryPolicyFactory.SetRetryManager(settings.BuildRetryManager());

            SpecifyRetryStrategiesInConfig();
            SpecifyRetryStrategiesInCode();
            RetryAsync();
            SQLAzureRetry();

            Console.ReadKey();
        }

        private static void SpecifyRetryStrategiesInConfig()
        {
            // Create a retry policy that uses a retry strategy from the configuration.
            var retryPolicy = RetryPolicyFactory.GetRetryPolicy
              <StorageTransientErrorDetectionStrategy>("Exponential Backoff Retry Strategy");
            // var retryPolicy = RetryPolicyFactory.GetDefaultAzureStorageRetryPolicy();

            // Receive notifications about retries.
            retryPolicy.Retrying += (sender, args) =>
                {
                    // Log details of the retry.
                    var msg = String.Format("Retry - Count:{0}, Delay:{1}, Exception:{2}",
                        args.CurrentRetryCount, args.Delay, args.LastException);
                    Trace.WriteLine(msg, "Information");
                };

            try
            {
                // Do some work that may result in a transient fault.
                retryPolicy.ExecuteAction(
                    () =>
                        {
                            // Call a method that uses Windows Azure storage and which may
                            // throw a transient exception.
                            var container = Blob.GetContainerReference("data");
                            container.CreateIfNotExists();
                            var blobs = container.ListBlobs();
                            Console.WriteLine();
                        });
            }
            catch (Exception)
            {
                // All the retries failed.
            }
        }

        private static void SpecifyRetryStrategiesInCode()
        {
            // Define your retry strategy: retry 5 times, starting 1 second apart
            // and adding 2 seconds to the interval each retry.
            var retryStrategy = new Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));

            // Define your retry policy using the retry strategy and the Windows Azure storage
            // transient fault detection strategy.
            var retryPolicy = new RetryPolicy<StorageTransientErrorDetectionStrategy>(retryStrategy);

            // Receive notifications about retries.
            retryPolicy.Retrying += (sender, args) =>
            {
                // Log details of the retry.
                var msg = String.Format("Retry - Count:{0}, Delay:{1}, Exception:{2}",
                    args.CurrentRetryCount, args.Delay, args.LastException);
                Trace.WriteLine(msg, "Information");
            };

            try
            {
                var queue = Queue.GetQueueReference("orders");

                // Do some work that may result in a transient fault.
                retryPolicy.ExecuteAction(
                  () =>
                  {
                      // Call a method that uses Windows Azure storage and which may
                      // throw a transient exception.
                      queue.CreateIfNotExists();
                  });
            }
            catch (Exception)
            {
                // All the retries failed.
            }
        }

        private static void RetryAsync()
        {
            var retryPolicy = RetryPolicyFactory.GetDefaultAzureStorageRetryPolicy();

            var container = Blob.GetContainerReference("data-not-exist");

            // ExecuteAsync will return a Task that is awaitable
            retryPolicy.ExecuteAsync(() => Task<bool>.Factory.FromAsync(container.BeginDeleteIfExists,
                                                                        container.EndDeleteIfExists, null)
                                                     .ContinueWith(t =>
                                                         {
                                                             if (t.Exception != null)
                                                             {
                                                                 Console.WriteLine(
                                                                     "Non-transient error, or retry count exceeded.");
                                                             }
                                                             else
                                                             {
                                                                 Console.WriteLine("Blob existed {0}", t.Result);
                                                             }
                                                         })).ConfigureAwait(false);
        }

        private static void SQLAzureRetry()
        {
            var settings = DatabaseSettings.GetDatabaseSettings(
                new SystemConfigurationSource());
            
            // Create a retry policy that uses a default retry strategy from the 
            // configuration.
            var retryPolicy = RetryPolicyFactory.GetDefaultSqlConnectionRetryPolicy();

            using (var conn =
                new ReliableSqlConnection(
                    ConfigurationManager.ConnectionStrings[settings.DefaultDatabase].ConnectionString, retryPolicy))
            {
                // Attempt to open a connection using the retry policy specified
                // when the constructor is invoked.    
                conn.Open();
                // ... execute SQL queries against this connection ...
            }
        }
    }
}
