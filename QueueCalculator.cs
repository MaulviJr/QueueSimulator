namespace QueueingCalculator
{
    public class QueueCalculator
    {
        public double Lambda;
        public double Mu;
        public double Rho;
        public double Idle;
        public double MeanArrival;
        public double Ca2;
        public double Cs2;
        public double Lq;
        public double L;
        public double Wq;
        public double W;
        public Customer AverageCustomer;

        void BuildAverageCustomer()
        {
            AverageCustomer = new Customer();
            AverageCustomer.ArrivalTime = 0;
            AverageCustomer.StartTime = Wq;
            AverageCustomer.ServiceTime = 1 / Mu;
            AverageCustomer.EndTime = W;
        }

        public bool MM1(double lambda, double mu)
        {
            Lambda = lambda;
            Mu = mu;
            Rho = Lambda / Mu;

            if (Rho >= 1)
            {
                return false;
            }

            Idle = 1 - Rho;
            Lq = (Rho * Rho) / (1 - Rho);
            Wq = Lq / Lambda;
            W = Wq + 1 / Mu;
            L = Lambda * W;
            BuildAverageCustomer();
            return true;
        }

        public bool MG1(double lambda, double meanService, double varService)
        {
            Lambda = lambda;
            Mu = 1 / meanService;
            Rho = Lambda / Mu;

            if (Rho >= 1)
            {
                return false;
            }

            Idle = 1 - Rho;
            Lq = (Lambda * Lambda * varService + Rho * Rho) / (2 * (1 - Rho));
            Wq = Lq / Lambda;
            W = Wq + 1 / Mu;
            L = Lambda * W;
            BuildAverageCustomer();
            return true;
        }

        public bool GG1(double meanArrival, double varArrival, double meanService, double varService)
        {
            MeanArrival = meanArrival;
            Lambda = 1 / meanArrival;
            Mu = 1 / meanService;
            Rho = Lambda / Mu;

            if (Rho >= 1)
            {
                return false;
            }

            Idle = 1 - Rho;
            Ca2 = varArrival / (meanArrival * meanArrival);
            Cs2 = varService / (meanService * meanService);

            Lq = (Rho * Rho * (1 + Cs2) * (Ca2 + Rho * Rho * Cs2)) / (2 * (1 - Rho) * (1 + Rho * Rho * Cs2));
            Wq = Lq / Lambda;
            W = Wq + 1 / Mu;
            L = Lambda * W;
            BuildAverageCustomer();
            return true;
        }
    }
}
