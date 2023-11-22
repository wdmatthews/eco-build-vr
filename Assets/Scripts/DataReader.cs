using System.Collections.Generic;
using UnityEngine;

public class DataReader : MonoBehaviour
{
    private static readonly Dictionary<string, int> _hourlyStartByVariable = new()
    {
        { "Air Relative Humidity (%)", 0 },
        { "Cooling Energy (J)", 81 },
        { "Heating Energy (J)", 162 },
        { "Air Temperature (C)", 243 },
        { "Electricity Energy (J)", 324 },
        { "Lights Electricity Energy (J)", 405 },
    };

    [SerializeField] private Transform[] _graphLocations = { };
    [SerializeField] private Material[] _floorMaterials = { };
    [SerializeField] private Camera _uiCamera = null;
    [SerializeField] private DataGraph _graphPrefab = null;
    [SerializeField] private string[] _zones = { };
    [SerializeField] private string _hourlyFileName = "AlumniHourly";

    private Dictionary<string, DataGraph> _graphByRoom = new();
    private float[][] _parsedData = null;
    private Dictionary<string, Dictionary<string, float[]>> _hourlyByRoomAndVariable = new();
    private Dictionary<string, Dictionary<string, float[]>> _monthlyByRoomAndVariable = new();
    private Dictionary<string, Dictionary<string, float>> _annualByRoomAndVariable = new();

    private void Awake()
    {
        LoadData();
        LoadHourly();
        LoadMonthly();
        LoadAnnual();
        CreateGraphs();
    }

    private void LoadData()
    {
        TextAsset data = Resources.Load<TextAsset>(_hourlyFileName);
        string[] rows = data.text.Split("\n");
        string[] labels = rows[0].Split(",");
        _parsedData = new float[labels.Length - 1][];

        for (int c = 0; c < labels.Length - 1; c++)
        {
            _parsedData[c] = new float[rows.Length - 1];
        }

        for (int r = 1; r < rows.Length; r++)
        {
            string[] row = rows[r].Split(",");
            if (row.Length < labels.Length) { continue; }

            for (int c = 1; c < labels.Length; c++)
            {
                _parsedData[c - 1][r - 1] = float.Parse(row[c]);
            }
        }
    }

    private void LoadHourly()
    {
        for (int z = 0; z < _zones.Length; z++)
        {
            string zone = _zones[z];
            Dictionary<string, float[]> roomData = new();

            foreach (string variable in _hourlyStartByVariable.Keys)
            {
                roomData.Add(variable, _parsedData[_hourlyStartByVariable[variable] + z]);
            }

            _hourlyByRoomAndVariable.Add(zone, roomData);
        }
    }

    private void LoadMonthly()
    {
        for (int z = 0; z < _zones.Length; z++)
        {
            string zone = _zones[z];
            Dictionary<string, float[]> roomData = new();

            foreach (string variable in _hourlyStartByVariable.Keys)
            {
                float[] data = _parsedData[_hourlyStartByVariable[variable] + z];
                float[] monthly = new float[12];
                int i = 0;

                for (int m = 0; m < 12; m++)
                {
                    for (int h = 0; h < (m == 0 ? 23 : 24); h++)
                    {
                        monthly[m] += data[i];
                        i++;
                    }
                }

                if (variable == "Air Temperature (C)" || variable == "Air Relative Humidity (%)")
                {
                    for (int m = 0; m < 12; m++)
                    {
                        monthly[m] /= m == 0 ? 23 : 24;
                    }
                }

                roomData.Add(variable, monthly);
            }

            _monthlyByRoomAndVariable.Add(zone, roomData);
        }
    }

    private void LoadAnnual()
    {
        for (int z = 0; z < _zones.Length; z++)
        {
            string zone = _zones[z];
            Dictionary<string, float> roomData = new();

            foreach (string variable in _hourlyStartByVariable.Keys)
            {
                float annual = 0;

                for (int m = 0; m < 12; m++)
                {
                    annual += _monthlyByRoomAndVariable[zone][variable][m];
                }

                if (variable == "Air Temperature (C)" || variable == "Air Relative Humidity (%)")
                {
                    annual /= 12;
                }

                roomData.Add(variable, annual);
            }

            _annualByRoomAndVariable.Add(zone, roomData);
        }
    }

    private void CreateGraphs()
    {
        for (int z = 0; z < _zones.Length; z++)
        {
            string zone = _zones[z];
            DataGraph graph = Instantiate(_graphPrefab, _graphLocations[z]);
            graph.Initialize(zone, _floorMaterials[z]);
            graph.Camera = _uiCamera;
            graph.HourlyDataByVariable = _hourlyByRoomAndVariable[zone];
            graph.MonthlyDataByVariable = _monthlyByRoomAndVariable[zone];
            graph.AnnualDataByVariable = _annualByRoomAndVariable[zone];
            _graphByRoom.Add(zone, graph);
            graph.UpdateBars();
        }
    }
}
