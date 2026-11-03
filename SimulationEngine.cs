using System;
using System.Collections.Generic;
using System.Linq;

namespace QueueingCalculator
{
    // What the user types on the Simulation tab.
    public class SimSettings
    {
        public double ArrivalRate;       // customers per minute for the WHOLE shop (lambda)
        public double MeanService;       // minutes per customer, pooled over all counters (1 / mu)
        public int Servers = 1;          // number of counters (c), each with its OWN queue
        public double Duration = 88;     // customers keep arriving until this time (minutes)
    }

    // One point of a step graph (the value holds from Time until the next point).
    public class StepPoint
    {
        public double Time;
        public double Value;
    }

    // Everything one run produces.
    public class SimResult
    {
        public List<Customer> Customers = new List<Customer>();
        public int Servers;

        public double TotalTime;            // first start to last end
        public double[] BusyTime;           // per counter (index 0 = counter 1)
        public int[] Served;                // per counter
        public double[] Utilization;        // per counter = busy time / total time (busy % of the clock)
        public double[] ShareOfWork;        // per counter = its service time / service time of ALL counters
                                            // (the professor's utilization: always adds up to 1)
        public double TotalUtilization;     // total busy time / (c x total time)

        public double AvgInterArrival;
        public double AvgService;
        public double AvgWait;              // Wq  (same as response time here)
        public double AvgTurnaround;        // W
        public int WaitedCount;             // customers with wait > 0
        public double AvgQueueLength;       // Lq, time-average of the queue graph (all lines together)
        public double AvgInSystem;          // L,  time-average of the system graph
        public int MaxQueue;                // longest total number waiting (all lines together)
        public int[] MaxQueuePerCounter;    // longest line in front of each counter (people waiting, not being served)

        public List<StepPoint> QueueSteps = new List<StepPoint>();    // total waiting over time
        public List<StepPoint> SystemSteps = new List<StepPoint>();   // customers in system over time
    }

    public static class SimulationEngine
    {
        // ---------- random number generation (the "dice") ----------

        // Cumulative Poisson table with the given mean. Built until it reaches (almost) 1.
        // This is the "Cumulative probability" column of the class table.
        static List<double> PoissonTable(double mean)
        {
            List<double> cum = new List<double>();
            double p = Math.Exp(-mean);      // P(0)
            double sum = 0;

            for (int k = 0; k < 2000; k++)
            {
                if (k > 0)
                {
                    p = p * mean / k;        // P(k) = P(k-1) * mean / k  (no factorial overflow)
                }
                sum += p;
                cum.Add(sum);
                if (sum >= 0.999999)
                {
                    break;
                }
            }
            return cum;
        }

        // Roll the dice: first row whose cumulative probability is >= RAND() (the "lookup").
        static int PoissonGap(List<double> table, Random rng)
        {
            double u = rng.NextDouble();
            for (int k = 0; k < table.Count; k++)
            {
                if (u <= table[k])
                {
                    return k;
                }
            }
            return table.Count - 1;
        }

        // Inverse transform for the exponential: -mean x LN(1 - RAND())
        static double ExpTime(double mean, Random rng)
        {
            return -mean * Math.Log(1 - rng.NextDouble());
        }

        // ---------- the simulation ----------

        public static SimResult Run(SimSettings s, Random rng)
        {
            int c = s.Servers;
            SimResult r = new SimResult();
            r.Servers = c;

            // STEP 1: generate the customers (arrival time and service time).
            // Customer 1 arrives at time 0, as in the class example.
            double meanGap = 1 / s.ArrivalRate;
            List<double> table = PoissonTable(meanGap);

            double t = 0;
            int id = 1;
            while (t <= s.Duration)
            {
                // service time in whole minutes, like ROUND(...,0) in Excel
                double service = Math.Round(ExpTime(s.MeanService, rng), MidpointRounding.AwayFromZero);

                Customer cust = new Customer();
                cust.Id = id;
                cust.Stream = "Normal";
                cust.ArrivalTime = t;
                cust.ServiceTime = service;
                cust.Outcome = "Purchased";
                r.Customers.Add(cust);
                id++;

                t += PoissonGap(table, rng);     // gap to the next arrival, from the Poisson table
            }

            // STEP 2: every counter has its OWN line (like in the shop).
            // When a customer arrives he looks at all the lines and joins the SHORTEST one
            // (people waiting + the person being served). An idle counter has 0 people, so it is
            // chosen first. If two lines are equally short, the lowest-numbered counter wins
            // (the rule from the lecture). After joining, the customer never changes line.
            // Inside one line the service is first come, first served.
            //
            // endTimes[i] = end times of everybody who joined line i so far.
            // freeAt[i]   = the time counter i+1 finishes the last customer in its line.
            List<double>[] endTimes = new List<double>[c];
            for (int i = 0; i < c; i++)
            {
                endTimes[i] = new List<double>();
            }
            double[] freeAt = new double[c];

            foreach (Customer cust in r.Customers)     // already in arrival order
            {
                int k = 0;
                int fewest = int.MaxValue;

                for (int i = 0; i < c; i++)
                {
                    // people still in line i at this moment = those who have not finished yet
                    int inLine = 0;
                    foreach (double end in endTimes[i])
                    {
                        if (end > cust.ArrivalTime)
                        {
                            inLine++;
                        }
                    }

                    if (inLine < fewest)       // "<" (not "<=") keeps the lowest number when tied
                    {
                        fewest = inLine;
                        k = i;
                    }
                }

                cust.Server = k + 1;
                cust.StartTime = Math.Max(cust.ArrivalTime, freeAt[k]);
                cust.EndTime = cust.StartTime + cust.ServiceTime;
                freeAt[k] = cust.EndTime;
                endTimes[k].Add(cust.EndTime);
            }

            // STEP 3: performance measures.
            int n = r.Customers.Count;
            double firstStart = r.Customers.Min(x => x.StartTime);
            double lastEnd = r.Customers.Max(x => x.EndTime);
            r.TotalTime = lastEnd - firstStart;

            r.BusyTime = new double[c];
            r.Served = new int[c];
            r.Utilization = new double[c];
            double totalBusy = 0;

            foreach (Customer cust in r.Customers)
            {
                r.BusyTime[cust.Server - 1] += cust.ServiceTime;
                r.Served[cust.Server - 1]++;
                totalBusy += cust.ServiceTime;
                if (cust.WaitingTime > 0)
                {
                    r.WaitedCount++;
                }
            }

            for (int i = 0; i < c; i++)
            {
                if (r.TotalTime > 0)
                {
                    r.Utilization[i] = r.BusyTime[i] / r.TotalTime;
                }
            }
            if (r.TotalTime > 0)
            {
                r.TotalUtilization = totalBusy / (c * r.TotalTime);
            }

            // Professor's method: counter's total service time / sum of service times of all counters.
            r.ShareOfWork = new double[c];
            if (totalBusy > 0)
            {
                for (int i = 0; i < c; i++)
                {
                    r.ShareOfWork[i] = r.BusyTime[i] / totalBusy;
                }
            }

            r.AvgService = r.Customers.Average(x => x.ServiceTime);
            r.AvgWait = r.Customers.Average(x => x.WaitingTime);
            r.AvgTurnaround = r.Customers.Average(x => x.TurnaroundTime);
            if (n > 1)
            {
                r.AvgInterArrival = (r.Customers[n - 1].ArrivalTime - r.Customers[0].ArrivalTime) / (n - 1);
            }

            // Queue length and number-in-system graphs.
            // A customer is in a queue from arrival until start, and in the system from arrival until end.
            // The total queue graph adds up all the lines. We also keep the events of each line separately.
            List<KeyValuePair<double, int>> queueEvents = new List<KeyValuePair<double, int>>();
            List<KeyValuePair<double, int>> systemEvents = new List<KeyValuePair<double, int>>();
            List<KeyValuePair<double, int>>[] lineEvents = new List<KeyValuePair<double, int>>[c];
            for (int i = 0; i < c; i++)
            {
                lineEvents[i] = new List<KeyValuePair<double, int>>();
            }

            foreach (Customer cust in r.Customers)
            {
                if (cust.StartTime > cust.ArrivalTime)
                {
                    queueEvents.Add(new KeyValuePair<double, int>(cust.ArrivalTime, 1));
                    queueEvents.Add(new KeyValuePair<double, int>(cust.StartTime, -1));
                    lineEvents[cust.Server - 1].Add(new KeyValuePair<double, int>(cust.ArrivalTime, 1));
                    lineEvents[cust.Server - 1].Add(new KeyValuePair<double, int>(cust.StartTime, -1));
                }
                systemEvents.Add(new KeyValuePair<double, int>(cust.ArrivalTime, 1));
                systemEvents.Add(new KeyValuePair<double, int>(cust.EndTime, -1));
            }

            double queueArea;
            double systemArea;
            r.QueueSteps = BuildSteps(queueEvents, lastEnd, out queueArea);
            r.SystemSteps = BuildSteps(systemEvents, lastEnd, out systemArea);

            if (lastEnd > 0)
            {
                r.AvgQueueLength = queueArea / lastEnd;
                r.AvgInSystem = systemArea / lastEnd;
            }
            r.MaxQueue = (int)r.QueueSteps.Max(p => p.Value);

            r.MaxQueuePerCounter = new int[c];
            for (int i = 0; i < c; i++)
            {
                double area;
                List<StepPoint> steps = BuildSteps(lineEvents[i], lastEnd, out area);
                r.MaxQueuePerCounter[i] = (int)steps.Max(p => p.Value);
            }

            return r;
        }

        // Turns "+1 / -1 at time t" events into a step graph, and also returns the area under it
        // (area / total time = time-average level).
        static List<StepPoint> BuildSteps(List<KeyValuePair<double, int>> events, double endTime, out double area)
        {
            events.Sort((a, b) => a.Key.CompareTo(b.Key));

            List<StepPoint> steps = new List<StepPoint>();
            steps.Add(new StepPoint { Time = 0, Value = 0 });

            int level = 0;
            double last = 0;
            area = 0;
            int i = 0;

            while (i < events.Count)
            {
                double time = events[i].Key;
                area += level * (time - last);

                while (i < events.Count && events[i].Key == time)
                {
                    level += events[i].Value;
                    i++;
                }

                steps.Add(new StepPoint { Time = time, Value = level });
                last = time;
            }

            if (endTime > last)
            {
                area += level * (endTime - last);
            }
            return steps;
        }
    }
}