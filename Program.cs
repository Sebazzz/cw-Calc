using System;

namespace Calc
{
    class Program
    {
        static void Main(string[] args)
        {
            string exp = "(123.45*(678.90 / (-2.5+ 11.5)-(((80 -(19))) *33.25)) / 20) - (123.45*(678.90 / (-2.5+ 11.5)-(((80 -(19))) *33.25)) / 20) + (13 - 2)/ -(-11) ";
            double expectedResult = (123.45*(678.90 / (-2.5+ 11.5)-(((80 -(19))) *33.25)) / 20) - (123.45*(678.90 / (-2.5+ 11.5)-(((80 -(19))) *33.25)) / 20) + (13 - 2)/ -(-11d) ;

            double result = new Evaluator().Evaluate(exp);

            Console.WriteLine($"{exp} = {result} ({expectedResult})");

            Console.ReadKey();
        }
    }
}
