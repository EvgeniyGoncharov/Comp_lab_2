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

	public void ConvertToDeterministic()
	{
		if (_isDeterministic)
		{
			Console.WriteLine("Автомат уже является детерминированным.");
			return;
		}

		Console.WriteLine("Конвертирование недетерминированного автомата в детерминированный...");

		var dfaTransitions = new Dictionary<(string, char), string>();
		var newStates = new Queue<HashSet<string>>();
		var stateMapping = new Dictionary<HashSet<string>, string>(HashSet<string>.CreateSetComparer());
		var dfaFinalStates = new HashSet<string>();

		newStates.Enqueue(new HashSet<string> { _initialState });
		stateMapping[new HashSet<string> { _initialState }] = _initialState;

		while (newStates.Count > 0)
		{
			var currentStates = newStates.Dequeue();

			// Проверка, если хотя бы одно состояние из currentStates является конечным в НКА
			bool isFinalState = currentStates.Any(s => _finalStates.Contains(s));
			string currentStateName = isFinalState ? $"f{stateMapping.Count}" : $"q{stateMapping.Count}";

			if (!stateMapping.ContainsKey(currentStates))
			{
				stateMapping[currentStates] = currentStateName;
			}
			else
			{
				currentStateName = stateMapping[currentStates];
			}

			if (isFinalState)
			{
				dfaFinalStates.Add(currentStateName); // Добавление в конечные состояния ДКА
			}

			foreach (char symbol in _transitions.Keys.Select(t => t.Item2).Distinct())
			{
				var nextStates = new HashSet<string>();

				foreach (var state in currentStates)
				{
					var key = (state, symbol);
					if (_transitions.TryGetValue(key, out List<string> stateTransitions))
					{
						nextStates.UnionWith(stateTransitions);
					}
				}

				if (nextStates.Count > 0)
				{
					if (!stateMapping.ContainsKey(nextStates))
					{
						// Присваиваем новое имя для нового состояния, добавляя "f", если оно конечное
						bool nextIsFinalState = nextStates.Any(s => _finalStates.Contains(s));
						string newStateName = nextIsFinalState ? $"f{stateMapping.Count}" : $"q{stateMapping.Count}";

						stateMapping[nextStates] = newStateName;
						newStates.Enqueue(nextStates);
						if (nextIsFinalState)
						{
							dfaFinalStates.Add(newStateName);
						}
					}

					dfaTransitions[(currentStateName, symbol)] = stateMapping[nextStates];
				}
			}
		}

		Console.WriteLine("Переходы для детерминированного автомата:");
		foreach (var transition in dfaTransitions)
		{
			string fromState = dfaFinalStates.Contains(transition.Key.Item1) ? $"f{transition.Key.Item1.Substring(1)}" : transition.Key.Item1;
			string toState = dfaFinalStates.Contains(transition.Value) ? $"f{transition.Value.Substring(1)}" : transition.Value;
			Console.WriteLine($"{fromState},{transition.Key.Item2}={toState}");
		}

		_transitions = dfaTransitions.ToDictionary(x => x.Key, x => new List<string> { x.Value });
		_finalStates = dfaFinalStates;
		_isDeterministic = true;
	}

	public bool AnalyzeString(string input)
	{
		if (!_isDeterministic)
		{
			Console.WriteLine("Для анализа строки необходимо преобразовать автомат в детерминированный.");
			ConvertToDeterministic();
		}

		string currentState = _initialState;

		foreach (char symbol in input)
		{
			if (!_transitions.TryGetValue((currentState, symbol), out List<string> nextStates))
			{
				Console.WriteLine($"Ошибка: нет перехода из состояния {currentState} по символу '{symbol}'.");
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
			string filePath = "C:\\Users\\Evgen\\Desktop\\graph\\graph.txt";

			Console.WriteLine("Введите строку для анализа:");
			string input = Console.ReadLine();

			try
			{
				var automaton = new FiniteAutomaton(filePath);
				automaton.ConvertToDeterministic();
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
