using System;
using System.Collections.Generic;
using System.Linq;

namespace TransportSolver
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Write("Введіть кількість постачальників: ");
            int m = int.Parse(Console.ReadLine());

            Console.Write("Введіть кількість споживачів: ");
            int n = int.Parse(Console.ReadLine());

            int[,] cost = new int[m, n];
            int[] supply = new int[m];
            int[] demand = new int[n];

            Console.WriteLine("\nВведіть матрицю вартостей (розміром {0}x{1}):", m, n);
            for (int i = 0; i < m; i++)
            {
                Console.WriteLine($"Рядок {i + 1}:");
                for (int j = 0; j < n; j++)
                {
                    Console.Write($"  Вартість[{i + 1},{j + 1}]: ");
                    cost[i, j] = int.Parse(Console.ReadLine());
                }
            }

            Console.WriteLine("\nВведіть запаси для кожного постачальника:");
            for (int i = 0; i < m; i++)
            {
                Console.Write($"  Постачальник {i + 1}: ");
                supply[i] = int.Parse(Console.ReadLine());
            }

            Console.WriteLine("\nВведіть потреби для кожного споживача:");
            for (int j = 0; j < n; j++)
            {
                Console.Write($"  Споживач {j + 1}: ");
                demand[j] = int.Parse(Console.ReadLine());
            }

            var nwPlan = NorthWestCorner(cost, supply, demand);
            var minplan = MinCostMethod(cost, supply, demand);
            Console.WriteLine(" ");
            Console.WriteLine("Пошук оптимального плану перевезень методом потенціалів:");
            var potenplan = Potentials(cost, minplan);
        }

        // Метод Північно-Західного кута для побудови початкового опорного плану
        static int[,] NorthWestCorner(int[,] cost, int[] Supply, int[] Demand)
        {
            int row = Supply.Length;
            int col = Demand.Length;
            int[,] plannorthwest = InitializePlanMatrix(row, col);
            int[] supply = Supply.ToArray();
            int[] demand = Demand.ToArray();

            int i = 0, j = 0;

            // Збираємо послідовність кроків у список рядків
            var steps = new List<string>();

            while (i < row && j < col)
            {
                int allocation = Math.Min(supply[i], demand[j]);
                plannorthwest[i, j] = allocation;

                // Додаємо крок у форматі (xij = value), індексація для користувача з 1
                steps.Add($"(x{i + 1}{j + 1} = {allocation})");

                supply[i] -= allocation;
                demand[j] -= allocation;

                if (supply[i] == 0) i++;
                if (demand[j] == 0) j++;
            }
            Console.WriteLine(" ");
            Console.WriteLine("План Північно-Західного кута:");
            Console.WriteLine(" ");
            // Вивід послідовності заповнення
            Console.WriteLine("Послідовність заповнення таблиці:");
            Console.WriteLine(string.Join("->", steps));
            Console.WriteLine(" ");
            Console.WriteLine("Опорний план перевезень:");
            PrintPlan(plannorthwest);
            PrintCost("Вартість:", cost, plannorthwest);
            return plannorthwest;
        }

        // Метод мінімального елемента — початковий опорний план на основі найменшої вартості
        static int[,] MinCostMethod(int[,] cost, int[] Supply, int[] Demand)
        {
            int row = Supply.Length;
            int col = Demand.Length;
            int[,] planlmincost = InitializePlanMatrix(row, col);
            int[] supply = Supply.ToArray(), demand = Demand.ToArray();
            var Cells = new List<(int i, int j, int c)>();

            for (int i = 0; i < row; i++)
                for (int j = 0; j < col; j++)
                    Cells.Add((i, j, cost[i, j]));

            foreach (var (i, j, _) in Cells.OrderBy(c => c.c))
            {
                int allocation = Math.Min(supply[i], demand[j]);
                if (allocation > 0)
                {
                    planlmincost[i, j] = allocation;
                    supply[i] -= allocation;
                    demand[j] -= allocation;
                }
            }

            Console.WriteLine(" ");
            Console.WriteLine("План мінімального елемента");
            Console.WriteLine(" ");
            FixDegeneracy(planlmincost, row, col);
            Console.WriteLine("Опорний план перевезень:");
            PrintPlan(planlmincost);
            PrintCost("Вартість:", cost, planlmincost);
            return planlmincost;
        }

        // Метод потенціалів для пошуку оптимального плану на основі початкового базисного плану
        static int[,] Potentials(int[,] cost, int[,] planpotentials)
        {
            int row = cost.GetLength(0);
            int col = cost.GetLength(1);
            planpotentials = (int[,])planpotentials.Clone();
            FixDegeneracy(planpotentials, row, col);

            int iter = 0;

            while (true)
            {
                iter++;
                Console.WriteLine($"\nІтерація {iter}");

                int?[] u = new int?[row];
                int?[] v = new int?[col];
                u[0] = 0;

                // Обчислюємо потенціали (лише по базису)
                bool changed;
                do
                {
                    changed = false;
                    for (int i = 0; i < row; i++)
                    {
                        for (int j = 0; j < col; j++)
                        {
                            if (planpotentials[i, j] >= 0)
                            {
                                if (u[i].HasValue && !v[j].HasValue)
                                {
                                    v[j] = cost[i, j] - u[i];
                                    changed = true;
                                }
                                else if (!u[i].HasValue && v[j].HasValue)
                                {
                                    u[i] = cost[i, j] - v[j];
                                    changed = true;
                                }
                            }
                        }
                    }
                } while (changed);

                // Вивід потенціалів
                Console.WriteLine("Потенціали постачальників:");
                for (int i = 0; i < row; i++)
                    Console.Write($"{(u[i]?.ToString() ?? "x")} ");
                Console.WriteLine();

                Console.WriteLine("Потенціали споживачів:");
                for (int j = 0; j < col; j++)
                    Console.Write($"{(v[j]?.ToString() ?? "x")} ");
                Console.WriteLine();

                Console.WriteLine("Непрямі вартості:");
                int[,] deltaMatrix = new int[row, col];
                List<(int, int)> problematicCells = new List<(int, int)>();
                int bestI = -1, bestJ = -1, minDelta = 0;

                // Обчислюємо всі delta наперед
                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < col; j++)
                    {
                        if (planpotentials[i, j] >= 0)
                        {
                            deltaMatrix[i, j] = int.MaxValue; // Позначаємо як "x"
                        }
                        else
                        {
                            deltaMatrix[i, j] = cost[i, j] - (u[i] ?? 0) - (v[j] ?? 0);
                            if (deltaMatrix[i, j] < minDelta)
                            {
                                minDelta = deltaMatrix[i, j];
                                bestI = i;
                                bestJ = j;
                            }
                            if (deltaMatrix[i, j] < 0)
                                problematicCells.Add((i, j));
                        }
                    }
                }

                // Правильне відображення delta в таблиці
                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < col; j++)
                    {
                        if (deltaMatrix[i, j] == int.MaxValue)
                            Console.Write("x ");
                        else
                            Console.Write($"{deltaMatrix[i, j]} ");
                    }
                    Console.WriteLine();
                }


                // Аналіз оптимальності
                if (minDelta >= 0)
                {
                    Console.WriteLine("Умова оптимальності виконується.");
                    Console.WriteLine("\nОптимальний план:");
                    PrintPlan(planpotentials);
                    PrintCost("Оптимальна вартість:", cost, planpotentials);
                    return planpotentials;
                }
                else
                {
                    Console.WriteLine("Умова оптимальності не виконується.");
                    Console.WriteLine("Знайдено «проблемні» клітини:");
                    foreach (var (i, j) in problematicCells)
                        Console.WriteLine($"[{i + 1}, {j + 1}]");
                }

                // Побудова циклу заміщення
                var cycle = FindAdjustmentCycle(GetBasicCells(planpotentials), (bestI, bestJ));

                int theta = cycle.Where((_, idx) => idx % 2 == 1).Min(p => planpotentials[p.i, p.j]);
                Console.WriteLine($"Знайдено значення λ: {theta}");

                for (int k = 0; k < cycle.Count; k++)
                {
                    var (i, j) = cycle[k];
                    planpotentials[i, j] += (k % 2 == 0) ? theta : -theta;
                }

                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < col; j++)
                    {
                        if (planpotentials[i, j] == 0 && IsBasicCell(planpotentials, i, j))
                        {
                            planpotentials[i, j] = -1;
                        }
                    }
                }

                planpotentials[bestI, bestJ] = theta;

                Console.WriteLine("Новий план перевезень:");
                PrintPlan(planpotentials);
                PrintCost("Вартість перевезень за новим планом:", cost, planpotentials);

                FixDegeneracy(planpotentials, row, col);
            }
        }

        // Допоміжна функція ініціалізації плану - заповнення -1
        static int[,] InitializePlanMatrix(int m, int n)
        {
            int[,] plan = new int[m, n];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    plan[i, j] = -1;
            return plan;
        }

        // Виправлення виродженості — забезпечення, що у базисі не менше m+n-1 опорних елементів
        static void FixDegeneracy(int[,] plan, int m, int n)
        {
            var all = GetAllCells(plan);
            int count = plan.Cast<int>().Count(v => v >= 0);
            foreach (var (i, j) in all)
            {
                if (count >= m + n - 1) break;
                if (plan[i, j] < 0)
                {
                    plan[i, j] = 0;
                    count++;
                }
            }
        }

        static bool IsBasicCell(int[,] plan, int i, int j) => plan[i, j] >= 0;

        // Повертає список усіх координат клітинок у плані
        static List<(int row, int col)> GetAllCells(int[,] plan)
        {
            int rowCount = plan.GetLength(0);
            int colCount = plan.GetLength(1);
            var cells = new List<(int row, int col)>();

            for (int row = 0; row < rowCount; row++)
                for (int col = 0; col < colCount; col++)
                    cells.Add((row, col));

            return cells;
        }

        // Повертає список координат усіх базисних клітинок у плані
        static List<(int row, int col)> GetBasicCells(int[,] plan)
        {
            var basicCells = new List<(int row, int col)>();
            int rowCount = plan.GetLength(0);
            int colCount = plan.GetLength(1);

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                {
                    if (IsBasicCell(plan, row, col))
                        basicCells.Add((row, col));
                }
            }

            return basicCells;
        }

        // Пошук циклу в таблиці для методу потенціалів (покращення плану)
        static List<(int i, int j)> FindAdjustmentCycle(List<(int i, int j)> basis, (int i, int j) entry)
        {
            var nodes = new List<(int, int)>(basis) { entry };
            int N = nodes.Count, start = N - 1;
            var parent = new Dictionary<(int, bool), (int, bool)>();
            var queue = new Queue<(int, bool)>();

            queue.Enqueue((start, true));
            queue.Enqueue((start, false));

            while (queue.Count > 0)
            {
                var (cur, byRow) = queue.Dequeue();
                var (ci, cj) = nodes[cur];
                for (int k = 0; k < N; k++)
                {
                    if (k == cur) continue;
                    var (ni, nj) = nodes[k];
                    if ((byRow && ci != ni) || (!byRow && cj != nj)) continue;

                    var next = (k, !byRow);
                    if (parent.ContainsKey(next)) continue;

                    parent[next] = (cur, byRow);
                    if (k == start)
                    {
                        var path = new List<(int, int)>();
                        var state = (cur, byRow);
                        while (true)
                        {
                            path.Add(nodes[state.Item1]);
                            if (state.Item1 == start) break;
                            state = parent[state];
                        }
                        path.Reverse();
                        return path;
                    }
                    queue.Enqueue(next);
                }
            }
            throw new Exception("Цикл не знайдено");
        }

        // Вивід плану у вигляді таблиці
        static void PrintPlan(int[,] plan)
        {
            int m = plan.GetLength(0), n = plan.GetLength(1);
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                    Console.Write(plan[i, j] >= 0 ? $"{plan[i, j],3}" : " x");
                Console.WriteLine();
            }
        }

        // Вивід загальної вартості плану
        static void PrintCost(string label, int[,] cost, int[,] plan)
        {
            int m = plan.GetLength(0), n = plan.GetLength(1), total = 0;
            var terms = new List<string>();
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    if (plan[i, j] > 0)
                    {
                        total += plan[i, j] * cost[i, j];
                        terms.Add($"{plan[i, j]}*{cost[i, j]}");
                    }
            Console.WriteLine($"{label} S = {string.Join(" + ", terms)} = {total}");
        }
    }
}
