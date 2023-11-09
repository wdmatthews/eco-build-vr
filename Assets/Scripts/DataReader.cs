using System.Collections.Generic;
using UnityEngine;

public class DataReader : MonoBehaviour
{
    private static readonly Dictionary<string, int> _hourlyStartByVariable = new()
    {
        { "Air Relative Humidity (%)", 0 },
        { "Cooling Energy (J)", 27 },
        { "Heating Energy (J)", 54 },
        { "Air Temperature (T)", 81 },
        { "Electricity Energy (J)", 108 },
    };

    [SerializeField] private Transform[] _graphLocations = null;
    [SerializeField] private Camera _uiCamera = null;
    [SerializeField] private DataGraph _graphPrefab = null;
    [SerializeField] private string[] _zones = { };
    [SerializeField] private string _hourlyFileName = "AlumniHourly";

    private Dictionary<string, DataGraph> _graphByRoom = new();
    private Dictionary<string, Dictionary<string, float[]>> _hourlyByRoomAndVariable = new();

    private void Awake()
    {
        LoadHourly();
        CreateGraphs();
    }

    private void LoadHourly()
    {
        TextAsset data = Resources.Load<TextAsset>(_hourlyFileName);
        string[] rows = data.text.Split("\n");
        string[] labels = rows[0].Split(",");
        float[][] parsedData = new float[labels.Length - 1][];

        for (int c = 0; c < labels.Length - 1; c++)
        {
            parsedData[c] = new float[rows.Length - 1];
        }

        for (int r = 1; r < rows.Length; r++)
        {
            string[] row = rows[r].Split(",");
            if (row.Length < labels.Length) { continue; }

            for (int c = 1; c < labels.Length; c++)
            {
                parsedData[c - 1][r - 1] = float.Parse(row[c]);
            }
        }

        for (int z = 0; z < _zones.Length; z++)
        {
            string zone = _zones[z];
            Dictionary<string, float[]> roomData = new();

            foreach (string variable in _hourlyStartByVariable.Keys)
            {
                roomData.Add(variable, parsedData[_hourlyStartByVariable[variable] + z]);
            }

            _hourlyByRoomAndVariable.Add(zone, roomData);
        }
    }

    private void CreateGraphs()
    {
        for (int z = 0; z < _zones.Length; z++)
        {
            string zone = _zones[z];
            DataGraph graph = Instantiate(_graphPrefab, _graphLocations[z]);
            graph.Initialize(zone);
            graph.Camera = _uiCamera;
            graph.HourlyDataByVariable = _hourlyByRoomAndVariable[zone];
            _graphByRoom.Add(zone, graph);
            graph.UpdateBars();
        }
    }
}
