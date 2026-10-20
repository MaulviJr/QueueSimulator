namespace QueueingCalculator
{
    // One customer. Used by the calculator ("average customer") and by the simulation (one row per customer).
    // These are properties (not plain fields) so the DataGrid in the Simulation tab can show them.
    public class Customer
    {
        public int Id { get; set; }
        public string Stream { get; set; } = "";     // "Normal" or "Express"
        public double ArrivalTime { get; set; }
        public double StartTime { get; set; }
        public double ServiceTime { get; set; }
        public double EndTime { get; set; }
        public int Server { get; set; }              // counter number inside its stream (0 = never reached a counter)
        public string Outcome { get; set; } = "";    // Purchased, Payment failed, or Reneged

        public double WaitingTime
        {
            get { return StartTime - ArrivalTime; }
        }

        public double TurnaroundTime
        {
            get { return EndTime - ArrivalTime; }
        }
    }
}