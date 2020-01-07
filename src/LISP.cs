using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experiment
{
	class LISP
	{
		// All data in LISP is stored in expressiontrees. To represent such tree,
		// some classes are defined. The class AbstractNode is used as a base class.
		// There are classes to represent a 'cons' node, an integer, a string, and
		// an ident node. The 'cons' node consists of a pair of AbstractNodes,
		// where the first is called 'car' and the second 'cdr' following the
		// traditional names that are used in LISP implementations. See:
		// https://en.wikipedia.org/wiki/CAR_and_CDR
		//
		// To most trickest part of the code is the ToString method for lists.
		// When printing lists in LISP, the dot notation is avoided, meaning that
		// an expression like (1 . (2 . nil)) is printed like (1 2)
		 
		public abstract class AbstractNode {};
		
		public class ConsNode : AbstractNode
		{
			public AbstractNode Car;
			public AbstractNode Cdr;
	        public override string ToString()
	            => Cdr == null 
	               ? $"({Car?.ToString() ?? ""})"
	               : Cdr is ConsNode cn 
	               ? $"({Car?.ToString() ?? "nil"} {cn.ToStringNoBrackets()})"
	               : $"({Car?.ToString() ?? "nil"} . {Cdr.ToString()})";
	        public string ToStringNoBrackets()
	            => Cdr == null 
	               ? $"{Car?.ToString() ?? "nil"}"
	               : Cdr is ConsNode cn 
	               ? $"{Car?.ToString() ?? "nil"} {cn.ToStringNoBrackets()}"
	               : $"{Car?.ToString() ?? "nil"} . {Cdr.ToString()}";
		};
		public class IntegerNode : AbstractNode
		{
			public int Number;
	        public override string ToString() => $"{Number}";
		}
		public class StringNode : AbstractNode
		{
			public String Value;
			public override string ToString() => $"\"{Value}\"";
		}
		public class IdentNode : AbstractNode
		{
			public String Name;
			public override string ToString() => $"{Name}";
		}

		
		// Some test for verifying that the ToString methods work correctly:
		
		private static void TestToString()
		{
			Test(new IntegerNode() { Number = 123 }, "123");
			Test(new StringNode() { Value = "abc" }, "\"abc\"");
			Test(new IdentNode() { Name = "xyz" }, "xyz");
			Test(new ConsNode() {}, "()");
			AbstractNode one = new IntegerNode() { Number = 1 };
			Test(new ConsNode() { Car = one }, "(1)");
			Test(new ConsNode() { Cdr = one }, "(nil . 1)");
			Test(new ConsNode() { Car = one, Cdr = one }, "(1 . 1)");
			Test(new ConsNode() { Car = one, Cdr = new ConsNode() { Car = one }}, "(1 1)");
			Test(new ConsNode() { Car = one, Cdr = new ConsNode() { Cdr = one }}, "(1 nil . 1)");

			bool Test(AbstractNode tree, String expected)
			{
				String result = tree.ToString();
				if (result != expected)
				{
					Console.WriteLine($"Error: ToString resulted in '{result}', not {expected}");
					return false;
				}
				return true;
			}
		}
		
		// The first thing to implement, is a simple parser, which can parse
		// a string into a expression tree. Local functions are used to define
		// a simple recursive decent parser. The added benefit of using local
		// functions is that we do not have to pass 'input' and 'pos' around.
		
		public static AbstractNode Parse(string input)
		{
			int pos = 0;
			return Parse();

			// The main parsing routing:			
			AbstractNode Parse()
			{
				SkipSpace();
				if (pos < input.Length && input[pos] == '(')
				{
					// Start of a dotted pair or list
					pos++;
					AbstractNode car = Parse();
					SkipSpace();
					AbstractNode cdr = null;
					if (pos < input.Length && input[pos] == '.')
					{
						// It is a dotted pair: Parse the second part
						pos++;
						cdr = Parse();
					}
					else
					{
						// It is a list: Parse the remainder of the list
						cdr = ParseRemainderList();
					}
					// Parse the closing bracket
					if (pos < input.Length && input[pos] == ')')
					{
						pos++;
						SkipSpace();
						return new ConsNode() { Car = car, Cdr = cdr };
					}
				}
				else if (IsDigit())
				{
					// Parse a positive number
					int num = 0;
					while (IsDigit())
					{
						num = 10 * num + (input[pos] - '0');
						pos++;
					}
					return new IntegerNode() { Number = num };
				}
				else if (IsAlpha())
				{
					// Parse an identifier
					String ident = "";
					while (IsAlpha() || IsDigit())
					{
						ident += input[pos];
						pos++;
					}
					return ident == "nil" ? null : new IdentNode() { Name = ident };
				}
				else if (pos < input.Length && input[pos] == '"')
				{
					// Parse a string
					pos++;
					String value = "";
					while (pos < input.Length && input[pos] != '"')
					{
						value += input[pos];
						pos++;
					}
					if (pos < input.Length && input[pos] == '"')
					{
						pos++;
						return new StringNode() { Value = value };
					}
				}
				
				return null;
			}	
					
			bool IsDigit() => pos < input.Length && '0' <= input[pos] && input[pos] <= '9';
			bool IsAlpha() => pos < input.Length && ('a' <= input[pos] && input[pos] <= 'z' || 'A' <= input[pos] && input[pos] <= 'Z');
			void SkipSpace()
			{
				while (pos < input.Length && (input[pos] == ' ' || input[pos] == '\n'  || input[pos] == '\t'))
					pos++;
			}
			AbstractNode ParseRemainderList()
			{
				SkipSpace();
				if (pos >= input.Length || input[pos] == ')')
					return null;
				AbstractNode car = Parse();
				if (car == null)
					return null;
				SkipSpace();
				if (pos < input.Length && input[pos] == '.')
				{
					pos++;
				    AbstractNode cdr = Parse();
			    	return new ConsNode() { Car = car, Cdr = cdr };
				}
				else
				{
				    AbstractNode cdr = ParseRemainderList();
				    return new ConsNode() { Car = car, Cdr = cdr };
				}
			}
		}
		
		private static void TestParsing()
		{
			Test("1", "1");
			Test("abc", "abc");
			Test("\"x\"", "\"x\"");
			Test(" 1 ", "1");
			Test("(1 . 1)", "(1 . 1)");
			Test("(1)", "(1)");
			Test("(1 2)", "(1 2)");
			Test("(1 2 . 3)", "(1 2 . 3)");
			Test("(1 a \"b\" (1 . 2))", "(1 a \"b\" (1 . 2))");
			Test("(1 . (2 3))", "(1 2 3)");
			Test("(1 . (2 . nil))", "(1 2)");
			Test("((1) 2 ())", "((1) 2 ())");
		
			bool Test(String input, String expected)
			{
				AbstractNode tree = Parse(input);
				if (tree == null)
				{
					Console.WriteLine($"Error: parsing '{input}' resulted in <null>, not {expected}");
					return false;
				}
				String result = tree.ToString();
				if (result != expected)
				{
					Console.WriteLine($"Error: parsing '{input}' resulted in '{result}', not {expected}");
					return false;
				}
				return true;
			}
		}

		static void AllTests()
		{
        	TestToString();
        	TestParsing();
		}
						
        static void Main(string[] args)
        {
        	AllTests();
        }
    }
}
