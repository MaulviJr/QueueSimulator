# Single Server Queueing Calculator

A WPF (.NET) desktop calculator for three single server queueing models: M/M/1, M/G/1 and G/G/1.
Made for the Modeling and Simulation course. Only the formulas from the class notebook are used.

## Requirements

- Windows
- .NET 8 SDK (or Visual Studio 2022 with the ".NET desktop development" workload)

## How to run

Using the command line, inside this folder:

    dotnet run

Using Visual Studio: open `QueueingCalculator.csproj` and press F5.

## How to use

1. Select a model from the dropdown.
2. Enter the values for that model.
3. Click the "Input mode" button if you want to switch between rates (lambda, mu) and times (E[A], E[S]).
4. Click Calculate.
5. Change the model or the values and calculate again.

If the system is unstable (rho >= 1), the program shows a message instead of results.
Invalid input (letters, negative numbers, zero where it is not allowed) shows an error message.
Use one time unit for everything (for example minutes) and type decimals, such as 0.1 instead of 1/10.

## Inputs

The "Input mode" button switches the mean values between rates and times. The program converts them using lambda = 1/E[A] and mu = 1/E[S]. Switching clears the boxes.

| Model | Rates mode | Times mode |
|---|---|---|
| M/M/1 | lambda, mu | E[A], E[S] |
| M/G/1 | lambda, mu, variance of service time | E[A], E[S], variance of service time |
| G/G/1 | lambda, variance of inter-arrival time, mu, variance of service time | E[A], variance of inter-arrival time, E[S], variance of service time |

Variances are always variances of times (time squared). They can be 0. All other values must be greater than 0.

## Formulas (from the notebook)

M/M/1

- rho = lambda / mu
- Lq = rho^2 / (1 - rho)
- Wq = Lq / lambda
- W = Wq + 1/mu
- L = lambda * W
- Idle proportion = 1 - rho

M/G/1

- mu = 1 / E[S]
- rho = lambda / mu
- Lq = (lambda^2 * Var(S) + rho^2) / (2 * (1 - rho))
- Wq = Lq / lambda
- W = Wq + 1/mu
- L = lambda * W
- Idle proportion = 1 - rho

G/G/1 (Marchal's approximation)

- lambda = 1 / E[A]
- mu = 1 / E[S]
- rho = lambda / mu
- Ca^2 = Var(A) / E[A]^2
- Cs^2 = Var(S) / E[S]^2
- Lq = rho^2 (1 + Cs^2) (Ca^2 + rho^2 Cs^2) / (2 (1 - rho) (1 + rho^2 Cs^2))
- Wq = Lq / lambda
- W = Wq + 1/mu
- L = lambda * W
- Idle proportion = 1 - rho

All models need rho < 1 to be stable.

## Output meaning

| Output | Meaning |
|---|---|
| rho | Server utilization (fraction of time the server is busy) |
| Idle proportion | Fraction of time the server is idle (1 - rho) |
| Ca^2, Cs^2 | Squared coefficient of variation of inter-arrival and service times |
| Lq | Average number of customers waiting in the queue |
| L | Average number of customers in the system |
| Wq | Average waiting time in the queue |
| W | Average total time in the system |

The "Average customer" section shows the waiting time (Wq), service time and turnaround time (W) of an average customer.

## Project files

| File | Purpose |
|---|---|
| `Customer.cs` | Customer attributes: arrival time, start time, service time, end time, waiting time, turnaround time |
| `ModelType.cs` | The model types: MM1, MG1, GG1 |
| `QueueCalculator.cs` | The formulas for each model |
| `MainWindow.xaml` / `MainWindow.xaml.cs` | The window and its button and dropdown logic |
| `App.xaml` / `App.xaml.cs` | WPF application startup |
| `QueueingCalculator.csproj` | Project file |

## Sample tests (from the notebook, times in minutes)

| Model | Input | Expected result |
|---|---|---|
| M/M/1 | lambda = 0.1, mu = 0.125 | rho = 0.8, idle = 0.2, Lq = 3.2, Wq = 32, W = 40, L = 4 |
| M/G/1 | lambda = 0.1, mu = 0.125, variance = 0.3333 | rho = 0.8, idle = 0.2, Lq = 1.6083, Wq = 16.0833, W = 24.0833, L = 2.4083 |
| G/G/1 | lambda = 0.1, variance = 20, mu = 0.125, variance = 25 | rho = 0.8, idle = 0.2, Ca^2 = 0.2, Cs^2 = 0.3906, Lq = 0.8010, Wq = 8.0100, W = 16.0100, L = 1.6010 |
| M/M/1 | lambda = 6, mu = 5 | unstable message |

Times mode gives the same answers. For example, M/M/1 with E[A] = 10 and E[S] = 8 gives the same result as lambda = 0.1 and mu = 0.125.
