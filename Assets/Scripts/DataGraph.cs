using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DataGraph : MonoBehaviour
{
    private static readonly Dictionary<string, int> _daysPerMonth = new()
    {
        { "January", 31 },
        { "February", 28 },
        { "March", 31 },
        { "April", 30 },
        { "May", 31 },
        { "June", 30 },
        { "July", 31 },
        { "August", 31 },
        { "September", 30 },
        { "October", 31 },
        { "November", 30 },
        { "December", 31 },
    };

    private static readonly Dictionary<string, int> _hourlyStartByMonth = new()
    {
        { "January", 0 },
        { "February", 743 },
        { "March", 1415 },
        { "April", 2159 },
        { "May", 2879 },
        { "June", 3623 },
        { "July", 4343 },
        { "August", 5087 },
        { "September", 5831 },
        { "October", 6551 },
        { "November", 7295 },
        { "December", 8015 },
    };

    [SerializeField] private Canvas _canvas = null;
    [SerializeField] private RectTransform _barParent = null;
    [SerializeField] private TextMeshProUGUI _roomLabel = null;
    [SerializeField] private TMP_Dropdown _periodField = null;
    [SerializeField] private TMP_Dropdown _variableField = null;
    [SerializeField] private TMP_Dropdown _monthField = null;
    [SerializeField] private TMP_Dropdown _dayField = null;
    [SerializeField] private GraphBar _barPrefab = null;

    public Camera Camera { set => _canvas.worldCamera = value; }
    public Dictionary<string, float[]> HourlyDataByVariable { get; set; }

    private GraphBar[] _bars = { };
    private float[] _data = { };
    private float _maxValue = Mathf.NegativeInfinity;
    private Stack<GraphBar> _oldBars = new();
    private string _period = "Hourly";
    private string _variable = "Air Relative Humidity (%)";
    private string _month = "January";
    private int _day = 1;

    public void Initialize(string roomName)
    {
        name = $"{roomName}_Graph";
        _roomLabel.text = roomName;
    }

    public void UpdateBars()
    {
        _period = _periodField.options[_periodField.value].text;
        _variable = _variableField.options[_variableField.value].text;
        _month = _monthField.options[_monthField.value].text;

        if (_dayField.options.Count != _daysPerMonth[_month])
        {
            var options = _dayField.options;
            options.Clear();

            for (int m = 0; m < _daysPerMonth[_month]; m++)
            {
                options.Add(new($"{m + 1}"));
            }

            _dayField.value = Mathf.Clamp(_dayField.value, 0, _daysPerMonth[_month] - 1);
        }

        _day = Mathf.Clamp(int.Parse(_dayField.options[_dayField.value].text), 1, _daysPerMonth[_month]);

        bool isHourly = _period == "Hourly";
        bool isMonthly = _period == "Monthly";
        bool isAnnual = _period == "Annual";
        _monthField.gameObject.SetActive(isHourly);
        _dayField.gameObject.SetActive(isHourly);

        for (int i = 0; i < _bars.Length; i++)
        {
            GraphBar bar = _bars[i];
            _oldBars.Push(bar);
            bar.gameObject.SetActive(false);
        }

        if (isHourly) CreateBars(GetHourlyData(_month, _day, HourlyDataByVariable[_variable]));
    }

    private void CreateBars(float[] data = null)
    {
        if (_period == "Hourly") _data = data ?? GetHourlyData(_month, _day, HourlyDataByVariable[_variable]);
        int barCount = _data.Length;
        _bars = new GraphBar[barCount];
        float barWidth = (_barParent.rect.width - barCount - 1) / barCount;
        float barHeight = _barParent.rect.height;
        _maxValue = Mathf.NegativeInfinity;

        for (int i = 0; i < barCount; i++)
        {
            GraphBar bar = _oldBars.Count > 0 ? _oldBars.Pop() : Instantiate(_barPrefab, _barParent);
            RectTransform barTransform = bar.RectTransform;
            barTransform.anchoredPosition = new Vector2(i * (barWidth + 1), 0);
            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barWidth);
            barTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barHeight);
            bar.gameObject.SetActive(true);
            _bars[i] = bar;

            float value = _data[i];
            if (value > _maxValue) _maxValue = value;
        }

        for (int i = 0; i < barCount; i++)
        {
            UpdateBar(i, _data[i]);
        }
    }

    private float[] GetHourlyData(string month, int day, float[] data)
    {
        int startIndex = _hourlyStartByMonth[month];

        if (month == "January" && day > 1) startIndex += 23 + (day - 2) * 24;
        else startIndex += (day - 1) * 24;

        int endIndex = month == "January" && day == 1 ? startIndex + 23 : startIndex + 24;
        float[] hourlyData = new float[endIndex - startIndex + 1];

        for (int i = startIndex; i <= endIndex; i++)
        {
            hourlyData[i - startIndex] = data[i];
        }

        return hourlyData;
    }

    private void UpdateBar(int index, float value)
    {
        GraphBar bar = _bars[index];
        float maxHeight = _barParent.rect.height;
        bar.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            maxHeight * value / _maxValue);
        if (_period == "Hourly") bar.Label.text = $"{index + 1}";
    }
}
