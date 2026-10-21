using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QueueingCalculator
{
    // The Simulation tab. This is the second half of the MainWindow class
    // (the calculator tab code is in MainWindow.xaml.cs).
    public partial class MainWindow
    {
        // Whole number between min and max
        bool TryReadInt(TextBox box, int min, int max, out int value)
        {
            if (!int.TryParse(box.Text, out value))
            {
                return false;
            }
            return value >= min && value <= max;
        }

        // Reads the four boxes of one stream.
        // Returns false if something is wrong. stream stays null when the stream has 0 servers (skipped).
        bool ReadStream(string name, TextBox txtL, TextBox txtM, TextBox txtV, TextBox txtS, out StreamSettings stream)
        {
            stream = null;
            int servers;
            if (!TryReadInt(txtS, 0, 200, out servers))
            {
                MessageBox.Show(name + ": number of counters must be a whole number from 0 to 200 (0 = skip this stream).");
                return false;
            }
            if (servers == 0)
            {
                return true;
            }

            double lambda, mean, variance;
            if (!TryRead(txtL, false, out lambda) || !TryRead(txtM, false, out mean) || !TryRead(txtV, true, out variance))
            {
                MessageBox.Show(name + ": arrival rate and mean service time must be above 0. Variance can be 0 or more.");
                return false;
            }

            stream = new StreamSettings();
            stream.Name = name;
            stream.Lambda = lambda;
            stream.MeanService = mean;
            stream.VarService = variance;
            stream.Servers = servers;
            return true;
        }

        // Fills the boxes with the real data we collected at Carrefour (minutes)
        private void btnPreset_Click(object sender, RoutedEventArgs e)
        {
            txtNLambda.Text = "1.26";
            txtNMean.Text = "2.72";
            txtNVar.Text = "7.69";
            txtNServers.Text = "6";

            txtELambda.Text = "0.82";
            txtEMean.Text = "0.94";
            txtEVar.Text = "1.01";
            txtEServers.Text = "1";

            txtDuration.Text = "88";
            txtPatience.Text = "0";
            txtPayFail.Text = "0";
            chkNormalService.IsChecked = false;
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            double duration, patience, payFail;
            int seed;

            if (!TryRead(txtDuration, false, out duration))
            {
                MessageBox.Show("Simulation length must be a number above 0.");
                return;
            }
            if (!TryRead(txtPatience, true, out patience))
            {
                MessageBox.Show("Patience must be 0 or more (0 = customers never leave the queue).");
                return;
            }
            if (!TryRead(txtPayFail, true, out payFail) || payFail > 1)
            {
                MessageBox.Show("Payment failure probability must be between 0 and 1.");
                return;
            }

            seed = 0;
            if (txtSeed.Text.Trim() != "" && !TryReadInt(txtSeed, 1, int.MaxValue, out seed))
            {
                MessageBox.Show("Seed must be a whole number above 0, or empty for a different run each time.");
                return;
            }

            StreamSettings normal, express;
            if (!ReadStream("Normal", txtNLambda, txtNMean, txtNVar, txtNServers, out normal))
            {
                return;
            }
            if (!ReadStream("Express", txtELambda, txtEMean, txtEVar, txtEServers, out express))
            {
                return;
            }

            List<StreamSettings> streams = new List<StreamSettings>();
            if (normal != null)
            {
                streams.Add(normal);
            }
            if (express != null)
            {
                streams.Add(express);
            }
            if (streams.Count == 0)
            {
                MessageBox.Show("Both streams have 0 counters. Nothing to simulate.");
                return;
            }

            SimulationEngine engine = new SimulationEngine(seed);
            engine.Duration = duration;
            engine.Patience = patience;
            engine.PayFailProb = payFail;
            engine.ExponentialService = chkNormalService.IsChecked != true;
            engine.Run(streams);

            dgCustomers.ItemsSource = null;
            dgCustomers.ItemsSource = engine.Customers;
            txtSimResult.Text = BuildSummaryText(engine);
        }

        string BuildSummaryText(SimulationEngine engine)
        {
            string text = "";

            foreach (StreamSummary s in engine.Summaries)
            {
                text += "----- " + s.Settings.Name + " (" + s.Settings.Servers + " counters) -----\n";
                text += "Customers arrived       = " + s.Arrived + "\n";
                text += "Reached a counter       = " + s.Served + "\n";
                text += "Reneged (left queue)    = " + s.Reneged + "\n";
                text += "Payment failed          = " + s.PaymentFailed + "\n";
                text += "Average wait (Wq)       = " + s.AvgWait.ToString("F4") + "\n";
                text += "Average service time    = " + s.AvgService.ToString("F4") + "\n";
                text += "Average turnaround (W)  = " + s.AvgTurnaround.ToString("F4") + "\n";
                text += "Longest wait            = " + s.MaxWait.ToString("F4") + "\n";

                for (int i = 0; i < s.Utilization.Length; i++)
                {
                    text += "Counter " + (i + 1) + " utilization   = " + s.Utilization[i].ToString("F4") + "\n";
                }

                // Compare with the calculator (M/M/c). Exact only for exponential service and no reneging.
                QueueCalculator q = new QueueCalculator();
                if (q.MMc(s.Settings.Lambda, 1 / s.Settings.MeanService, s.Settings.Servers))
                {
                    text += "Calculator M/M/c Wq    = " + q.Wq.ToString("F4") + "\n";
                }
                else
                {
                    text += "Calculator M/M/c       = unstable (rho >= 1)\n";
                }
                text += "\n";
            }

            return text;
        }
    }
}
