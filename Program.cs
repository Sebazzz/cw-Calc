using System;

namespace Calc
{
    class Program
    {
        static void Main(string[] args)
        {
            string exp = "12* 123/-(-5 + 2)";
            double expectedResult = 12d* 123d/-(-5d + 2d);

            double result = new Evaluator().Evaluate(exp);

            Console.WriteLine($"{exp} = {result} ({expectedResult})");

            Console.ReadKey();
        }
    }
}
