#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

public class FSMBasedControllerGenerator : EditorWindow
{
	#region Parameters

	private bool _autoOpenController = true;
	private string _controllerName;
	private List<string> _statesNames = new List<string>();
	private string _path;
	
	#endregion

	#region KeyWords

	private const string NAME = "$NAME$";
	
	private const string STATE_NAME = "$STATE_NAME$";
	private const string FIRST_STATE_NAME = "$FIRST_STATE_NAME$";
	
	private const string STATE_INSTANTIATION_LINE = "$STATE_INSTANTIATION_LINE$";
	private const string STATE_TYPE_DEFINITION_LINE = "$STATE_TYPE_DEFINITION_LINE$";

	#endregion

	#region Local

	private string PackagesFolderPath => Path.Combine(Application.dataPath, "../Packages");
	private string TemplateFolderPath => $"{PackagesFolderPath}/com.akinkursatozkan.fsm/Runtime/Templates/";
	private string BaseStateTemplatePath => $"{TemplateFolderPath}BaseStateTemplate.txt";
	private string ControllerTemplatePath => $"{TemplateFolderPath}ControllerTemplate.txt";
	private string StateInstantiationTemplatePath => $"{TemplateFolderPath}StateInstantiationTemplate.txt";
	private string StateTemplatePath => $"{TemplateFolderPath}StateTemplate.txt";
	private string StateTypeDefinitionTemplatePath => $"{TemplateFolderPath}StateTypeDefinitionTemplate.txt";

	private string ControllerFolderPath => $"{_path}/{_controllerName}";
	private string ControllerPath => $"{ControllerFolderPath}/{_controllerName}Controller.cs";
	private string StateFolderPath => $"{ControllerFolderPath}/States";
	private string BaseStatePath => $"{StateFolderPath}/{_controllerName}BaseState.cs";

	private string MakeStatePath(string stateName) => $"{StateFolderPath}/{_controllerName}{stateName}State.cs";
	
	private string StateTypeDefinitionPattern => 
		@$"\s*public static readonly {_controllerName}StateType .* = new {_controllerName}StateType\(nameof\(.*\)\);";
	
	private string StateInstantiationPattern => 
		@$"\s*new {_controllerName}.*State\(this, {_controllerName}StateType\.(.*)\)";

	private string StateMachineInitPattern => 
		@$"_stateMachine\.Init\({_controllerName}StateType\.(.*), states\);";
	
	private string StateMachineInitTemplate(string startStateName) =>
		$"_stateMachine.Init({_controllerName}StateType.{startStateName}, states);";
	
	private string MakeStateNamePattern(string stateName) => @$"{_controllerName}{stateName}State";
	
	private readonly GUILayoutOption GL_WIDTH_25 = GUILayout.Width( 25f );
	
	private int _stateCount;
	private List<string> _bufferedStatesNames = new List<string>();
	private string[] _existedStates = Array.Empty<string>();
	
	private MatchCollection _controllerStatesMatches;
	
	#endregion

	[MenuItem( "Window/Generation/ScriptGeneration/FSMGenerator")]
	private static void Init()
	{
		InitWindow();
	}

	private static void InitWindow()
	{
		FSMBasedControllerGenerator window = GetWindow<FSMBasedControllerGenerator>();
		window.InitVariables();
		window.Show();
	}

	private void InitVariables()
	{
		_path = $"{Application.dataPath}/_GameData/Scripts/Controllers";
		titleContent = new GUIContent( "FSM Generator" );
		minSize = new Vector2( 200f, 150f );
	}

	private void CreateFSM()
	{
		if (CheckIfFSMExist())
		{
			RefactorController();
			RefactorBaseState();
			RefactorStates();
		}
		else
		{
			CreateControllerFolder();
			CreateStateFolder();
			
			GenerateController();
			GenerateBasesState();
			GenerateStates();
		}

		AssetDatabase.Refresh();
		Close();

		TryOpenControllerCs();
	}

	private void TryOpenControllerCs()
	{
		if (_autoOpenController)
		{
			var relativePath = ControllerPath.Remove(0, Application.dataPath.Length - "Assets".Length);
			AssetDatabase.OpenAsset((MonoScript)AssetDatabase.LoadAssetAtPath(relativePath, typeof(MonoScript)));
		}
	}
	
	private string RemoveStatesFromFile(MatchCollection regexMatches, string file)
	{
		return file.Remove(regexMatches[0].Index, (regexMatches[regexMatches.Count - 1].Index
		                                           + regexMatches[regexMatches.Count - 1].Length)
		                                          - regexMatches[0].Index);
	}

	private string RemoveMatchFromContent(Match match, string content)
	{
		return content.Remove(match.Index, match.Length);
	}

	private bool IsStateRenamedOrDeleted(string stateName, out string replaceName)
	{
		replaceName = stateName;
		if (_statesNames.Contains(stateName))
		{
			return false;
		}
			
		var existedStatesList = _existedStates.ToList();
		var stateIndex = existedStatesList.IndexOf(stateName);

		replaceName = stateIndex < _statesNames.Count ? 
			_statesNames[stateIndex] :
			_statesNames[0];

		return true;
	}

	private void RefactorController()
	{
		string controllerContent = File.ReadAllText(ControllerPath);

		controllerContent = RemoveStatesFromFile(_controllerStatesMatches, controllerContent);
		
		controllerContent = controllerContent.Insert(_controllerStatesMatches[0].Index, 
			GetStatesTemplateImplementations($"{Environment.NewLine}\t\t\t", $",{Environment.NewLine}\t\t\t", StateInstantiationTemplatePath));
		
		//Change start state;
		var match = Regex.Match(controllerContent, StateMachineInitPattern, RegexOptions.Singleline);
		if (IsStateRenamedOrDeleted(match.Groups[1].Value, out string startStateName))
		{
			controllerContent = RemoveMatchFromContent(match, controllerContent);
			controllerContent = controllerContent.Insert(match.Index, StateMachineInitTemplate(startStateName));
		}

		File.WriteAllText(ControllerPath, controllerContent);
	}

	private bool IsStateRenamed(int index)
	{
		return (index < _existedStates.Length 
		        && !_existedStates[index].Equals(_statesNames[index]))
		       && !_existedStates.ToList().Contains(_statesNames[index]);
	}

	private void RenameState(int index)
	{
		string stateContent = File.ReadAllText(MakeStatePath(_existedStates[index]));

		string pattern = MakeStateNamePattern(_existedStates[index]);
		stateContent = Regex.Replace(stateContent, pattern, MakeStateNamePattern(_statesNames[index]), 
			RegexOptions.Multiline | RegexOptions.Compiled);

		File.Delete(MakeStatePath(_existedStates[index]));
		File.WriteAllText(MakeStatePath(_statesNames[index]), stateContent);
	}
	
	private void RefactorStates()
	{
		string stateTemplate = File.ReadAllText(StateTemplatePath);
		stateTemplate = stateTemplate.Replace(NAME, _controllerName);
		
		for (int i = 0; i < _stateCount; i++)
		{
			if (IsStateRenamed(i))
			{
				RenameState(i);
				continue;
			}
			
			GenerateState(stateTemplate, _statesNames[i]);
		}
	}
	
	private void GenerateController()
	{
		string controllerTemplate = File.ReadAllText(ControllerTemplatePath);
		
		controllerTemplate = controllerTemplate.Replace(NAME, _controllerName);
		controllerTemplate = controllerTemplate.Replace(FIRST_STATE_NAME, _statesNames[0]);
		
		controllerTemplate = controllerTemplate.Replace(STATE_INSTANTIATION_LINE, 
			GetStatesTemplateImplementations("", $",{Environment.NewLine}\t\t\t", StateInstantiationTemplatePath));
		
		File.WriteAllText(ControllerPath, controllerTemplate);
	}
	
	private void GenerateBasesState()
	{
		string baseStateTemplate = File.ReadAllText(BaseStateTemplatePath);

		baseStateTemplate = baseStateTemplate.Replace(NAME, _controllerName);
		baseStateTemplate = baseStateTemplate.Replace(STATE_TYPE_DEFINITION_LINE, 
			GetStatesTemplateImplementations("", $"{Environment.NewLine}\t", StateTypeDefinitionTemplatePath));
		
		File.WriteAllText(BaseStatePath, baseStateTemplate);
	}

	//prefix is needed because of regex deletion, implementing new states to existing file needs new line and tab.
	private string GetStatesTemplateImplementations(string prefix, string suffix, string templatePath)
	{
		string statesTemplate = File.ReadAllText(templatePath);
		
		var statesTypeDefinitions = prefix;
		statesTemplate = statesTemplate.Replace(NAME, _controllerName);
		for (int i = 0; i < _stateCount; i++)
		{
			var stateTypeDef = $"{statesTemplate.Replace(STATE_NAME, _statesNames[i])}";
			stateTypeDef += i == _stateCount - 1 ? "" : suffix; 
			statesTypeDefinitions += stateTypeDef;
		}

		return statesTypeDefinitions;
	}
	
	
	private void RefactorBaseState()
	{
		string baseStateContent = File.ReadAllText(BaseStatePath);

		var stateTypeMatches = Regex.Matches(baseStateContent, StateTypeDefinitionPattern, RegexOptions.Multiline);

		baseStateContent = RemoveStatesFromFile(stateTypeMatches, baseStateContent);
		baseStateContent = baseStateContent.Insert(stateTypeMatches[0].Index, 
			GetStatesTemplateImplementations($"{Environment.NewLine}\t", $"{Environment.NewLine}\t", StateTypeDefinitionTemplatePath));
		
		File.WriteAllText(BaseStatePath, baseStateContent);
	}

	private void GenerateStates()
	{
		string stateTemplate = File.ReadAllText(StateTemplatePath);

		stateTemplate = stateTemplate.Replace(NAME, _controllerName);
		foreach (var stateName in _statesNames)
		{
			GenerateState(stateTemplate, stateName);
		}
	}
	
	private void GenerateState(string stateTemplate, string stateName)
	{
		var statePath = MakeStatePath(stateName);
		if (File.Exists(statePath))
		{
			return;
		}
		
		stateTemplate = stateTemplate.Replace(STATE_NAME, stateName);
		File.WriteAllText(statePath, stateTemplate);
	}

	private void CreateControllerFolder()
	{
		Directory.CreateDirectory(ControllerFolderPath);
	}

	private void CreateStateFolder()
	{
		Directory.CreateDirectory(StateFolderPath);
	}
	
	private string PathField(string label, string value)
	{
		GUILayout.BeginHorizontal();
		value = EditorGUILayout.TextField(label, value);
		if(GUILayout.Button("o", GL_WIDTH_25))
		{
			string selectedPath = EditorUtility.OpenFolderPanel("Choose output directory", "", "");
			if (!string.IsNullOrEmpty(selectedPath))
			{
				value = selectedPath;
			}

			GUIUtility.keyboardControl = 0; // Remove focus from active text field
		}
		GUILayout.EndHorizontal();

		return value;
	}

	private void ButtonField(string label, Action OnClick)
	{
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(label))
		{
			OnClick?.Invoke();
		}
		GUILayout.EndHorizontal();
	}
	
	private string TextField(string label, string value)
	{
		GUILayout.BeginHorizontal();
		value = EditorGUILayout.TextField(label, value);
		GUILayout.EndHorizontal();

		return value;
	}
	
	private bool ToggleField(string label, bool value)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label);
		value = EditorGUILayout.Toggle(value, GUILayout.MaxWidth(64f));
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		return value;
	}
	
	private int IntField(string label, int value)
	{
		GUILayout.BeginHorizontal();
		value = EditorGUILayout.IntField(label, value);
		GUILayout.EndHorizontal();

		return value;
	}

	private void StateField()
	{
		EditorGUI.BeginChangeCheck();
		var newStateCount = IntField("State Count:", _stateCount);
		if (EditorGUI.EndChangeCheck())
		{
			var stateNumDiff = newStateCount - _stateCount;
			if (stateNumDiff > 0)
			{
				_bufferedStatesNames.AddRange(Enumerable.Repeat("Empty", stateNumDiff));
			}
			_stateCount = newStateCount;
		}
		
		for (int i = 0; i < _stateCount; i++)
		{
			_bufferedStatesNames[i] = TextField(i.ToString(), _bufferedStatesNames[i]);
		}
			
		_statesNames = _bufferedStatesNames.GetRange(0, _stateCount);
	}

	private bool CheckIfFSMExist()
	{
		return File.Exists(ControllerPath);
	}

	private void CatchAndFillStates()
	{
		string controllerContent = File.ReadAllText(ControllerPath);

		_controllerStatesMatches = Regex.Matches(controllerContent, StateInstantiationPattern, RegexOptions.Multiline);
				
		//Get existed states
		_existedStates = new string[_controllerStatesMatches.Count];
		for (int i = 0; i < _controllerStatesMatches.Count; i++)
		{
			_existedStates[i] = _controllerStatesMatches[i].Groups[1].Value;
		}
		_bufferedStatesNames = _existedStates.ToList();
		_stateCount = _bufferedStatesNames.Count;
	}

	private void ControllerNameField()
	{
		EditorGUI.BeginChangeCheck();
		_controllerName = TextField("Controller Name:", _controllerName);
		if (EditorGUI.EndChangeCheck()) 
		{
			if (CheckIfFSMExist())
			{
				CatchAndFillStates();
			}
		}
	}

	private void PathField()
	{
		_path = PathField("Save to:", _path);
	}

	private void OnGUI()
	{
		GUI.enabled = true;

		PathField();
		GUILayout.Space(10);

		var controllerName = string.IsNullOrEmpty(_controllerName) ? "Controller" : $"{_controllerName}Controller.cs";
		_autoOpenController = ToggleField($"Auto Open {controllerName}  ", _autoOpenController);
		GUILayout.Space(10);

		ControllerNameField();
		GUILayout.Space(10);
		
		StateField();
		GUILayout.Space(10);
		
		ButtonField("Create FSM", CreateFSM);
	}
}
#endif	