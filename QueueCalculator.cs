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
        public int Servers = 1;
        public double P0;
        public double ProbWait;
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
            Servers = 1;
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

        // M/M/c: c identical servers, one shared queue.
        // Rho is the utilization of each server: lambda / (c * mu).
        public bool MMc(double lambda, double mu, int c)
        {
            Servers = c;
            Lambda = lambda;
            Mu = mu;
            Rho = Lambda / (c * Mu);

            if (Rho >= 1)
            {
                return false;
            }

            double a = Lambda / Mu;      // offered load
            double sum = 0;              // sum of a^n / n! for n = 0 .. c-1
            double term = 1;             // a^n / n!, built step by step so nothing overflows

            for (int n = 0; n < c; n++)
            {
                if (n > 0)
                {
                    term = term * a / n;
                }
                sum += term;
            }

            double lastTerm = term * a / c;                // a^c / c!
            double waitPart = lastTerm / (1 - Rho);        // a^c / (c! (1 - rho))

            P0 = 1 / (sum + waitPart);
            ProbWait = waitPart * P0;                      // Erlang C: probability a customer has to wait

            Idle = 1 - Rho;
            Lq = ProbWait * Rho / (1 - Rho);
            Wq = Lq / Lambda;
            W = Wq + 1 / Mu;
            L = Lambda * W;
            BuildAverageCustomer();
            return true;
        }

        public bool MG1(double lambda, double meanService, double varService)
        {
            Servers = 1;
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
            Servers = 1;
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