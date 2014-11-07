using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace PerfIt.Handlers
{
    public class NumberOfOperationsPerSecondHandler : CounterHandlerBase
    {
        
        private const string TimeTakenTicksKey = "NumberOfOperationsPerSecondHandler_#_StopWatch_#_";
        protected ConcurrentDictionary<string, Lazy<PerformanceCounter>> _counters;

        public NumberOfOperationsPerSecondHandler
            (
            string categoryName,
            string instanceName)
            : base(categoryName, instanceName)
        {
            _counters = new ConcurrentDictionary<string, Lazy<PerformanceCounter>>();
           
        }

        public override string CounterType
        {
            get { return CounterTypes.NumberOfOperationsPerSecond; }
        }

        protected override void DoOnRequestStarting(IPerfItContext context)
        {
           
        }

        protected override void DoOnRequestEnding(IPerfItContext context)
        {
            //ensure instance counters exist
            BuildCounters(context.InstanceNameSuffix);

            var currentInstanceName = PerfItRuntime.GetCounterInstanceNameWithSuffix(GetInstanceName(), context.InstanceNameSuffix);

            _counters[currentInstanceName].Value.Increment();
        }

        protected override void BuildCounters( string instanceNameSuffix, bool newInstanceName = false)
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
            var counterCreationDatas = new CounterCreationData[1];
            counterCreationDatas[0] = new CounterCreationData()
            {
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32,
                CounterName = Name,
                CounterHelp = "# of operations / sec"
            };

            return counterCreationDatas;
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
