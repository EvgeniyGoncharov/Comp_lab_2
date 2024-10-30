using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class FiniteAutomaton
{
	private Dictionary<(string, char), List<string>> _transitions = new();
	private string _initialState = "q0";
	private HashSet<string> _finalStates = new();
	private bool _isDeterministic = true;

	public FiniteAutomaton(string filePath)
	{
		LoadTransitions(filePath);
		CheckDeterminism();
	}

	private void LoadTransitions(string filePath)
	{
		string pattern = @"(q|f)(\d+),([a-zA-Z0-9])=(q|f)(\d+)";

		foreach (string line in File.ReadLines(filePath))
		{
			var match = Regex.Match(line, pattern);
			if (!match.Success)
			{
				Console.WriteLine($"Ошибка в формате строки: {line}");
				continue;
			}

			string currentState = match.Groups[1].Value + match.Groups[2].Value;
			char symbol = match.Groups[3].Value[0];
			string nextState = match.Groups[4].Value + match.Groups[5].Value;

			var key = (currentState, symbol);
			if (!_transitions.ContainsKey(key))
				_transitions[key] = new List<string>();

			_transitions[key].Add(nextState);

			if (nextState.StartsWith("f"))
			{
				_finalStates.Add(nextState);
			}
		}
	}

	private void CheckDeterminism()
	{
		foreach (var key in _transitions.Keys)
		{
			if (_transitions[key].Count > 1)
			{
				_isDeterministic = false;
				break;
			}
		}
	}

	public void PrintDeterministicEquivalent()
	{
		if (_isDeterministic)
		{
			Console.WriteLine("Автомат является детерминированным.");
		}
		else
		{
			Console.WriteLine("Автомат является недетерминированным.");
			Console.WriteLine("Переходы для детерминированного автомата:");

			var deterministicTransitions = new Dictionary<(string, char), string>();
			var newStates = new Queue<HashSet<string>>();
			var stateMapping = new Dictionary<HashSet<string>, string>(HashSet<string>.CreateSetComparer());

			newStates.Enqueue(new HashSet<string> { _initialState });
			stateMapping[new HashSet<string> { _initialState }] = _initialState;

			while (newStates.Count > 0)
			{
				var currentStates = newStates.Dequeue();
				string currentStateName = stateMapping[currentStates];

				foreach (var symbol in _transitions.Values.SelectMany(t => t).SelectMany(t => t).Distinct())
				{
					var nextStates = new HashSet<string>();

					foreach (var state in currentStates)
					{
						var key = (state, symbol);
						if (_transitions.ContainsKey(key))
						{
							nextStates.UnionWith(_transitions[key]);
						}
					}

					if (nextStates.Count > 0)
					{
						if (!stateMapping.ContainsKey(nextStates))
						{
							string newStateName = $"q{stateMapping.Count}";
							stateMapping[nextStates] = newStateName;
							newStates.Enqueue(nextStates);
						}

						deterministicTransitions[(currentStateName, symbol)] = stateMapping[nextStates];
					}
				}
			}

			foreach (var transition in deterministicTransitions)
			{
				Console.WriteLine($"{transition.Key.Item1},{transition.Key.Item2}={transition.Value}");
			}
		}
	}

	public bool AnalyzeString(string input)
	{
		string currentState = _initialState;

		foreach (char symbol in input)
		{
			if (!_transitions.TryGetValue((currentState, symbol), out List<string> nextStates))
			{
				Console.WriteLine($"Ошибка: нет перехода из состояния {currentState} по символу '{symbol}'.");
				return false;
			}

			if (nextStates.Count > 1)
			{
				Console.WriteLine("Ошибка: автомат не является детерминированным для анализа строки.");
				return false;
			}

			currentState = nextStates[0];
			Console.WriteLine($"Перешли в состояние {currentState}");
		}

		if (_finalStates.Contains(currentState))
		{
			Console.WriteLine("Строка успешно принята автоматом.");
			return true;
		}
		else
		{
			Console.WriteLine("Ошибка: автомат не завершил работу в конечном состоянии.");
			return false;
		}
	}
}

public class Program
{
	public static void Main()
	{
		while (true)
		{
			Console.WriteLine("Введите путь к файлу с описанием автомата:");
			string filePath = Console.ReadLine();

			Console.WriteLine("Введите строку для анализа:");
			string input = Console.ReadLine();

			try
			{
				var automaton = new FiniteAutomaton(filePath);
				automaton.PrintDeterministicEquivalent();
				bool result = automaton.AnalyzeString(input);
				Console.WriteLine(result ? "Строка принята" : "Строка не принята");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка: {ex.Message}");
			}
		}
		
	}
}
