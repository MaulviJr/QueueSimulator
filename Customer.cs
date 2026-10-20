namespace QueueingCalculator
{
    public class Customer
    {
        public double ArrivalTime;
        public double StartTime;
        public double ServiceTime;
        public double EndTime;

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
