﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using log4net;

namespace Check_Up.Util {
    class ProcessesDataCollection {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Maximum Values
        public Dictionary<string, float> HighestCpuUsage = new Dictionary<string, float>();
        public Dictionary<string, float> HighestMemUsage = new Dictionary<string, float>();

        List<PerformanceCounter> ProcessPerfCounters = new List<PerformanceCounter>();

        public ProcessesDataCollection() {
            
            foreach (var p in Process.GetProcesses()) {
                var cpuCounter = ProcessMonitor.GetPerfCounterForProcessId(p.Id, "% Processor Time");
                var memCounter = ProcessMonitor.GetPerfCounterForProcessId(p.Id, "Working Set - Private");

                if (cpuCounter != null) {
                    cpuCounter.NextValue();
                    ProcessPerfCounters.Add(cpuCounter);
                }

                if (memCounter != null) {
                    memCounter.NextValue();
                    ProcessPerfCounters.Add(memCounter);
                } 
            }


        }

        public void GatherData(bool FirstRun) {

            for (int i = 0; i < ProcessPerfCounters.Count; i++ ) {
                float data;
                PerformanceCounter counter = ProcessPerfCounters[i];

                try {
                    data = counter.NextValue();
                }
                catch {
                    Console.WriteLine("Could not get information on process {0}", counter.InstanceName);
                    Console.WriteLine("Removing process {0}", counter.InstanceName);

                    ProcessPerfCounters.Remove(counter);
                    continue;
                }

                if (!FirstRun) {
                    if (counter.CounterName == "% Processor Time") {

                        data = data / (float)RandomInfo.logicalCpuCount;
                        HighestCpuUsage[counter.InstanceName] = data;
                    }

                    if (counter.CounterName == "Working Set - Private") {

                        HighestMemUsage[counter.InstanceName] = data / 1024 / 1024;
                    }
                }
            }

            if (!FirstRun) {
                HighestCpuUsage = AggregateData(HighestCpuUsage);
                HighestMemUsage = AggregateData(HighestMemUsage);
            }

            Console.WriteLine("Finished gathering process data");

        }



        private void CheckData(string type, PerformanceCounter counter, float data, ref Dictionary<string, float> DataUsage) {
            List<string> keys = new List<string>(DataUsage.Keys);

            string lowest_key = keys[0];
            float lowest_value = DataUsage[keys[0]];

            foreach (string key in keys) {

                if (DataUsage[key] < lowest_value && key != lowest_key) {
                    lowest_key = key;
                    lowest_value = DataUsage[key];
                }
            }

            if (lowest_value < data) {
                DataUsage.Remove(lowest_key);
                DataUsage[counter.InstanceName] = data;
            }
        }

        private Dictionary<string, float> AggregateData(Dictionary<string, float> DataUsage) {
            List<string> keys = new List<string>(DataUsage.Keys);
            Dictionary<string, float> NewData = new Dictionary<string,float>();

            foreach (string key in keys) {
                int PoundIndex = key.IndexOf('#');
                if (PoundIndex != -1) {
                    string newKey = key.Substring(0, PoundIndex);
                    if (!NewData.ContainsKey(newKey)) {
                        NewData[newKey] = DataUsage[key];
                    }
                    else {                        
                        NewData[newKey] = NewData[newKey] + DataUsage[key];
                    }
                }
                else {
                    if (NewData.ContainsKey(key)) {
                        NewData[key] = NewData[key] + DataUsage[key];
                    }
                    else {
                        NewData[key] = DataUsage[key];
                    }
                    
                }
            }

            return NewData;
        }

    }
}
