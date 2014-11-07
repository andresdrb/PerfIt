using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;

namespace PerfIt.Handlers
{
    public class TotalCountHandler : CounterHandlerBase
    {

        protected ConcurrentDictionary<string, Lazy<PerformanceCounter>> _counters;

        public TotalCountHandler
            (
            string categoryName,
            string instanceName)
            : base(categoryName, instanceName)
        {
            _counters = new ConcurrentDictionary<string, Lazy<PerformanceCounter>>();
            
        }

        public override string CounterType
        {
            get { return CounterTypes.TotalNoOfOperations; }
        }

        protected override void DoOnRequestStarting(IPerfItContext context)
        {
            // nothing 
        }

        protected override void DoOnRequestEnding(IPerfItContext context)
        {
            //ensure instance counters exist
            BuildCounters(context.InstanceNameSuffix);

            var currentInstanceName = PerfItRuntime.GetCounterInstanceNameWithSuffix(GetInstanceName(), context.InstanceNameSuffix);

            _counters[currentInstanceName].Value.Increment();
        }

        protected override void BuildCounters(string instanceNameSuffix,bool newInstanceName = false)
        {

            var currentInstanceName = PerfItRuntime.GetCounterInstanceNameWithSuffix(GetInstanceName(), instanceNameSuffix);
            if (!_counters.Keys.Contains(currentInstanceName))
            {
                var _counter = new Lazy<PerformanceCounter>(() =>
                {
                    var counter = new PerformanceCounter()
                    {
                        CategoryName = _categoryName,
                        CounterName = Name,
                        InstanceName = currentInstanceName,
                        ReadOnly = false,
                        InstanceLifetime = PerformanceCounterInstanceLifetime.Process
                    };
                    counter.RawValue = 0;
                    return counter;
                });


                _counters.AddOrUpdate(currentInstanceName,
                    _counter,
                    (key, existingCounter) => existingCounter);
            }
        }

        protected override CounterCreationData[] DoGetCreationData()
        {
            return new []
                       {
                           new CounterCreationData()
                               {
                                   CounterName = Name,
                                   CounterType = PerformanceCounterType.NumberOfItems32,
                                   CounterHelp = "Total # of operations"
                               }
                       };
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var counter in _counters.Values)
            {
                if (counter != null && counter.IsValueCreated)
                {
                    counter.Value.RemoveInstance();
                    counter.Value.Dispose();
                }
            }
            
        }

        
    }
}
