using System;
using System.Collections.Generic;

namespace QueueingCalculator
{
    // The inputs for ONE stream of customers (Normal or Express).
    public class StreamSettings
    {
        public string Name = "";
        public double Lambda;        // arrival rate (customers per minute)
        public double MeanService;   // mean service time (minutes)
        public double VarService;    // variance of service time
        public int Servers = 1;      // number of counters open for this stream
    }

    // The results for ONE stream.
    public class StreamSummary
    {
        public StreamSettings Settings;
        public int Arrived;
        public int Served;           // reached a counter (Purchased + Payment failed)
        public int Reneged;
        public int PaymentFailed;
        public double AvgWait;       // average wait of customers who reached a counter
        public double AvgService;
        public double AvgTurnaround;
        public double MaxWait;
        public double[] Utilization; // one value per counter: busy time / simulation length
    }

    public class SimulationEngine
    {
        // Settings that apply to every stream
        public double Duration = 87;           // simulated minutes
        public double Patience = 0;            // longest wait a customer accepts; 0 = nobody reneges
        public double PayFailProb = 0;         // chance that payment fails (0 to 1)
        public bool ExponentialService = true; // true = exponential, false = normal (uses the variance)

        // Results
        public List<Customer> Customers = new List<Customer>();
        public List<StreamSummary> Summaries = new List<StreamSummary>();

        Random rng;

        // seed = 0 gives a different run every time; any other number repeats the same run.
        public SimulationEngine(int seed)
        {
            if (seed == 0)
            {
                rng = new Random();
            }
            else
            {
                rng = new Random(seed);
            }
        }

        // Exponential random number with the given mean (inverse transform method).
        double RandomExponential(double mean)
        {
            double u = 1 - rng.NextDouble();    // between 0 (not included) and 1
            return -mean * Math.Log(u);
        }

        // Normal random number (Box-Muller method). Redraws until the value is above 0.
        double RandomNormal(double mean, double variance)
        {
            double x;
            do
            {
                double u1 = 1 - rng.NextDouble();
                double u2 = rng.NextDouble();
                double z = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
                x = mean + Math.Sqrt(variance) * z;
            }
            while (x <= 0);
            return x;
        }

        double RandomService(StreamSettings s)
        {
            if (ExponentialService)
            {
                return RandomExponential(s.MeanService);
            }
            return RandomNormal(s.MeanService, s.VarService);
        }

        // Runs all streams. Each stream has its own customers and its own counters.
        public void Run(List<StreamSettings> streams)
        {
            Customers.Clear();
            Summaries.Clear();

            foreach (StreamSettings s in streams)
            {
                Summaries.Add(RunStream(s));
            }

            // Show all customers in the order they arrived
            Customers.Sort((x, y) => x.ArrivalTime.CompareTo(y.ArrivalTime));
            for (int i = 0; i < Customers.Count; i++)
            {
                Customers[i].Id = i + 1;
            }
        }

        StreamSummary RunStream(StreamSettings s)
        {
            double[] freeAt = new double[s.Servers];   // time when each counter becomes free
            double[] busy = new double[s.Servers];     // total busy time of each counter

            StreamSummary sum = new StreamSummary();
            sum.Settings = s;

            double totalWait = 0;
            double totalService = 0;
            double totalTurnaround = 0;
            double clock = 0;

            while (true)
            {
                // 1. Next arrival: add an exponential inter-arrival time (Poisson arrivals)
                clock = clock + RandomExponential(1 / s.Lambda);
                if (clock > Duration)
                {
                    break;
                }

                Customer c = new Customer();
                c.Stream = s.Name;
                c.ArrivalTime = clock;
                sum.Arrived++;

                // 2. Pick the counter that becomes free first (one shared FIFO queue)
                int best = 0;
                for (int i = 1; i < s.Servers; i++)
                {
                    if (freeAt[i] < freeAt[best])
                    {
                        best = i;
                    }
                }

                double start = Math.Max(clock, freeAt[best]);

                // 3a. Customer gives up if the wait is longer than the patience
                if (Patience > 0 && start - clock > Patience)
                {
                    c.StartTime = clock + Patience;
                    c.EndTime = clock + Patience;
                    c.ServiceTime = 0;
                    c.Server = 0;
                    c.Outcome = "Reneged";
                    sum.Reneged++;
                    Customers.Add(c);
                    continue;
                }

                // 3b. Customer is served
                double service = RandomService(s);
                double end = start + service;

                c.StartTime = start;
                c.ServiceTime = service;
                c.EndTime = end;
                c.Server = best + 1;

                if (rng.NextDouble() < PayFailProb)
                {
                    c.Outcome = "Payment failed";
                    sum.PaymentFailed++;
                }
                else
                {
                    c.Outcome = "Purchased";
                }

                // 4. Update the counter and the totals
                freeAt[best] = end;
                busy[best] += Math.Max(0, Math.Min(end, Duration) - start);   // only count time inside the run

                sum.Served++;
                totalWait += c.WaitingTime;
                totalService += service;
                totalTurnaround += c.TurnaroundTime;
                if (c.WaitingTime > sum.MaxWait)
                {
                    sum.MaxWait = c.WaitingTime;
                }

                Customers.Add(c);
            }

            if (sum.Served > 0)
            {
                sum.AvgWait = totalWait / sum.Served;
                sum.AvgService = totalService / sum.Served;
                sum.AvgTurnaround = totalTurnaround / sum.Served;
            }

            sum.Utilization = new double[s.Servers];
            for (int i = 0; i < s.Servers; i++)
            {
                sum.Utilization[i] = busy[i] / Duration;
            }

            return sum;
        }
    }
}