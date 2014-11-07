using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace PerfIt.Handlers
{
    public class AverageTimeHandler : CounterHandlerBase
    {

        private const string AverageTimeTakenTicksKey = "AverageTimeHandler_#_StopWatch_#_";
        
        protected ConcurrentDictionary<string, Lazy<PerformanceCounter>> _counters;
        protected ConcurrentDictionary<string, Lazy<PerformanceCounter>> _baseCounters;
        

        public AverageTimeHandler(
            string categoryName,
            string instanceName)
            : base(categoryName,instanceName)
        {
            _counters = new ConcurrentDictionary<string, Lazy<PerformanceCounter>>();
            _baseCounters = new ConcurrentDictionary<string, Lazy<PerformanceCounter>>();
        }

        public override string CounterType
        {
            get { return CounterTypes.AverageTimeTaken; }
        }

        protected override void DoOnRequestStarting(IPerfItContext context)
        {
            context.Data.Add(AverageTimeTakenTicksKey + _instanceName, Stopwatch.StartNew());
        }

        protected override void DoOnRequestEnding(IPerfItContext context)
        {

            //ensure instance counters exist
            BuildCounters(context.InstanceNameSuffix);
            var currentInstanceName = PerfItRuntime.GetCounterInstanceNameWithSuffix(GetInstanceName(), context.InstanceNameSuffix);

            var sw = (Stopwatch)context.Data[AverageTimeTakenTicksKey + _instanceName];
            sw.Stop();
            _counters[currentInstanceName].Value.IncrementBy(sw.ElapsedTicks);
            _baseCounters[currentInstanceName].Value.Increment();
        }

        protected override void BuildCounters(string instanceNameSuffix, bool newInstanceName = false)
        {
            var currentInstanceName=PerfItRuntime.GetCounterInstanceNameWithSuffix(GetInstanceName(newInstanceName) , instanceNameSuffix);
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
                }
          );
                _counters.AddOrUpdate(currentInstanceName,
                                       _counter,
                                       (key, existingCounter) => existingCounter);
            }

            if (!_baseCounters.Keys.Contains(currentInstanceName))
            {
               var  _baseCounter = new Lazy<PerformanceCounter>(() =>
                {
                    var counter = new PerformanceCounter()
                    {
                        CategoryName = _categoryName,
                        CounterName = GetBaseCounterName(),
                        InstanceName = currentInstanceName,
                        ReadOnly = false,
                        InstanceLifetime = PerformanceCounterInstanceLifetime.Process
                    };
                    counter.RawValue = 0;
                    return counter;
                }
                    );
               _baseCounters.AddOrUpdate(currentInstanceName,
                                          _baseCounter,
                                          (key, existingCounter) => existingCounter);
            }
        }

        protected override CounterCreationData[] DoGetCreationData()
        {
            var counterCreationDatas = new CounterCreationData[2];
            counterCreationDatas[0] = new CounterCreationData()
                                          {
                                              CounterType = PerformanceCounterType.AverageTimer32,
                                              CounterName = Name,
                                              CounterHelp = "Average seconds taken to execute"
                                          };
            counterCreationDatas[1] = new CounterCreationData()
                                          {
                                              CounterType = PerformanceCounterType.AverageBase,
                                              CounterName = GetBaseCounterName(),
                                              CounterHelp = "Average seconds taken to execute"
                                          };
            return counterCreationDatas;
        }

        private string GetBaseCounterName()
        {
            return "Total " + Name + " (base)";
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
            foreach (var counter in _baseCounters.Values)
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
