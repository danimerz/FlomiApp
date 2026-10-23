namespace FlomiApp.Helpers;

/// <summary>Ergebnis einer Tourenplanung: pro Fahrzeug die Stopps in Fahrreihenfolge.</summary>
/// <param name="Routes">Stopp-Indizes der Matrix (1..n) pro Fahrzeug; 0 ist der Startort.</param>
/// <param name="RouteSeconds">Dauer pro Tour inkl. Ladezeit und Rückfahrt zum Startort.</param>
public sealed record RoutePlan(IReadOnlyList<IReadOnlyList<int>> Routes, IReadOnlyList<double> RouteSeconds)
{
    public double LongestSeconds => RouteSeconds.Count == 0 ? 0 : RouteSeconds.Max();
}

/// <summary>
/// Verteilt Abholstopps auf Fahrzeuge und bestimmt die Reihenfolge (Tourenplanung).
/// Alle Touren starten und enden am Startort (Index 0 der Matrix), wo abgeladen wird.
///
/// Ziel: Gesamtfahrzeit + längste Tour möglichst klein – also kurze Wege, aber auch
/// gleichmässig verteilt, damit nicht ein Fahrzeug bis abends unterwegs ist.
/// Vorgehen: Einfügen nach geringsten Mehrkosten (weit entfernte Stopps zuerst),
/// danach lokale Verbesserung (Stopp verschieben, Stopps tauschen, 2-opt), bis
/// sich nichts mehr verbessert. Für die bei einem Flomi üblichen Mengen
/// (einige Dutzend Stopps, wenige Fahrzeuge) läuft das in Millisekunden.
/// </summary>
public static class RouteOptimizer
{
    /// <summary>Angenommene Zeit pro Stopp für Klingeln, Tragen und Einladen.</summary>
    public const double ServiceSecondsPerStop = 10 * 60;

    private const double BalanceWeight = 1.0;
    private const double Epsilon       = 1e-6;
    private const int    MaxPasses     = 200;

    /// <summary>Verteilt alle Stopps 1..n der Matrix auf <paramref name="vehicleCount"/> Fahrzeuge.</summary>
    public static RoutePlan Distribute(double[,] seconds, int vehicleCount)
    {
        var stopCount = seconds.GetLength(0) - 1;
        var routes    = Enumerable.Range(0, Math.Max(1, vehicleCount)).Select(_ => new List<int>()).ToList();
        var costs     = new double[routes.Count];

        var stops = Enumerable.Range(1, stopCount)
            .OrderByDescending(s => seconds[0, s] + seconds[s, 0])
            .ThenBy(s => s);

        foreach (var stop in stops)
        {
            var best = (Route: 0, Pos: 0, Cost: 0.0, Objective: double.MaxValue);
            for (var r = 0; r < routes.Count; r++)
            {
                for (var pos = 0; pos <= routes[r].Count; pos++)
                {
                    routes[r].Insert(pos, stop);
                    var cost = RouteSeconds(seconds, routes[r]);
                    routes[r].RemoveAt(pos);

                    var objective = Objective(costs, r, cost);
                    if (objective < best.Objective - Epsilon) best = (r, pos, cost, objective);
                }
            }
            routes[best.Route].Insert(best.Pos, stop);
            costs[best.Route] = best.Cost;
        }

        Improve(seconds, routes, costs, interRoute: true);
        return ToPlan(routes, costs);
    }

    /// <summary>Behält die Zuteilung bei und optimiert nur die Reihenfolge innerhalb jeder Tour.</summary>
    public static RoutePlan OrderOnly(double[,] seconds, IReadOnlyList<IReadOnlyList<int>> assignedRoutes)
    {
        var routes = assignedRoutes.Select(r => NearestNeighbour(seconds, r)).ToList();
        var costs  = routes.Select(r => RouteSeconds(seconds, r)).ToArray();

        Improve(seconds, routes, costs, interRoute: false);
        return ToPlan(routes, costs);
    }

    /// <summary>Dauer einer Tour: Startort → Stopps → Startort, plus Ladezeit pro Stopp.</summary>
    public static double RouteSeconds(double[,] seconds, IReadOnlyList<int> route) =>
        RouteLength(seconds, route) + ServiceSecondsPerStop * route.Count;

    /// <summary>Summe der Matrixwerte entlang der Tour (z.B. Meter), ohne Ladezeit.</summary>
    public static double RouteLength(double[,] matrix, IReadOnlyList<int> route)
    {
        if (route.Count == 0) return 0;

        var total = matrix[0, route[0]] + matrix[route[^1], 0];
        for (var i = 0; i < route.Count - 1; i++)
            total += matrix[route[i], route[i + 1]];
        return total;
    }

    // ── Lokale Verbesserung ───────────────────────────────────────────────────

    private static void Improve(double[,] seconds, List<List<int>> routes, double[] costs, bool interRoute)
    {
        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var improved = Relocate(seconds, routes, costs, interRoute);
            improved |= TwoOpt(seconds, routes, costs);
            if (interRoute) improved |= Swap(seconds, routes, costs);
            if (!improved) break;
        }
    }

    /// <summary>Einen Stopp an die beste Stelle verschieben (auch in eine andere Tour).</summary>
    private static bool Relocate(double[,] seconds, List<List<int>> routes, double[] costs, bool interRoute)
    {
        var improved = false;
        for (var a = 0; a < routes.Count; a++)
        {
            for (var i = 0; i < routes[a].Count; i++)
            {
                var current = Objective(costs);
                var stop    = routes[a][i];
                routes[a].RemoveAt(i);
                var costWithout = RouteSeconds(seconds, routes[a]);

                var best = (Route: -1, Pos: 0, Cost: 0.0, Objective: current - Epsilon);
                for (var r = 0; r < routes.Count; r++)
                {
                    if (!interRoute && r != a) continue;
                    for (var pos = 0; pos <= routes[r].Count; pos++)
                    {
                        if (r == a && pos == i) continue;

                        routes[r].Insert(pos, stop);
                        var cost = RouteSeconds(seconds, routes[r]);
                        routes[r].RemoveAt(pos);

                        var objective = r == a
                            ? Objective(costs, a, cost)
                            : Objective(costs, a, costWithout, r, cost);
                        if (objective < best.Objective) best = (r, pos, cost, objective);
                    }
                }

                if (best.Route < 0)
                {
                    routes[a].Insert(i, stop);
                    continue;
                }

                routes[best.Route].Insert(best.Pos, stop);
                if (best.Route != a) costs[a] = costWithout;
                costs[best.Route] = best.Cost;
                improved = true;
            }
        }
        return improved;
    }

    /// <summary>Zwei Stopps aus verschiedenen Touren tauschen.</summary>
    private static bool Swap(double[,] seconds, List<List<int>> routes, double[] costs)
    {
        var improved = false;
        for (var a = 0; a < routes.Count; a++)
        for (var b = a + 1; b < routes.Count; b++)
        for (var i = 0; i < routes[a].Count; i++)
        for (var j = 0; j < routes[b].Count; j++)
        {
            var current = Objective(costs);
            (routes[a][i], routes[b][j]) = (routes[b][j], routes[a][i]);
            var costA = RouteSeconds(seconds, routes[a]);
            var costB = RouteSeconds(seconds, routes[b]);

            if (Objective(costs, a, costA, b, costB) < current - Epsilon)
            {
                costs[a] = costA;
                costs[b] = costB;
                improved = true;
            }
            else
            {
                (routes[a][i], routes[b][j]) = (routes[b][j], routes[a][i]);
            }
        }
        return improved;
    }

    /// <summary>Ein Teilstück einer Tour umdrehen (entfernt Überkreuzungen).</summary>
    private static bool TwoOpt(double[,] seconds, List<List<int>> routes, double[] costs)
    {
        var improved = false;
        for (var r = 0; r < routes.Count; r++)
        {
            var route = routes[r];
            for (var i = 0; i < route.Count - 1; i++)
            for (var j = i + 1; j < route.Count; j++)
            {
                route.Reverse(i, j - i + 1);
                var cost = RouteSeconds(seconds, route);
                if (cost < costs[r] - Epsilon)
                {
                    costs[r] = cost;
                    improved = true;
                }
                else
                {
                    route.Reverse(i, j - i + 1);
                }
            }
        }
        return improved;
    }

    // ── Hilfsfunktionen ───────────────────────────────────────────────────────

    private static List<int> NearestNeighbour(double[,] seconds, IReadOnlyList<int> stops)
    {
        var remaining = stops.ToList();
        var ordered   = new List<int>(remaining.Count);
        var position  = 0;
        while (remaining.Count > 0)
        {
            var next = remaining.OrderBy(s => seconds[position, s]).ThenBy(s => s).First();
            remaining.Remove(next);
            ordered.Add(next);
            position = next;
        }
        return ordered;
    }

    private static double Objective(double[] costs) => Objective(costs, -1, 0);

    // Gesamtzeit + längste Tour + gewichtetes Mittel (Σc²/Σc). Der letzte Term belohnt auch
    // Verschiebungen zwischen kürzeren Touren, die Gesamtzeit und längste Tour nicht ändern –
    // sonst bleibt z.B. ein Fahrzeug mit zwei Stopps neben einem mit neun.
    private static double Objective(double[] costs, int r1, double c1, int r2 = -1, double c2 = 0)
    {
        double sum = 0, max = 0, squares = 0;
        for (var k = 0; k < costs.Length; k++)
        {
            var c = k == r1 ? c1 : k == r2 ? c2 : costs[k];
            sum     += c;
            squares += c * c;
            max      = Math.Max(max, c);
        }
        var weightedMean = sum > 0 ? squares / sum : 0;
        return sum + BalanceWeight * (max + weightedMean);
    }

    private static RoutePlan ToPlan(List<List<int>> routes, double[] costs) =>
        new(routes.Select(r => (IReadOnlyList<int>)r.ToList()).ToList(), costs.ToList());
}
