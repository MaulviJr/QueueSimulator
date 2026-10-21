using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QueueingCalculator
{
    // The Calculator tab. (The Simulation tab code is in MainWindow.Simulation.cs.)
    public partial class MainWindow : Window
    {
        QueueCalculator calc = new QueueCalculator();
        bool useRates = true;
        bool loading = true;     // true while the window is being set up

        public MainWindow()
        {
            InitializeComponent();

            cmbModel.Items.Add("M/M/1");
            cmbModel.Items.Add("M/M/c");
            cmbModel.Items.Add("M/G/1");
            cmbModel.Items.Add("G/G/1");

            string[] dists = { "Exponential", "Gamma", "Normal", "Uniform" };
            foreach (string d in dists)
            {
                cmbArrDist.Items.Add(d);
                cmbSvcDist.Items.Add(d);
            }

            cmbArrDist.SelectedIndex = 0;
            cmbSvcDist.SelectedIndex = 1;
            cmbModel.SelectedIndex = 0;

            loading = false;
            ShowFields();
        }

        ModelType GetModel()
        {
            if (cmbModel.SelectedIndex == 1)
            {
                return ModelType.MMc;
            }
            if (cmbModel.SelectedIndex == 2)
            {
                return ModelType.MG1;
            }
            if (cmbModel.SelectedIndex == 3)
            {
                return ModelType.GG1;
            }
            return ModelType.MM1;
        }

        DistType GetDist(ComboBox box)
        {
            if (box.SelectedIndex == 1)
            {
                return DistType.Gamma;
            }
            if (box.SelectedIndex == 2)
            {
                return DistType.Normal;
            }
            if (box.SelectedIndex == 3)
            {
                return DistType.Uniform;
            }
            return DistType.Exponential;
        }

        void SetField(TextBlock label, TextBox box, string text)
        {
            box.Text = "";

            if (text == null)
            {
                label.Visibility = Visibility.Collapsed;
                box.Visibility = Visibility.Collapsed;
            }
            else
            {
                label.Text = text;
                label.Visibility = Visibility.Visible;
                box.Visibility = Visibility.Visible;
            }
        }

        void SetCombo(TextBlock label, ComboBox box, bool visible)
        {
            if (visible)
            {
                label.Visibility = Visibility.Visible;
                box.Visibility = Visibility.Visible;
            }
            else
            {
                label.Visibility = Visibility.Collapsed;
                box.Visibility = Visibility.Collapsed;
            }
        }

        bool TryRead(TextBox box, bool allowZero, out double value)
        {
            if (!double.TryParse(box.Text, out value))
            {
                return false;
            }
            if (allowZero)
            {
                return value >= 0;
            }
            return value > 0;
        }

        // Whole number of servers, between 1 and 200.
        bool TryReadServers(TextBox box, out int value)
        {
            if (!int.TryParse(box.Text, out value))
            {
                return false;
            }
            return value >= 1 && value <= 200;
        }

        // The labels of the boxes needed for one distribution.
        // role is "Arrival" or "Service".
        List<string> LabelsFor(DistType type, string role)
        {
            List<string> list = new List<string>();

            if (type == DistType.Exponential)
            {
                if (role == "Arrival")
                {
                    if (useRates)
                    {
                        list.Add("Arrival rate (lambda)");
                    }
                    else
                    {
                        list.Add("Mean inter-arrival time E[A]");
                    }
                }
                else
                {
                    if (useRates)
                    {
                        list.Add("Service rate (mu)");
                    }
                    else
                    {
                        list.Add("Mean service time E[S]");
                    }
                }
            }
            else if (type == DistType.Gamma)
            {
                list.Add(role + ": shape K");
                list.Add(role + ": scale theta");
            }
            else if (type == DistType.Normal)
            {
                list.Add(role + ": mean");
                list.Add(role + ": standard deviation");
            }
            else
            {
                list.Add(role + ": minimum");
                list.Add(role + ": maximum");
            }

            return list;
        }

        void ShowFields()
        {
            txtResult.Text = "";
            ModelType model = GetModel();

            if (useRates)
            {
                btnMode.Content = "Input mode: Rates (lambda, mu) - click to switch to Times";
            }
            else
            {
                btnMode.Content = "Input mode: Times (E[A], E[S]) - click to switch to Rates";
            }

            // The distribution lists are only needed for M/G/1 (service) and G/G/1 (arrival and service)
            SetCombo(lblArrDist, cmbArrDist, model == ModelType.GG1);
            SetCombo(lblSvcDist, cmbSvcDist, model == ModelType.MG1 || model == ModelType.GG1);

            List<string> labels = new List<string>();

            if (model == ModelType.MM1)
            {
                lblInfo.Text = "Poisson arrivals, exponential service, one server. Needs the arrival and service values.";
                labels.AddRange(LabelsFor(DistType.Exponential, "Arrival"));
                labels.AddRange(LabelsFor(DistType.Exponential, "Service"));
            }
            else if (model == ModelType.MMc)
            {
                lblInfo.Text = "Poisson arrivals, exponential service, c identical servers with one shared queue. The service value is for ONE server.";
                labels.AddRange(LabelsFor(DistType.Exponential, "Arrival"));
                labels.AddRange(LabelsFor(DistType.Exponential, "Service"));
                labels.Add("Number of servers (c), a whole number from 1 to 200");
            }
            else if (model == ModelType.MG1)
            {
                lblInfo.Text = "Poisson arrivals, general service. Choose the service distribution and type its parameters. The mean and variance of service are calculated for you.";
                labels.AddRange(LabelsFor(DistType.Exponential, "Arrival"));
                labels.AddRange(LabelsFor(GetDist(cmbSvcDist), "Service"));
            }
            else
            {
                lblInfo.Text = "General arrivals, general service (Marchal's approximation). Choose a distribution for arrivals and for service. Means and variances are calculated for you. Choosing Exponential gives the M part.";
                labels.AddRange(LabelsFor(GetDist(cmbArrDist), "Arrival"));
                labels.AddRange(LabelsFor(GetDist(cmbSvcDist), "Service"));
            }

            TextBlock[] lbls = { lbl1, lbl2, lbl3, lbl4 };
            TextBox[] boxes = { txt1, txt2, txt3, txt4 };

            for (int i = 0; i < 4; i++)
            {
                if (i < labels.Count)
                {
                    SetField(lbls[i], boxes[i], labels[i]);
                }
                else
                {
                    SetField(lbls[i], boxes[i], null);
                }
            }
        }

        // Reads one distribution from the boxes, starting at box number 'start' (0 = first box).
        // Calculates the mean and variance. Returns false (after showing a message) if a value is not valid.
        bool ReadDist(DistType type, string role, int start, out Distribution dist)
        {
            dist = null;
            TextBox[] boxes = { txt1, txt2, txt3, txt4 };
            double p1 = 0;
            double p2 = 0;

            if (type == DistType.Exponential)
            {
                if (!TryRead(boxes[start], false, out p1))
                {
                    MessageBox.Show(role + ": please enter a number above 0.");
                    return false;
                }
                double mean = p1;
                if (useRates)
                {
                    mean = 1 / p1;
                }
                dist = Distribution.Exponential(mean);
                return true;
            }

            if (type == DistType.Gamma)
            {
                if (!TryRead(boxes[start], false, out p1) || !TryRead(boxes[start + 1], false, out p2))
                {
                    MessageBox.Show(role + ": shape K and scale theta must be numbers above 0.");
                    return false;
                }
                dist = Distribution.Gamma(p1, p2);
                return true;
            }

            if (type == DistType.Normal)
            {
                if (!TryRead(boxes[start], false, out p1) || !TryRead(boxes[start + 1], true, out p2))
                {
                    MessageBox.Show(role + ": the mean must be above 0 and the standard deviation 0 or more.");
                    return false;
                }
                dist = Distribution.Normal(p1, p2);
                return true;
            }

            // Uniform
            if (!TryRead(boxes[start], true, out p1) || !TryRead(boxes[start + 1], false, out p2) || p2 <= p1)
            {
                MessageBox.Show(role + ": the minimum must be 0 or more and the maximum must be above the minimum.");
                return false;
            }
            dist = Distribution.Uniform(p1, p2);
            return true;
        }

        private void cmbModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loading)
            {
                return;
            }
            ShowFields();
        }

        private void cmbDist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loading)
            {
                return;
            }
            ShowFields();
        }

        private void btnMode_Click(object sender, RoutedEventArgs e)
        {
            useRates = !useRates;
            ShowFields();
        }

        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            ModelType model = GetModel();
            Distribution arr = null;
            Distribution svc = null;
            bool stable = false;
            string text = "";
            double a;
            double b;

            if (model == ModelType.MM1)
            {
                if (!TryRead(txt1, false, out a) || !TryRead(txt2, false, out b))
                {
                    MessageBox.Show("Please enter valid positive numbers.");
                    return;
                }

                double lambda = a;
                double mu = b;
                if (!useRates)
                {
                    lambda = 1 / a;
                    mu = 1 / b;
                }

                stable = calc.MM1(lambda, mu);
                if (stable)
                {
                    text += "Utilization (rho) = " + calc.Rho.ToString("F4") + "\n";
                    text += "Idle proportion    = " + calc.Idle.ToString("F4") + "\n";
                }
            }
            else if (model == ModelType.MMc)
            {
                int servers;
                if (!TryRead(txt1, false, out a) || !TryRead(txt2, false, out b))
                {
                    MessageBox.Show("Please enter valid positive numbers.");
                    return;
                }
                if (!TryReadServers(txt3, out servers))
                {
                    MessageBox.Show("The number of servers must be a whole number from 1 to 200.");
                    return;
                }

                double lambda = a;
                double mu = b;
                if (!useRates)
                {
                    lambda = 1 / a;
                    mu = 1 / b;
                }

                stable = calc.MMc(lambda, mu, servers);
                if (stable)
                {
                    text += "Servers (c)                  = " + calc.Servers + "\n";
                    text += "Utilization per server (rho) = " + calc.Rho.ToString("F4") + "\n";
                    text += "Idle proportion              = " + calc.Idle.ToString("F4") + "\n";
                    text += "P0 (system empty)            = " + calc.P0.ToString("F4") + "\n";
                    text += "P(customer waits)           = " + calc.ProbWait.ToString("F4") + "\n";
                }
            }
            else if (model == ModelType.MG1)
            {
                DistType svcType = GetDist(cmbSvcDist);
                if (!ReadDist(DistType.Exponential, "Arrival", 0, out arr) || !ReadDist(svcType, "Service", 1, out svc))
                {
                    return;
                }

                stable = calc.MG1(1 / arr.Mean, svc.Mean, svc.Variance);
                if (stable)
                {
                    text += "Service mean E[S]   = " + svc.Mean.ToString("F4") + "\n";
                    text += "Service variance    = " + svc.Variance.ToString("F4") + "  (calculated)\n";
                    text += "Service rate (mu)  = " + calc.Mu.ToString("F4") + "\n";
                    text += "Utilization (rho) = " + calc.Rho.ToString("F4") + "\n";
                    text += "Idle proportion    = " + calc.Idle.ToString("F4") + "\n";
                }
            }
            else
            {
                DistType arrType = GetDist(cmbArrDist);
                DistType svcType = GetDist(cmbSvcDist);
                int arrBoxes = 2;
                if (arrType == DistType.Exponential)
                {
                    arrBoxes = 1;
                }

                if (!ReadDist(arrType, "Arrival", 0, out arr) || !ReadDist(svcType, "Service", arrBoxes, out svc))
                {
                    return;
                }

                stable = calc.GG1(arr.Mean, arr.Variance, svc.Mean, svc.Variance);
                if (stable)
                {
                    text += "Arrival mean E[A]     = " + arr.Mean.ToString("F4") + "\n";
                    text += "Arrival variance      = " + arr.Variance.ToString("F4") + "  (calculated)\n";
                    text += "Service mean E[S]     = " + svc.Mean.ToString("F4") + "\n";
                    text += "Service variance      = " + svc.Variance.ToString("F4") + "  (calculated)\n";
                    text += "Utilization (rho)     = " + calc.Rho.ToString("F4") + "\n";
                    text += "Idle proportion       = " + calc.Idle.ToString("F4") + "\n";
                    text += "Ca^2                  = " + calc.Ca2.ToString("F4") + "\n";
                    text += "Cs^2                  = " + calc.Cs2.ToString("F4") + "\n";
                }
            }

            if (!stable)
            {
                txtResult.Text = "System is unstable (rho >= 1). Please change the values.";
                return;
            }

            text += "Lq                = " + calc.Lq.ToString("F4") + "\n";
            text += "L                 = " + calc.L.ToString("F4") + "\n";
            text += "Wq                = " + calc.Wq.ToString("F4") + "\n";
            text += "W                 = " + calc.W.ToString("F4") + "\n\n";
            text += "Average customer:\n";
            text += "Waiting time    = " + calc.AverageCustomer.WaitingTime.ToString("F4") + "\n";
            text += "Service time    = " + calc.AverageCustomer.ServiceTime.ToString("F4") + "\n";
            text += "Turnaround time = " + calc.AverageCustomer.TurnaroundTime.ToString("F4");

            txtResult.Text = text;
        }
    }
}
