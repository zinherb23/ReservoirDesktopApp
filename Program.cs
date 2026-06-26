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
    // =========================================================================
    // 1. DATA MODELS
    // =========================================================================
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

    // =========================================================================
    // 2. BUSINESS LOGIC / SERVICE LAYER (Separation of Concerns)
    // =========================================================================
    public class TelemetryService
    {
        private const string FileName = "response_1781533209388.json";

        /// <summary>
        /// Fetches, parses, and filters reservoir telemetry data.
        /// </summary>
        public async Task<List<ReservoirRecord>> GetLargeReservoirsAsync()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, FileName);

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

            // Keep business logic processing out of the UI Layer
            return records
                .Where(r => r.Capacity > 10000.0)
                .OrderByDescending(r => r.Capacity)
                .ToList();
        }
    }

    // =========================================================================
    // 3. USER INTERFACE LAYER (Modern Flat Design Style)
    // =========================================================================
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
            // Form Setup
            this.Text = "Reservoir Telemetry Monitor";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 600);

            // 1. Header Panel (Acts as a visual anchor)
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 170,
                BackColor = Color.FromArgb(31, 41, 55), // Dark Slate/Charcoal
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
                Text = "Loading local telemetry matrix...",
                Dock = DockStyle.Bottom,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(255, 163, 175), // Soft gray text
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeaderSubtitle);
            pnlHeader.Controls.Add(lblHeaderTitle);
            

            // 2. Tabular Data Grid
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
                GridColor = Color.FromArgb(243, 244, 246), // Ultra-light border lines
                EnableHeadersVisualStyles = false // Required to apply custom header colors
            };

            // Add UI Controls to Form
            this.Controls.Add(dgvReservoirs);
            this.Controls.Add(pnlHeader);
            
             // Added second so Top dock lays out correctly against Fill
        }

        private void ApplyModernTheme()
        {
            // Style the DataGrid Headers to look modern and flat
            dgvReservoirs.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(243, 244, 246),
                ForeColor = Color.FromArgb(255, 65, 81),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(243, 244, 246),
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Padding = new Padding(8, 10, 8, 0)
            };
            dgvReservoirs.ColumnHeadersHeight = 38;
            dgvReservoirs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Style the DataGrid Rows
            dgvReservoirs.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.Green,
                ForeColor = Color.FromArgb(31, 41, 55),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                SelectionBackColor = Color.FromArgb(239, 246, 255), // Soft modern light-blue highlight
                SelectionForeColor = Color.FromArgb(29, 78, 216),
                Padding = new Padding(8, 0, 8, 0)
            };
            dgvReservoirs.RowTemplate.Height = 32;

            // Alternating row background for optimal readability
            dgvReservoirs.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(49, 250, 51)
            };
        }

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
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            dgvReservoirs.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Capacity",
                HeaderText = "Capacity (10⁴ m³)",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, // Fill remaining space dynamically
                DefaultCellStyle = 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N20" 
                }
            });
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.UseWaitCursor = true;

            try
            {
                // UI layer relies safely on the service layer to pull data
                var processedRecords = await _telemetryService.GetLargeReservoirsAsync();

                if (!processedRecords.Any())
                {
                    lblHeaderSubtitle.Text = "Telemetry Notice: Empty dataset returned.";
                    return;
                }

                // Update UI Information cleanly
                lblHeaderSubtitle.Text = $"Snapshot Time: {processedRecords.First().DatewTime} | Heavy Capacities Filtered ({processedRecords.Count} stations)";
                dgvReservoirs.DataSource = processedRecords;
            }
            catch (Exception ex)
            {
                lblHeaderSubtitle.Text = "System Fault: Telemetry initialization failed.";
                MessageBox.Show($"An error occurred while building the telemetry cache:\n{ex.Message}", 
                    "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                this.UseWaitCursor = false;
            }
        }
    }

    // =========================================================================
    // 4. RUNTIME BOOTSTRAP ENTRY
    // =========================================================================
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