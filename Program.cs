using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReservoirMonitorApp
{
    #region JSON Layout
    public class BasicInfoRaw
    {
        [JsonPropertyName("reservoiridentifier")]
        public string ReservoirIdentifier { get; set; } = string.Empty;

        [JsonPropertyName("reservoirname")]
        public string ReservoirName { get; set; } = string.Empty;

        [JsonPropertyName("capacity")]
        public string Capacity { get; set; } = string.Empty;

        [JsonPropertyName("datetime")]
        public string DateTime { get; set; } = string.Empty;
    }

    public class ObservationRaw
    {
        [JsonPropertyName("reservoiridentifier")]
        public string reservoiridentifier { get; set; } = string.Empty;

        [JsonPropertyName("observationtime")]
        public string observationtime { get; set; } = string.Empty;

        [JsonPropertyName("effectivewaterstoragecapacity")]
        public string effectivewaterstoragecapacity { get; set; } = string.Empty;

        [JsonPropertyName("waterlevel")]
        public string waterlevel { get; set; } = string.Empty;
    }
    #endregion

    #region Data format
    // Dedicated model to ensure clean UI data-binding matching your exact requested columns
    public class ReservoirDisplayModel
    {
        public string ReservoirIdentifier { get; set; } = string.Empty;
        public string ReservoirName { get; set; } = string.Empty;
        public double EffectiveWaterStorageCapacity { get; set; }
        public double Capacity { get; set; }
        public string Time { get; set; } = string.Empty;
    }
    #endregion

    #region Fetch and Filter
    public class TelemetryService
    {
        private const string FolderName = "data";
        private const string BasicInfoFile = "response_1781533209388.json";
        private const string ObservationsFile = "response_1782908121768.json";

        public async Task<List<ReservoirDisplayModel>> GetProcessedReservoirsAsync()
        {
            string dataFolder = Path.Combine(AppContext.BaseDirectory, FolderName);
            string basicPath = Path.Combine(dataFolder, BasicInfoFile);
            string obsPath = Path.Combine(dataFolder, ObservationsFile);

            if (!File.Exists(basicPath) || !File.Exists(obsPath))
            {
                throw new FileNotFoundException("Some files are missing.");
            }

            string basicJsonTask = await File.ReadAllTextAsync(basicPath);
            string obsJsonTask = await File.ReadAllTextAsync(obsPath);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var basicRecords = JsonSerializer.Deserialize<List<BasicInfoRaw>>(basicJsonTask, options) ?? new List<BasicInfoRaw>();
            var obsRecords = JsonSerializer.Deserialize<List<ObservationRaw>>(obsJsonTask, options) ?? new List<ObservationRaw>();

            var basicMap = basicRecords
                .GroupBy(b => b.ReservoirIdentifier)
                .ToDictionary(g => g.Key, g => g.First());

            var latestObservations = obsRecords
                .GroupBy(o => o.reservoiridentifier)
                .Select(g => g.OrderByDescending(o => o.observationtime).First());

            var processedList = new List<ReservoirDisplayModel>();

            foreach (var obs in latestObservations)
            {
                if (basicMap.TryGetValue(obs.reservoiridentifier, out var basic))
                {
                    double.TryParse(basic.Capacity, out double capacityVal);
                    if (capacityVal <= 5000.0) continue; 
                    double.TryParse(obs.effectivewaterstoragecapacity, out double effectiveCapVal);
                    string displayTime = DateTime.TryParse(obs.observationtime, out var parsedDateTime)
                                ? parsedDateTime.ToString("HH:mm") : obs.observationtime;
                    processedList.Add(new ReservoirDisplayModel
                    {
                        ReservoirIdentifier = obs.reservoiridentifier,
                        ReservoirName = basic.ReservoirName,
                        EffectiveWaterStorageCapacity = effectiveCapVal,
                        Capacity = capacityVal,
                        Time = displayTime
                    });
                }
            }
            return processedList.OrderByDescending(r => r.Capacity).ToList();
        }
    }
    #endregion

    #region WinForms
    public class MainForm : Form
    {
        private Panel pnlHeader;
        private Label lblHeaderTitle;
        private Label lblHeaderSubtitle;
        private DataGridView dgvReservoirs;
        private readonly TelemetryService _telemetryService;
        public MainForm()
        {
            _telemetryService = new TelemetryService();
            InitializeComponent();
            ApplyModernTheme();
            ConfigureDataGridColumns();
        }

        private void InitializeComponent()
        {
            this.Text = "ReservoirDesktopApp";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(500, 400);

            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = Color.FromArgb(31, 41, 55),
                Padding = new Padding(15, 12, 15, 12)
            };

            lblHeaderTitle = new Label
            {
                Text = "Reservoir Telemetry Monitor",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            lblHeaderSubtitle = new Label
            {
                Text = "Initializing local processing streams...",
                Dock = DockStyle.Bottom,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeaderSubtitle);
            pnlHeader.Controls.Add(lblHeaderTitle);

            // Data Grid Setup
            dgvReservoirs = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(243, 244, 246),
                EnableHeadersVisualStyles = false
            };

            this.Controls.Add(dgvReservoirs);
            this.Controls.Add(pnlHeader);
        }

        private void ApplyModernTheme()
        {
            dgvReservoirs.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(243, 244, 246),
                ForeColor = Color.FromArgb(55, 65, 81),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(243, 244, 246),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 2, 8, 2)
            };
            dgvReservoirs.ColumnHeadersHeight = 38;
            dgvReservoirs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgvReservoirs.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(31, 41, 55),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                SelectionBackColor = Color.FromArgb(239, 246, 255),
                SelectionForeColor = Color.FromArgb(29, 78, 216),
                Padding = new Padding(8, 0, 8, 0)
            };
            dgvReservoirs.RowTemplate.Height = 32;

            dgvReservoirs.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(249, 250, 251)
            };
        }

        private void ConfigureDataGridColumns()
        {
            dgvReservoirs.AutoGenerateColumns = false;

            dgvReservoirs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ReservoirIdentifier",
                HeaderText = "ID",
                Width = 90,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReservoirs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Time",
                HeaderText = "Time",
                Width = 90,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });


            dgvReservoirs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ReservoirName",
                HeaderText = "Reservoir Name",
                Width = 180,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvReservoirs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "EffectiveWaterStorageCapacity",
                HeaderText = "Effective Capacity (10⁴ m³)",
                Width = 250,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }
            });

            dgvReservoirs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Capacity",
                HeaderText = "Total Capacity (10⁴ m³)",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }
            });
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.UseWaitCursor = true;
            try
            {
                var processedRecords = await _telemetryService.GetProcessedReservoirsAsync();
                lblHeaderSubtitle.Text = $"Displaying heavy reservoir (> 5000 capacity) | Count: {processedRecords.Count}";
                dgvReservoirs.DataSource = processedRecords;
            }
            catch (Exception ex)
            {
                lblHeaderSubtitle.Text = "Data processing is failed.";
                MessageBox.Show($"Processing error:\n{ex.Message}", "System Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.UseWaitCursor = false;
            }
        }
    }
    #endregion

    #region Entry Point
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
    #endregion
}