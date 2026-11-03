using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QueueingCalculator
{
    // The Simulation tab. (The Calculator tab code is in MainWindow.xaml.cs.)
    // This file only reads the inputs, calls SimulationEngine, and shows the results.
    public partial class MainWindow : Window
    {
        static readonly Color[] Palette =
        {
            Color.FromRgb(0x8E, 0xB8, 0xE5), Color.FromRgb(0xF2, 0xB1, 0x7A),
            Color.FromRgb(0x9C, 0xD3, 0x9B), Color.FromRgb(0xE8, 0x9F, 0xA6),
            Color.FromRgb(0xC5, 0xB0, 0xE0), Color.FromRgb(0xE6, 0xD3, 0x7E),
            Color.FromRgb(0x8F, 0xD3, 0xCF), Color.FromRgb(0xD3, 0xA5, 0x8F)
        };

        // ---------------------------------------------------------------
        //  Buttons
        // ---------------------------------------------------------------

        private void btnPreset_Click(object sender, RoutedEventArgs e)
        {
            txtSimLambda.Text = "1.29";
            txtSimMean.Text = "2.73";
            txtSimServers.Text = "5";
            txtSimDuration.Text = "88";
            txtSimSeed.Text = "";
            txtSimReps.Text = "75";
            chkClass.IsChecked = true;
        }

        // One run: shows the result text, the customer table and all four graphs.
        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            int? seed;
            SimSettings s = ReadSimSettings(out seed);
            if (s == null)
            {
                return;
            }

            Random rng;
            if (seed.HasValue)
            {
                rng = new Random(seed.Value);
            }
            else
            {
                rng = new Random();
            }

            SimResult r = SimulationEngine.Run(s, rng);
            txtSimResult.Text = RunText(r, s);
            ShowRun(r);
        }

        // Many runs: averages the results and compares them with the M/M/c formulas.
        private void btnReplicate_Click(object sender, RoutedEventArgs e)
        {
            int? seed;
            SimSettings s = ReadSimSettings(out seed);
            if (s == null)
            {
                return;
            }

            int reps;
            if (!int.TryParse(txtSimReps.Text, out reps) || reps < 1 || reps > 1000)
            {
                MessageBox.Show("Replications must be a whole number from 1 to 1000.");
                return;
            }

            List<double> wq = new List<double>();
            List<double> w = new List<double>();
            List<double> lq = new List<double>();
            List<double> l = new List<double>();
            List<double> util = new List<double>();
            List<double> pWait = new List<double>();
            double[] counterUtil = new double[s.Servers];
            SimResult first = null;

            for (int i = 0; i < reps; i++)
            {
                Random rng;
                if (seed.HasValue)
                {
                    rng = new Random(seed.Value + i);     // a different, but repeatable, run each time
                }
                else
                {
                    rng = new Random();
                }

                SimResult r = SimulationEngine.Run(s, rng);
                if (i == 0)
                {
                    first = r;
                }

                wq.Add(r.AvgWait);
                w.Add(r.AvgTurnaround);
                lq.Add(r.AvgQueueLength);
                l.Add(r.AvgInSystem);
                util.Add(r.TotalUtilization);
                pWait.Add((double)r.WaitedCount / r.Customers.Count);
                for (int k = 0; k < s.Servers; k++)
                {
                    counterUtil[k] += r.Utilization[k];
                }
            }

            // Analytical answer (same formulas as the Calculator tab)
            QueueCalculator q = new QueueCalculator();
            q.MMc(s.ArrivalRate, 1 / s.MeanService, s.Servers);

            string text = "";
            text += "Replications = " + reps + "   (each one is a full run of " + s.Duration + " minutes)\n";
            text += "95% half-width = 1.96 x s / sqrt(n)\n\n";
            text += string.Format("{0,-30}{1,10}{2,12}{3,14}\n", "Measure", "Simulated", "+/- 95%", "M/M/c formula");
            text += SimRow("Avg wait Wq", wq, q.Wq);
            text += SimRow("Avg turnaround W", w, q.W);
            text += SimRow("Avg queue length Lq", lq, q.Lq);
            text += SimRow("Avg in system L", l, q.L);
            text += SimRow("P(customer waits)", pWait, q.ProbWait);
            text += SimRow("Total utilization (rho)", util, q.Rho);

            text += "\nAverage utilization of each counter:\n";
            for (int k = 0; k < s.Servers; k++)
            {
                text += string.Format("  Counter {0,-3}{1,10:F4}\n", k + 1, counterUtil[k] / reps);
            }

            if (s.ClassMethod)
            {
                text += "\nNote: the class method uses whole-minute gaps and service times, so it does not follow the\n";
                text += "pure M/M/c formulas exactly. Untick it to compare against the formulas.\n";
            }
            text += "Note: every run starts with an empty system, so short runs read a little lower than the formulas.\n";
            text += "The table and graphs below show replication 1.";

            txtSimResult.Text = text;
            ShowRun(first);
        }

        // ---------------------------------------------------------------
        //  Reading and checking the inputs
        // ---------------------------------------------------------------

        // Returns null (after showing a message) if something is not valid.
        SimSettings ReadSimSettings(out int? seed)
        {
            seed = null;
            double lambda;
            double mean;
            double duration;
            int servers;

            if (!TryRead(txtSimLambda, false, out lambda) || !TryRead(txtSimMean, false, out mean))
            {
                MessageBox.Show("Arrival rate and mean service time must be numbers above 0.");
                return null;
            }
            if (!TryReadServers(txtSimServers, out servers) || servers > 30)
            {
                MessageBox.Show("The number of counters must be a whole number from 1 to 30.");
                return null;
            }
            if (!TryRead(txtSimDuration, false, out duration))
            {
                MessageBox.Show("Simulation length must be a number above 0.");
                return null;
            }
            if (lambda * duration > 5000)
            {
                MessageBox.Show("That would create more than about 5000 customers. Use a shorter length or a lower arrival rate.");
                return null;
            }

            // First test of every queue: it must be stable.
            double rho = lambda * mean / servers;
            if (rho >= 1)
            {
                MessageBox.Show("System is unstable: rho = " + rho.ToString("F4") + " (must be below 1). Please change the values.");
                return null;
            }

            string seedText = txtSimSeed.Text.Trim();
            if (seedText != "")
            {
                int value;
                if (!int.TryParse(seedText, out value))
                {
                    MessageBox.Show("The seed must be a whole number, or empty.");
                    return null;
                }
                seed = value;
            }

            SimSettings s = new SimSettings();
            s.ArrivalRate = lambda;
            s.MeanService = mean;
            s.Servers = servers;
            s.Duration = duration;
            s.ClassMethod = chkClass.IsChecked == true;
            return s;
        }

        // ---------------------------------------------------------------
        //  Showing the results
        // ---------------------------------------------------------------

        string RunText(SimResult r, SimSettings s)
        {
            int n = r.Customers.Count;
            string text = "";

            text += "Customers arrived        = " + n + "\n";
            text += "Simulation time          = " + r.TotalTime.ToString("F2") + " min (first start to last end)\n";
            text += "Avg inter-arrival time   = " + r.AvgInterArrival.ToString("F4") + "\n";
            text += "Avg service time         = " + r.AvgService.ToString("F4") + "\n";
            text += "Avg wait Wq              = " + r.AvgWait.ToString("F4") + "\n";
            text += "Avg turnaround W         = " + r.AvgTurnaround.ToString("F4") + "\n";
            text += "Customers who waited     = " + r.WaitedCount + " of " + n + "  (" + (100.0 * r.WaitedCount / n).ToString("F1") + "%)\n";
            text += "Avg queue length Lq      = " + r.AvgQueueLength.ToString("F4") + "\n";
            text += "Avg in system L          = " + r.AvgInSystem.ToString("F4") + "\n";
            text += "Longest queue            = " + r.MaxQueue + "\n";
            text += "Total utilization        = " + r.TotalUtilization.ToString("F4");

            bool ok = r.TotalUtilization <= 1.0 + 1e-9;
            foreach (double u in r.Utilization)
            {
                if (u > 1.0 + 1e-9)
                {
                    ok = false;
                }
            }
            if (ok)
            {
                text += "   (check: every utilization is between 0 and 1, OK)\n";
            }
            else
            {
                text += "   (check FAILED: a utilization is above 1)\n";
            }

            text += "\n" + string.Format("{0,-10}{1,7}{2,12}{3,14}\n", "Counter", "Served", "Busy time", "Utilization");
            for (int i = 0; i < r.Servers; i++)
            {
                text += string.Format("{0,-10}{1,7}{2,12:F2}{3,14:F4}\n", i + 1, r.Served[i], r.BusyTime[i], r.Utilization[i]);
            }
            return text;
        }

        // One row of the replication table: mean, 95% half-width and the formula value.
        string SimRow(string name, List<double> values, double formula)
        {
            return string.Format("{0,-30}{1,10:F4}{2,12:F4}{3,14:F4}\n", name, values.Average(), HalfWidth(values), formula);
        }

        static double HalfWidth(List<double> values)
        {
            if (values.Count < 2)
            {
                return 0;
            }
            double m = values.Average();
            double sum = 0;
            foreach (double x in values)
            {
                sum += (x - m) * (x - m);
            }
            double sd = Math.Sqrt(sum / (values.Count - 1));
            return 1.96 * sd / Math.Sqrt(values.Count);
        }

        void ShowRun(SimResult r)
        {
            dgCustomers.ItemsSource = r.Customers;

            DrawGantt(canGantt, r);

            List<Point> queue = new List<Point>();
            foreach (StepPoint p in r.QueueSteps)
            {
                queue.Add(new Point(p.Time, p.Value));
            }
            DrawLineChart(canQueue, queue, true, r.TotalTime, "Time (minutes)", "Customers in queue");

            // Running average of the waiting time: after each customer starts service,
            // the average wait of all customers who have started so far.
            List<Point> avgWait = new List<Point>();
            double sum = 0;
            int count = 0;
            foreach (Customer c in r.Customers)
            {
                sum += c.WaitingTime;
                count++;
                avgWait.Add(new Point(c.StartTime, sum / count));
            }
            DrawLineChart(canWait, avgWait, false, r.TotalTime, "Time (minutes)", "Average wait (min)");

            DrawBarChart(canUtil, r);
        }

        // ---------------------------------------------------------------
        //  Drawing helpers (no extra packages needed)
        // ---------------------------------------------------------------

        void AddText(Canvas canvas, string text, double x, double y, double size, Brush brush)
        {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.FontSize = size;
            if (brush == null)
            {
                tb.Foreground = Brushes.Black;
            }
            else
            {
                tb.Foreground = brush;
            }
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            canvas.Children.Add(tb);
        }

        Line AddLine(Canvas canvas, double x1, double y1, double x2, double y2, Brush brush, double thickness)
        {
            Line line = new Line();
            line.X1 = x1;
            line.Y1 = y1;
            line.X2 = x2;
            line.Y2 = y2;
            line.Stroke = brush;
            line.StrokeThickness = thickness;
            canvas.Children.Add(line);
            return line;
        }

        Rectangle AddRect(Canvas canvas, double x, double y, double width, double height, Brush fill)
        {
            Rectangle rect = new Rectangle();
            rect.Width = width;
            rect.Height = height;
            rect.Fill = fill;
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            canvas.Children.Add(rect);
            return rect;
        }

        // A "nice" tick distance (1, 2, 5, 10, ... times a power of ten) for about 'ticks' ticks.
        static double NiceStep(double range, int ticks)
        {
            if (range <= 0)
            {
                return 1;
            }
            double raw = range / ticks;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double norm = raw / magnitude;
            double step;
            if (norm < 1.5)
            {
                step = 1;
            }
            else if (norm < 3)
            {
                step = 2;
            }
            else if (norm < 7)
            {
                step = 5;
            }
            else
            {
                step = 10;
            }
            return step * magnitude;
        }

        // Line graph or step graph. Used for "queue length" and "average wait".
        void DrawLineChart(Canvas canvas, List<Point> pts, bool stepStyle, double xMax, string xLabel, string yLabel)
        {
            canvas.Children.Clear();

            double left = 60;
            double right = 20;
            double top = 15;
            double bottom = 45;
            double pw = canvas.Width - left - right;
            double ph = canvas.Height - top - bottom;

            double yMax = 0;
            foreach (Point p in pts)
            {
                if (p.Y > yMax)
                {
                    yMax = p.Y;
                }
            }
            if (yMax <= 0)
            {
                yMax = 1;
            }
            double yStep = NiceStep(yMax, 5);
            yMax = Math.Ceiling(yMax / yStep - 1e-9) * yStep;

            if (xMax <= 0)
            {
                xMax = 1;
            }
            double xStep = NiceStep(xMax, 8);
            double xTop = Math.Ceiling(xMax / xStep - 1e-9) * xStep;

            // grid and tick labels
            for (int i = 0; i * yStep <= yMax + 1e-9; i++)
            {
                double y = top + ph - (i * yStep) / yMax * ph;
                AddLine(canvas, left, y, left + pw, y, Brushes.Gainsboro, 1);
                AddText(canvas, (i * yStep).ToString("0.##"), left - 38, y - 8, 10, null);
            }
            for (int i = 0; i * xStep <= xTop + 1e-9; i++)
            {
                double x = left + (i * xStep) / xTop * pw;
                AddLine(canvas, x, top, x, top + ph, Brushes.Gainsboro, 1);
                AddText(canvas, (i * xStep).ToString("0.##"), x - 8, top + ph + 3, 10, null);
            }
            AddLine(canvas, left, top, left, top + ph, Brushes.Gray, 1.5);
            AddLine(canvas, left, top + ph, left + pw, top + ph, Brushes.Gray, 1.5);
            AddText(canvas, xLabel, left + pw / 2 - 35, top + ph + 22, 11, null);

            TextBlock yText = new TextBlock();
            yText.Text = yLabel;
            yText.FontSize = 11;
            yText.RenderTransform = new RotateTransform(-90);
            Canvas.SetLeft(yText, 6);
            Canvas.SetTop(yText, top + ph / 2 + yLabel.Length * 3);
            canvas.Children.Add(yText);

            // the data
            Polyline line = new Polyline();
            line.Stroke = Brushes.SteelBlue;
            line.StrokeThickness = 1.8;

            if (pts.Count > 0)
            {
                line.Points.Add(new Point(left + pts[0].X / xTop * pw, top + ph - pts[0].Y / yMax * ph));
                for (int i = 1; i < pts.Count; i++)
                {
                    double x = left + pts[i].X / xTop * pw;
                    if (stepStyle)
                    {
                        // hold the old value until the time of the change, then jump
                        line.Points.Add(new Point(x, top + ph - pts[i - 1].Y / yMax * ph));
                    }
                    line.Points.Add(new Point(x, top + ph - pts[i].Y / yMax * ph));
                }
                if (stepStyle)
                {
                    double lastY = top + ph - pts[pts.Count - 1].Y / yMax * ph;
                    line.Points.Add(new Point(left + xMax / xTop * pw, lastY));
                }
            }
            canvas.Children.Add(line);
        }

        // Bar graph: utilization of each counter and of the whole system.
        void DrawBarChart(Canvas canvas, SimResult r)
        {
            canvas.Children.Clear();

            double left = 60;
            double right = 20;
            double top = 15;
            double bottom = 45;
            double pw = canvas.Width - left - right;
            double ph = canvas.Height - top - bottom;

            for (int i = 0; i <= 5; i++)
            {
                double value = i * 0.2;
                double y = top + ph - value * ph;
                AddLine(canvas, left, y, left + pw, y, Brushes.Gainsboro, 1);
                AddText(canvas, (value * 100).ToString("0") + "%", left - 40, y - 8, 10, null);
            }
            AddLine(canvas, left, top, left, top + ph, Brushes.Gray, 1.5);
            AddLine(canvas, left, top + ph, left + pw, top + ph, Brushes.Gray, 1.5);

            // dashed line at 100%: no bar may go above it
            Line limit = AddLine(canvas, left, top, left + pw, top, Brushes.Firebrick, 1);
            limit.StrokeDashArray = new DoubleCollection { 4, 3 };

            int bars = r.Servers + 1;
            double slot = pw / bars;
            double barWidth = Math.Min(50, slot * 0.6);

            for (int i = 0; i < bars; i++)
            {
                double value;
                string label;
                Brush fill;
                if (i < r.Servers)
                {
                    value = r.Utilization[i];
                    label = "C" + (i + 1);
                    fill = new SolidColorBrush(Palette[i % Palette.Length]);
                }
                else
                {
                    value = r.TotalUtilization;
                    label = "Total";
                    fill = Brushes.SteelBlue;
                }

                double h = Math.Min(value, 1.0) * ph;
                double x = left + i * slot + (slot - barWidth) / 2;
                AddRect(canvas, x, top + ph - h, barWidth, h, fill);
                AddText(canvas, (value * 100).ToString("F1") + "%", x - 2, top + ph - h - 16, 10, null);
                AddText(canvas, label, x + barWidth / 2 - 10, top + ph + 4, 11, null);
            }
        }

        // Gantt chart: one lane per counter, one block per customer (the number is the customer).
        void DrawGantt(Canvas canvas, SimResult r)
        {
            canvas.Children.Clear();

            int c = r.Servers;
            double laneH = 26;
            double gap = 6;
            double left = 70;
            double right = 20;
            double top = 10;
            double bottom = 40;

            canvas.Height = top + c * (laneH + gap) + bottom;
            double pw = canvas.Width - left - right;
            double axisY = top + c * (laneH + gap);

            double xMax = Math.Max(r.TotalTime, 1);
            double xStep = NiceStep(xMax, 10);
            double xTop = Math.Ceiling(xMax / xStep - 1e-9) * xStep;

            // lane backgrounds and names
            for (int i = 0; i < c; i++)
            {
                double y = top + i * (laneH + gap);
                AddRect(canvas, left, y, pw, laneH, Brushes.WhiteSmoke);
                AddText(canvas, "Counter " + (i + 1), 8, y + 5, 11, null);
            }

            // time grid
            for (int i = 0; i * xStep <= xTop + 1e-9; i++)
            {
                double x = left + (i * xStep) / xTop * pw;
                AddLine(canvas, x, top, x, axisY, Brushes.Gainsboro, 1);
                AddText(canvas, (i * xStep).ToString("0.##"), x - 8, axisY + 3, 10, null);
            }
            AddText(canvas, "Time (minutes)", left + pw / 2 - 40, axisY + 20, 11, null);

            // customers
            foreach (Customer cust in r.Customers)
            {
                double y = top + (cust.Server - 1) * (laneH + gap);
                double x1 = left + cust.StartTime / xTop * pw;
                double x2 = left + cust.EndTime / xTop * pw;
                double width = Math.Max(x2 - x1, 1.5);

                Rectangle rect = AddRect(canvas, x1, y, width, laneH, new SolidColorBrush(Palette[cust.Id % Palette.Length]));
                rect.Stroke = Brushes.White;
                rect.StrokeThickness = 0.5;
                rect.ToolTip = "Customer " + cust.Id
                    + "\nArrival " + cust.ArrivalTime.ToString("F2")
                    + "\nStart " + cust.StartTime.ToString("F2")
                    + "\nEnd " + cust.EndTime.ToString("F2")
                    + "\nWait " + cust.WaitingTime.ToString("F2");

                if (width >= 16)
                {
                    AddText(canvas, cust.Id.ToString(), x1 + 2, y + 6, 9, null);
                }
            }
        }
    }
}
