using System.Windows;
using System.Windows.Controls;

namespace QueueingCalculator
{
    public partial class MainWindow : Window
    {
        QueueCalculator calc = new QueueCalculator();
        bool useRates = true;

        public MainWindow()
        {
            InitializeComponent();
            cmbModel.Items.Add("M/M/1");
            cmbModel.Items.Add("M/G/1");
            cmbModel.Items.Add("G/G/1");
            cmbModel.SelectedIndex = 0;
        }

        ModelType GetModel()
        {
            if (cmbModel.SelectedIndex == 1)
            {
                return ModelType.MG1;
            }
            if (cmbModel.SelectedIndex == 2)
            {
                return ModelType.GG1;
            }
            return ModelType.MM1;
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

        void ShowFields()
        {
            txtResult.Text = "";
            ModelType model = GetModel();

            string arrival = "Mean inter-arrival time E[A]";
            string service = "Mean service time E[S]";

            if (useRates)
            {
                arrival = "Arrival rate (lambda)";
                service = "Service rate (mu)";
                btnMode.Content = "Input mode: Rates (lambda, mu) - click to switch to Times";
            }
            else
            {
                btnMode.Content = "Input mode: Times (E[A], E[S]) - click to switch to Rates";
            }

            if (model == ModelType.MM1)
            {
                lblInfo.Text = "Poisson arrivals, exponential service. Needs the arrival and service values.";
                SetField(lbl1, txt1, arrival);
                SetField(lbl2, txt2, service);
                SetField(lbl3, txt3, null);
                SetField(lbl4, txt4, null);
            }
            else if (model == ModelType.MG1)
            {
                lblInfo.Text = "Poisson arrivals, general service. Needs the arrival and service values and the variance of service time.";
                SetField(lbl1, txt1, arrival);
                SetField(lbl2, txt2, service);
                SetField(lbl3, txt3, "Variance of service time");
                SetField(lbl4, txt4, null);
            }
            else
            {
                lblInfo.Text = "General arrivals, general service (Marchal's approximation). Needs the arrival and service values and both variances.";
                SetField(lbl1, txt1, arrival);
                SetField(lbl2, txt2, "Variance of inter-arrival time");
                SetField(lbl3, txt3, service);
                SetField(lbl4, txt4, "Variance of service time");
            }
        }

        private void cmbModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
            double a, b, c, d;
            bool stable = false;
            string text = "";

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
            else if (model == ModelType.MG1)
            {
                if (!TryRead(txt1, false, out a) || !TryRead(txt2, false, out b) || !TryRead(txt3, true, out c))
                {
                    MessageBox.Show("Please enter valid numbers. Variance can be 0, the others must be above 0.");
                    return;
                }

                double lambda = a;
                double meanService = b;
                if (useRates)
                {
                    meanService = 1 / b;
                }
                else
                {
                    lambda = 1 / a;
                }

                stable = calc.MG1(lambda, meanService, c);
                if (stable)
                {
                    text += "Service rate (mu) = " + calc.Mu.ToString("F4") + "\n";
                    text += "Utilization (rho) = " + calc.Rho.ToString("F4") + "\n";
                    text += "Idle proportion    = " + calc.Idle.ToString("F4") + "\n";
                }
            }
            else
            {
                if (!TryRead(txt1, false, out a) || !TryRead(txt2, true, out b) || !TryRead(txt3, false, out c) || !TryRead(txt4, true, out d))
                {
                    MessageBox.Show("Please enter valid numbers. Variances can be 0, the other values must be above 0.");
                    return;
                }

                double meanArrival = a;
                double meanService = c;
                if (useRates)
                {
                    meanArrival = 1 / a;
                    meanService = 1 / c;
                }

                stable = calc.GG1(meanArrival, b, meanService, d);
                if (stable)
                {
                    text += "Mean inter-arrival time = " + calc.MeanArrival.ToString("F4") + "\n";
                    text += "Utilization (rho)       = " + calc.Rho.ToString("F4") + "\n";
                    text += "Idle proportion          = " + calc.Idle.ToString("F4") + "\n";
                    text += "Ca^2                    = " + calc.Ca2.ToString("F4") + "\n";
                    text += "Cs^2                    = " + calc.Cs2.ToString("F4") + "\n";
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
