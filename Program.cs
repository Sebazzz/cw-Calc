using System;

namespace Calc
{
    class Program
    {
        static void Main(string[] args)
        {
            string exp = "(1 - 2) + -(-(-(-4)))";
            double expectedResult = (1 - 2) + -(-(-(-4)));

            double result = new Evaluator().Evaluate(exp);

            Console.WriteLine($"{exp} = {result} ({expectedResult})");

            Console.ReadKey();
        }
    }
}
