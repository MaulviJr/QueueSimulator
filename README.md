# Queueing Calculator

A WPF (.NET 8) desktop calculator for single-queue models: **M/M/1, M/M/c, M/G/1 and G/G/1**.
Made for the CS-577 Modeling and Simulation course (case study: Carrefour, Lucky One Mall, Karachi).
Only the formulas from the class notebook are used.

## Requirements

- Windows
- .NET 8 SDK (or Visual Studio 2022 with the ".NET desktop development" workload)

## How to run

Command line, inside this folder:

    dotnet run

Visual Studio: open `QueueingCalculator.csproj` and press F5.

## How to use

1. Select a model from the dropdown.
2. For M/G/1 and G/G/1, pick the distribution(s) from the dropdown(s) that appear.
3. Enter the values shown in the input boxes. The labels change with the model and distribution.
4. (Exponential inputs only) Click the "Input mode" button to switch between rates (lambda, mu) and times (E[A], E[S]).
5. Click Calculate.

Notes:

- Use one time unit for everything (for example minutes) and type decimals such as `0.1`, not `1/10`.
- Changing the model, a distribution or the input mode clears the boxes and the result.
- If the system is unstable (rho >= 1), a message is shown instead of results.
- Invalid input (letters, negatives, zero where not allowed) shows an error message.

## Models and inputs

| Model | Description | Inputs |
|---|---|---|
| M/M/1 | Poisson arrivals, exponential service, 1 server | Arrival value, service value |
| M/M/c | Poisson arrivals, exponential service, c identical servers, one shared queue | Arrival value, service value (for ONE server), number of servers c (whole number, 1 to 200) |
| M/G/1 | Poisson arrivals, general service | Arrival value (exponential), service distribution and its parameters |
| G/G/1 | General arrivals, general service (Marchal's approximation) | Arrival distribution and parameters, service distribution and parameters |

### Distributions (M/G/1 and G/G/1)

You never type a variance. You type the distribution parameters and the program calculates the mean and variance.

| Distribution | You enter | Mean | Variance | Rules |
|---|---|---|---|---|
| Exponential | Rate, or mean time (depends on Input mode) | mean | mean^2 | value > 0 |
| Gamma | Shape K, scale theta | K * theta | K * theta^2 | both > 0 |
| Normal | Mean, standard deviation | mean | sd^2 | mean > 0, sd >= 0 |
| Uniform | Minimum, maximum | (min + max) / 2 | (max - min)^2 / 12 | min >= 0, max > min |

The Input mode button (rates vs. times) only changes Exponential fields, using lambda = 1/E[A] and mu = 1/E[S]. Gamma, Normal and Uniform parameters are the same in both modes.

Choosing Exponential for arrival or service gives the "M" part of the model. In M/G/1 the arrival side is always exponential. In G/G/1 both sides can be changed.

## Formulas (from the notebook)

Let lambda = arrival rate, mu = service rate, rho = utilization.

**M/M/1**

- rho = lambda / mu
- Lq = rho^2 / (1 - rho)

**M/M/c** (rho is per server)

- a = lambda / mu (offered load)
- rho = lambda / (c * mu)
- P0 = 1 / ( sum for n = 0..c-1 of a^n / n!  +  a^c / (c! (1 - rho)) )
- P(wait) = a^c / (c! (1 - rho)) * P0 (Erlang C)
- Lq = P(wait) * rho / (1 - rho)

**M/G/1** (Pollaczek-Khinchine)

- mu = 1 / E[S]
- rho = lambda / mu
- Lq = (lambda^2 * Var(S) + rho^2) / (2 * (1 - rho))

**G/G/1** (Marchal's approximation)

- lambda = 1 / E[A], mu = 1 / E[S], rho = lambda / mu
- Ca^2 = Var(A) / E[A]^2
- Cs^2 = Var(S) / E[S]^2
- Lq = rho^2 (1 + Cs^2) (Ca^2 + rho^2 Cs^2) / (2 (1 - rho) (1 + rho^2 Cs^2))

**Common to all models**

- Idle proportion = 1 - rho
- Wq = Lq / lambda
- W = Wq + 1 / mu
- L = lambda * W (Little's Law)

All models need rho < 1 to be stable.

## Output meaning

| Output | Meaning |
|---|---|
| rho | Server utilization (per server for M/M/c) |
| Idle proportion | Fraction of time a server is idle (1 - rho) |
| P0 | Probability the system is empty (M/M/c) |
| P(customer waits) | Probability an arriving customer has to wait (M/M/c, Erlang C) |
| Service mean / variance | Calculated from the chosen distribution (M/G/1, G/G/1) |
| Arrival mean / variance | Calculated from the chosen distribution (G/G/1) |
| Ca^2, Cs^2 | Squared coefficient of variation of inter-arrival and service times (G/G/1) |
| Lq | Average number of customers waiting in the queue |
| L | Average number of customers in the system |
| Wq | Average waiting time in the queue |
| W | Average total time in the system |

The "Average customer" section shows the waiting time (Wq), service time (1/mu) and turnaround time (W) of an average customer.

## How the code is organized

The window has four generic input slots (`lbl1/txt1` to `lbl4/txt4`). `ShowFields()` relabels, shows or hides and clears them depending on the selected model, distributions and input mode. `btnCalculate_Click` reads and validates the boxes, calls the matching method in `QueueCalculator`, and prints the result.

| File | Purpose |
|---|---|
| `App.xaml` / `App.xaml.cs` | WPF startup. `StartupUri` opens `MainWindow.xaml` |
| `MainWindow.xaml` / `MainWindow.xaml.cs` | The window, field switching, input validation, output text |
| `QueueCalculator.cs` | The formulas: `MM1`, `MMc`, `MG1`, `GG1` (each returns false if unstable) |
| `Distribution.cs` | Turns distribution parameters into mean and variance |
| `ModelType.cs` | Enum of the models: MM1, MMc, MG1, GG1 |
| `Customer.cs` | Customer attributes: arrival, start, service and end time, waiting and turnaround time |
| `SimulationEngine.cs` | Simulation engine (see below) |
| `QueueingCalculator.csproj` | Project file |
