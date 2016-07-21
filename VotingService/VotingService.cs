using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Health;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using VotingService.Controllers;

namespace VotingService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class VotingService : StatelessService
    {
        #region fields

        private FabricClient _client;
        private Timer _healthTimer;

        private TimeSpan _interval = TimeSpan.FromSeconds(30);
        private long _lastCount;
        private DateTime _lastReport = DateTime.UtcNow;

        #endregion

        #region constructors

        public VotingService(StatelessServiceContext context)
            : base(context)
        {
            // Create the timer here, so we can do a change operation on it later, avoiding creating/disposing of the 
            // timer.
            _healthTimer = new Timer(ReportHealthAndLoad, null, Timeout.Infinite, Timeout.Infinite);
        }

        #endregion

        #region methods

        public void ReportHealthAndLoad(object notused)
        {
            // Calculate the values and then remember current values for the next report.
            var total = VotesController.RequestCount;
            var diff = total - _lastCount;
            var duration = Math.Max((long) DateTime.UtcNow.Subtract(_lastReport).TotalSeconds, 1L);
            var rps = diff/duration;
            _lastCount = total;
            _lastReport = DateTime.UtcNow;

            // Create the health information for this instance of the service and send report to Service Fabric.
            var hi = new HealthInformation("VotingServiceHealth", "Heartbeat", HealthState.Ok)
            {
                TimeToLive = _interval.Add(_interval),
                Description = $"{diff} requests since last report. RPS: {rps} Total requests: {total}.",
                RemoveWhenExpired = false,
                SequenceNumber = HealthInformation.AutoSequenceNumber
            };

            var sshr = new StatelessServiceInstanceHealthReport(Context.PartitionId, Context.InstanceId, hi);
            _client.HealthManager.ReportHealth(sshr);

            // Report the load
            Partition.ReportLoad(new[] {new LoadMetric("RPS", (int) rps)});
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener(
                    serviceContext =>
                        new OwinCommunicationListener(Startup.ConfigureApp, serviceContext, ServiceEventSource.Current,
                            "ServiceEndpoint"))
            };
        }

        protected override Task OnOpenAsync(CancellationToken cancellationToken)
        {
            _client = new FabricClient();
            _healthTimer = new Timer(ReportHealthAndLoad, null, _interval, _interval);

            return base.OnOpenAsync(cancellationToken);
        }

        #endregion
    }
}