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
    //data format
    public class ReservoirRecord
    {
        [JsonPropertyName("reservoiridentifier")]
        public string ReservoirIdentifier { get; set; } = string.Empty;
        
        [JsonPropertyName("reservoirname")]
        public string ReservoirName { get; set; } = string.Empty;

        [JsonPropertyName("capacity")]
        public string CapacityRaw { get; set; } = string.Empty;

        [JsonPropertyName("datetime")]
        public string DatewTime { get; set; } = string.Empty;

        [JsonIgnore]
        public double Capacity => double.TryParse(CapacityRaw, out var value) ? value : 0.0;
    }

    //data fetching and filtering
    public class TelemetryService
    {
        private const string FolderName = "data";
        private const string FileName = "response_1781533209388.json";
        public async Task<List<ReservoirRecord>> GetLargeReservoirsAsync()
        {
            //data fetching
            string filePath = Path.Combine(AppContext.BaseDirectory,FolderName, FileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Telemetry configuration file missing.");
            }
            string jsonPayload = await File.ReadAllTextAsync(filePath);
            var records = JsonSerializer.Deserialize<List<ReservoirRecord>>(jsonPayload);
            
            if (records == null || records.Count == 0)
            {
                return new List<ReservoirRecord>();
            }

            //data filtering
            return records
                .Where(r => r.Capacity > 5000.0)
                .OrderByDescending(r => r.Capacity)
                .ToList();
        }
    }

    //UI
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
            //form setup
            this.Text = "Reservoir Telemetry Monitor";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(400, 300);

            //header
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(31, 41, 55),
                Padding = new Padding(15, 12, 15, 12)
            };

            lblHeaderTitle = new Label
            {
                Text = "TAIWAN RESERVOIR TELEMETRY",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            lblHeaderSubtitle = new Label
            {
                Text = "Loading local telemetry...",
                Dock = DockStyle.Bottom,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeaderSubtitle);
            pnlHeader.Controls.Add(lblHeaderTitle);
            
            //data grid
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

            //append grid and header in order
            this.Controls.Add(dgvReservoirs);
            this.Controls.Add(pnlHeader);
        }

        private void ApplyModernTheme()
        {
            //style of the grid header
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

            //style of the grid
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

            //style of the alternating row
            dgvReservoirs.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(249, 250, 251)
            };
            
        }
        //content of the column
        private void ConfigureDataGridColumns()
        {
            dgvReservoirs.AutoGenerateColumns = false;

            dgvReservoirs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ReservoirIdentifier",
                HeaderText = "ID",
                Width = 80,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            dgvReservoirs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ReservoirName",
                HeaderText = "Reservoir Name",
                Width = 200,
                DefaultCellStyle = {Alignment = DataGridViewContentAlignment.MiddleLeft}
            });

            dgvReservoirs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Capacity",
                HeaderText = "Capacity (10⁴ m³)",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DefaultCellStyle = 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N2" 
                }
            });
        }
        //data loading
        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.UseWaitCursor = true;
            try
            {
                var processedRecords = await _telemetryService.GetLargeReservoirsAsync();

                if (!processedRecords.Any())
                {
                    lblHeaderSubtitle.Text = "Telemetry Notice: Empty dataset returned.";
                    return;
                }

                lblHeaderSubtitle.Text = $"Snapshot Time: {processedRecords.First().DatewTime} | Heavy Capacities Filtered ({processedRecords.Count} stations)";
                dgvReservoirs.DataSource = processedRecords;
            }
            catch (Exception ex)
            {
                lblHeaderSubtitle.Text = "Telemetry initialization failed.";
                MessageBox.Show($"An error occurred:\n{ex.Message}","Application Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                this.UseWaitCursor = false;
            }
        }
    }

    //main
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
}