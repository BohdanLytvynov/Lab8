using DB;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WindowsFormsApp1.Properties;
using WindowsFormsApp1.Utilities;
using dgvcmbx = System.Windows.Forms.DataGridViewComboBoxColumn;

namespace WindowsFormsApp1
{
    

    public partial class LabWork8 : Form
    {
        private event Action<Exception> OnErrorOccured;
        
        private MySqlDataAdapter mySqlDataAdapter;

        public static readonly string connectionString;

        private static List<string> table_List;

        private static List<(string, string)> table_columns;

        private string SelTable;

        private string SelectedColumn;

        private static readonly MySqlConnection globConn;

        private List<BindingSource> m_bindingSources;
        private List<DataGridViewColumn> m_cmboxes;

        public LabWork8()
        {           
            InitializeComponent();

            m_bindingSources = new List<BindingSource>();
            m_cmboxes = new List<DataGridViewColumn>();

            mySqlDataAdapter = new MySqlDataAdapter();

            OnErrorOccured += LabWork8_OnErrorOccured;

            table_List = DBUtilities.GetAllTables("TanksDb", globConn, 
                err => { OnErrorOccured(err); });
                        
            this.tables.Items.AddRange(table_List.ToArray());
            
            this.dataGridViewSQL.AutoGenerateColumns = false;

            dataGridViewSQL.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);            
        }
     
        private void LabWork8_OnErrorOccured(Exception obj)
        {
            MessageBox.Show($"Error happened in DataGrid! Error: {obj.Message} {obj.StackTrace}",
                    "DataGrid Module", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        static LabWork8()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
            builder.Server = "127.0.0.1";
            builder.Port = 3333;
            builder.UserID = "root";
            builder.Password = "root";
            builder.Database = "TanksDb";

            connectionString = builder.ConnectionString;

            globConn = new MySqlConnection(connectionString);

            table_columns = new List<(string, string)>();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.Save();

            globConn.Close();
            globConn.Dispose();
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This Propgramm was created by Bohdan Lytvynov Student of 125 Group!", "LabWork", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void LabWork8_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = MessageBox.Show("Do you want to close the Programm?",
            "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes;            
        }

        private bool Contains_Get(string colName, List<(string, string)> fkdata, ref (string, string) elemFound)
        {            
            elemFound = fkdata.Find(p => p.Item1.Equals(colName));

            if(elemFound == default)
                return false;

            return true;                                            
        }

        public void GetAllData(string selectCommand, BindingSource bindingSource, DataGridView dataGridView,
            params (string, string)[] parameters)
        {
            this.dataGridViewSQL.Columns.Clear();

            MySqlCommand cmd = null;
            try
            {    
                if(globConn.State == ConnectionState.Closed)
                    globConn.Open();

                cmd = new MySqlCommand(selectCommand, globConn);

                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Item1, p.Item2);
                }

                mySqlDataAdapter = new MySqlDataAdapter(cmd);

                MySqlCommandBuilder commandBuilder = new MySqlCommandBuilder(mySqlDataAdapter);
                            
                DataTable table = new DataTable
                {
                    Locale = System.Globalization.CultureInfo.InvariantCulture
                };
                mySqlDataAdapter.Fill(table);
                bindingSource.DataSource = table;

                if (dataGridView != null)
                {
                    dataGridView.DataSource = bindingSource;

                    dataGridView.AutoResizeColumns(
              DataGridViewAutoSizeColumnsMode.AllCellsExceptHeader);
                }

                if(m_bindingSources.Count > 0) 
                    m_bindingSources.Clear();
                if(m_cmboxes.Count > 0)
                    m_cmboxes.Clear();

                var fk = DBUtilities.GetAllFKAndTablesWithFK(SelTable, globConn, err => OnErrorOccured(err));

                (string, string) k = default;

                BindingSource s = null;

                DataGridViewColumn cbx = null;

                foreach (var c in table_columns)
                {
                    if (Contains_Get(c.Item1, fk, ref k))
                    {
                        cbx = new dgvcmbx();
                        s = new BindingSource();
                        
                        GetData($"select * from {k.Item2}", s, null);

                        (cbx as dgvcmbx).DataPropertyName = k.Item1;
                        (cbx as dgvcmbx).DataSource = s;
                        (cbx as dgvcmbx).HeaderText = k.Item2;
                        (cbx as dgvcmbx).ValueMember = k.Item1;
                        (cbx as dgvcmbx).DisplayMember = "Name";                                               
                    }
                    else
                    {
                        if (c.Item2.Equals("varchar"))
                        {
                            cbx = new MaskedTextBoxColumn();
                        }
                        else if (c.Item2.Equals("float") || c.Item2.Equals("int") || c.Item2.Equals("tinyint"))
                        {
                            cbx = new NumericUpDownColumn();
                        }
                        else if (c.Item2.Equals("date"))
                        { 
                            cbx = new DataGridViewCalendarColumn();
                        }
                        
                        cbx.DataPropertyName = c.Item1; 
                        cbx.HeaderText = c.Item1;
                    }

                    m_bindingSources.Add(s);
                    m_cmboxes.Add(cbx);

                    this.dataGridViewSQL.Columns
                            .AddRange(new System.Windows.Forms.DataGridViewColumn[] { cbx });

                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(selectCommand + "\n\n" + ex.Message, "Error happened while Executing SQL script!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cmd.Dispose();                            
            }
        }

        public void GetData(string selectCommand, BindingSource bindingSource, DataGridView dataGridView,
            params (string, string)[] parameters)
        {
            this.dataGridViewSQL.Columns.Clear();

            MySqlCommand cmd = null;
            try
            {
                if (globConn.State == ConnectionState.Closed)
                    globConn.Open();

                cmd = new MySqlCommand(selectCommand, globConn);

                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Item1, p.Item2);
                }

                mySqlDataAdapter = new MySqlDataAdapter(cmd);

                MySqlCommandBuilder commandBuilder = new MySqlCommandBuilder(mySqlDataAdapter);

                DataTable table = new DataTable
                {
                    Locale = System.Globalization.CultureInfo.InvariantCulture
                };
                mySqlDataAdapter.Fill(table);
                bindingSource.DataSource = table;

                if (dataGridView != null)
                {
                    dataGridView.DataSource = bindingSource;

                    dataGridView.AutoResizeColumns(
              DataGridViewAutoSizeColumnsMode.AllCellsExceptHeader);
                }               
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(selectCommand + "\n\n" + ex.Message, "Error happened while Executing SQL script!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cmd.Dispose();
            }
        }

        private void Search()
        {            
            GetData($"select * from {SelTable} where {SelectedColumn} = @value", GridViewBinding, dataGridViewSQL,
                ("@value", this.search_input.Text));
        }
        
        private void ExecSql_Click(object sender, EventArgs e)
        {
            GetData(this.sql_query_input.Text, GridViewBinding, dataGridViewSQL);
        }

        private void ExecSql_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
                GetData(this.sql_query_input.Text, GridViewBinding, dataGridViewSQL);
        }

        private void dataGridViewSQL_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show($"Error happened in DataGrid! Error: {e.Exception.Message}",
                    "DataGrid Module", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void LabWork8_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.GridViewBinding.DataSource = null;
        }
                
        private void tables_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Columns.Items.Clear();
            table_columns.Clear();

            SelTable = (sender as ComboBox).Text;

            table_columns = DBUtilities.GetAllColumnsAndTypes(SelTable, globConn, err => OnErrorOccured(err));
           
            var col_names = table_columns.Select(c => c.Item1);
            
            this.Columns.Items.AddRange(col_names.ToArray());
            GetAllData($"select * from {this.SelTable}", GridViewBinding, dataGridViewSQL);           
        }

        private void Columns_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedColumn = (sender as ComboBox).Text;
        }

        private void Search_Button_Click(object sender, EventArgs e)
        {
            Search();
        }

        private void saveDB_Click(object sender, EventArgs e)
        {
            try
            {
                var r = mySqlDataAdapter.Update((DataTable)this.GridViewBinding.DataSource);

                MessageBox.Show($"DataBase Updated! Rows Modified: {r.ToString()}.");
            }
            catch (Exception ex)
            {
                OnErrorOccured.Invoke(ex);
            }
            
        }
       
        
    }
}
