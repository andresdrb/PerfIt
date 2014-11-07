using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace PerfIt.Handlers
{
    public class LastOperationExecutionTimeHandler : CounterHandlerBase
    {
        private const string TimeTakenTicksKey = "LastOperationExecutionTimeHandler_#_StopWatch_#_";
        protected ConcurrentDictionary<string,Lazy<PerformanceCounter>> _counters;
        

        public LastOperationExecutionTimeHandler(
            string categoryName,
            string instanceName)
            : base(categoryName, instanceName)
        {
            _counters = new ConcurrentDictionary<string, Lazy<PerformanceCounter>>();
        }

       
        public override string CounterType
        {
            get { return CounterTypes.LastOperationExecutionTime; }
        }

        protected override void DoOnRequestStarting(IPerfItContext context)
        {
            context.Data.Add(TimeTakenTicksKey + _instanceName, Stopwatch.StartNew());
        }

        protected override void DoOnRequestEnding(IPerfItContext context)
        {
            //ensure instance counters exist
            BuildCounters(context.InstanceNameSuffix);

            var currentInstanceName = PerfItRuntime.GetCounterInstanceNameWithSuffix(GetInstanceName(), context.InstanceNameSuffix);

            var sw = (Stopwatch)context.Data[TimeTakenTicksKey + _instanceName];
            sw.Stop();
            _counters[currentInstanceName].Value.RawValue = sw.ElapsedMilliseconds;
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
            var counterCreationDatas = new CounterCreationData[1];
            counterCreationDatas[0] = new CounterCreationData()
            {
                CounterType = PerformanceCounterType.NumberOfItems32,
                CounterName = Name,
                CounterHelp = "Time in ms to run last request"
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
