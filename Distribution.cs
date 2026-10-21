using System;

namespace QueueingCalculator
{
    public enum DistType
    {
        Exponential,
        Gamma,
        Normal,
        Uniform
    }

    // Holds the mean and variance of a time distribution.
    // The user types only the distribution parameters; the variance is calculated here.
    public class Distribution
    {
        public double Mean;
        public double Variance;

        // Exponential: variance = mean^2
        public static Distribution Exponential(double mean)
        {
            Distribution d = new Distribution();
            d.Mean = mean;
            d.Variance = mean * mean;
            return d;
        }

        // Gamma with shape K and scale theta: mean = K * theta, variance = K * theta^2
        public static Distribution Gamma(double k, double theta)
        {
            Distribution d = new Distribution();
            d.Mean = k * theta;
            d.Variance = k * theta * theta;
            return d;
        }

        // Normal: the user gives the mean and the standard deviation, variance = sd^2
        public static Distribution Normal(double mean, double sd)
        {
            Distribution d = new Distribution();
            d.Mean = mean;
            d.Variance = sd * sd;
            return d;
        }

        // Uniform between min and max: mean = (min + max) / 2, variance = (max - min)^2 / 12
        public static Distribution Uniform(double min, double max)
        {
            Distribution d = new Distribution();
            d.Mean = (min + max) / 2;
            d.Variance = (max - min) * (max - min) / 12;
            return d;
        }
    }
}
