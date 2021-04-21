using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Interactivity;
using Syncfusion.WinForms.DataGrid.Serialization;

namespace DeathVerificationFW
{

    public partial class DeathManager : SfForm
    {
        public DeathManager()
        {
            InitializeComponent();
        }

        public string SerPath = $"C:\\Users\\{EnvMethods.GetCurrentUser()}\\AppData\\Roaming\\DataGrid";

        private void Form1_Load(object sender, EventArgs e)
        {
            sfDataGrid1.Style.HeaderStyle.BackColor = sfDataGrid2.Style.HeaderStyle.BackColor = ColorTranslator.FromHtml("#2f4388");
            sfDataGrid1.Style.HeaderStyle.TextColor = sfDataGrid2.Style.HeaderStyle.TextColor = Color.White;
            sfButton1.Style.BackColor = sfButton2.Style.BackColor = sfButton3.Style.BackColor =
                sfButton4.Style.BackColor = ColorTranslator.FromHtml("#2f4388");
            sfButton1.Style.ForeColor = sfButton2.Style.ForeColor = sfButton3.Style.ForeColor =
                sfButton4.Style.ForeColor = Color.White;

            textBox1.ReadOnly = true;
            textBox2.ReadOnly = true;
            textBox3.ReadOnly = true;
            textBox4.ReadOnly = true;
            textBox5.ReadOnly = true;

            if (!comboBox1.Items.Contains("Newspaper")) { comboBox1.Items.Add("Newspaper"); }
            if (!comboBox1.Items.Contains("Funeral Home")) { comboBox1.Items.Add("Funeral Home"); }

            sfDataGrid1.SearchController.AllowFiltering = true;
        }


        public void FillDataGrid(SqlCommand cmd, SfDataGrid dgv, bool dg1)
        {
            if (dg1)
            {
                var stream = new FileStream(SerPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                this.sfDataGrid1.Serialize(stream, new SerializationOptions()
                {
                    SerializeFiltering = true,
                    SerializeSorting = true,
                    SerializeColumns = false,
                    SerializeDetailsViewDefinitions = false,
                    SerializeGrouping = false,
                    SerializeCaptionSummaries = false,
                    SerializeGroupSummaries = false,
                    SerializeStyle = false,
                    SerializeStackedHeaders = false,
                    SerializeTableSummaries = false,
                    SerializeUnboundRows = false
                });
            }
            var conn = new SqlConnection(connectionString: DbInteractions.Utils.GetConn());

            cmd.Connection = conn;

            conn.Open();

            var data = new DataTable();
            var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(data);

            dgv.DataSource = data;
            adapter.Dispose();
            conn.Close();

            if (dg1)
            {
                using (var fileStream = new FileStream(SerPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    this.sfDataGrid1.Deserialize(fileStream, new DeserializationOptions()
                    {
                        DeserializeFiltering = true,
                        DeserializeSorting = true,
                        DeserializeColumns = false,
                        DeserializeCaptionSummary = false,
                        DeserializeDetailsViewDefinitions = false,
                        DeserializeGroupSummaries = false,
                        DeserializeGrouping = false,
                        DeserializeStackedHeaders = false,
                        DeserializeStyle = false,
                        DeserializeTableSummaries = false,
                        DeserializeUnboundRows = false
                    });
                }
            }

            if (dg1)
            {
                if (IsFh())
                {
                    using (var conn2 = new SqlConnection(DbInteractions.Utils.GetConn()))
                    {
                        var query = "SELECT COUNT(*) FROM tLegacyDeaths WHERE ignore = 0 AND hasMatch = 1 AND isFH = 1";
                        var cmd2 = new SqlCommand(query, conn2);
                        conn2.Open();
                        var total = cmd2.ExecuteScalar().ToString();
                        label8.Text = $"Total Records: {total}";
                    }
                }
                else
                {
                    using (var conn2 = new SqlConnection(DbInteractions.Utils.GetConn()))
                    {
                        var query = "SELECT COUNT(*) FROM tLegacyDeaths WHERE ignore = 0 AND hasMatch = 1 AND isFH = 0";
                        var cmd2 = new SqlCommand(query, conn2);
                        conn2.Open();
                        var total = cmd2.ExecuteScalar().ToString();
                        label8.Text = $"Total Records: {total}";
                    }
                }
            }
        }

        private void DeadBtn_Click(object sender, EventArgs e)
        {
            DisableButtons();

            if (string.IsNullOrEmpty(DbInteractions.CheckMethods.RecordInUseBy(int.Parse(textBox1.Text), EnvMethods.GetCurrentUser())))
            {
                if (string.IsNullOrEmpty(textBox3.Text))
                {
                    MessageBox.Show("Please Enter a DOD");
                    return;
                }
                var id = textBox1.Text;
                var ssn = textBox2.Text;
                var dod = textBox3.Text + " 00:00:00";
                var newCmd = GetCommand();
                if (id.Length == 0 || ssn.Length == 0)
                {
                    MessageBox.Show("Please select a row", "Warning");
                }
                else
                {
                    var success = false;
                    if (DbInteractions.CheckMethods.IsDead(ssn, ld: false) == false && DbInteractions.CheckMethods.IsDead(ssn, ld: true) == false)
                    {
                        try
                        {
                            DbInteractions.AlterMethods.SetDeathPd(ssn, dod);

                            var legacyQuery = "IF (SELECT DOD FROM tLegacyDeaths WHERE id = 2526679) IS NULL UPDATE tLegacyDeaths SET isDead = 1, ignore = 1, " +
                                               "DOD = @DOD, LastEditedDate = GETDATE() WHERE ID = @ID ELSE UPDATE tLegacyDeaths SET isDead = 1, ignore = 1, LastEditedDate = GETDATE() WHERE ID = @ID";

                            using (var conn = new SqlConnection(DbInteractions.Utils.GetConn()))
                            {
                                using (var cmd = new SqlCommand(cmdText: legacyQuery, conn))
                                {
                                    cmd.CommandType = CommandType.Text;
                                    cmd.Parameters.AddWithValue("@ID", id);
                                    cmd.Parameters.AddWithValue("@DOD", dod);

                                    cmd.Connection.Open();
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            success = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.StackTrace, "Critical Error");
                            MessageBox.Show(ex.Message, "Critical Error");
                        }
                        finally
                        {
                            if (success)
                            {
                                MessageBox.Show($"{ssn} successfully Marked as dead", "Info");
                                FillDataGrid(newCmd, sfDataGrid1, true);
                            }
                        }
                    }
                    else if (DbInteractions.CheckMethods.IsDead(ssn, ld: false) == false && DbInteractions.CheckMethods.IsDead(ssn, ld: true))
                    {
                        MessageBox.Show($"{ssn} is marked as dead in the Legacy Deaths table.\nPlease notify IT.", "Error");
                        FillDataGrid(newCmd, sfDataGrid1, true);
                    }
                    else if (DbInteractions.CheckMethods.IsDead(ssn, ld: false) && DbInteractions.CheckMethods.IsDead(ssn, ld: true) == false)
                    {
                        MessageBox.Show("The viator is already dead.\nUpdating table...", "Info");
                        var query = "UPDATE tLegacyDeaths SET isDead = 1 WHERE SSN = @SSN";

                        using (var conn = new SqlConnection(DbInteractions.Utils.GetConn()))
                        {
                            using (var cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@SSN", ssn);
                                cmd.Connection.Open();
                                cmd.ExecuteNonQuery();
                            }
                        }
                        FillDataGrid(newCmd, sfDataGrid1, true);
                    }
                    else
                    {
                        MessageBox.Show("The viator has already been marked as dead", "Info");
                        FillDataGrid(newCmd, sfDataGrid1, true);
                    }
                }
                SetUse(int.Parse(textBox1.Text), EnvMethods.GetCurrentUser(), true);
            }

            else
            { CheckUseStatus(int.Parse(textBox1.Text)); }

            EnableButtons();
        }

        private void NotDeadBtn_Click(object sender, EventArgs e)
        {
            DisableButtons();
            if (string.IsNullOrEmpty(DbInteractions.CheckMethods.RecordInUseBy(int.Parse(textBox1.Text), EnvMethods.GetCurrentUser())))
            {
                var ignoreReason = new IgnoreReasonDlg();
                var dialogResult = ignoreReason.ShowDialog();
                if (dialogResult == DialogResult.Cancel)
                {
                    EnableButtons();
                    return;
                }

                var ignoreText = ignoreReason.IgnoreText;
                var id = textBox1.Text;
                var ssn = textBox2.Text;
                var user = EnvMethods.GetCurrentUser();

                var ignoreQuery = "UPDATE tLegacyDeaths SET ignore = 1, SSN = NULL, hasMatch = 0, src = NULL, LastEditedByUser = @USR, " +
                                  "ignoreReason = @IGNORE, LastEditedDate = GETDATE() WHERE ID = @ID AND ssn = @SSN";

                using (var conn = new SqlConnection(DbInteractions.Utils.GetConn()))
                {
                    using (var cmd = new SqlCommand(ignoreQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Add("@ID", SqlDbType.Int).Value = id;
                        cmd.Parameters.Add("@SSN", SqlDbType.VarChar).Value = ssn;
                        cmd.Parameters.Add("@USR", SqlDbType.VarChar).Value = user;
                        cmd.Parameters.Add("@IGNORE", SqlDbType.VarChar).Value = ignoreText;

                        cmd.Connection.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"{id} marked as not dead", "Info");
                var endCmd = GetCommand();
                FillDataGrid(endCmd, sfDataGrid1, true);
                SetUse(int.Parse(textBox1.Text), EnvMethods.GetCurrentUser(), true);
            }
            else
            { CheckUseStatus(int.Parse(textBox1.Text)); }
            
            EnableButtons();
        }



        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text))
            { SetUse(int.Parse(textBox1.Text), EnvMethods.GetCurrentUser(), true); }

            Application.Exit();
        }

        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text))
            { SetUse(int.Parse(textBox1.Text), EnvMethods.GetCurrentUser(), true); }

            FillDataGrid(GetCommand(), sfDataGrid1, false);
        }

        private void SearchTxtBx_MouseClick(object sender, MouseEventArgs e)
        {
            SearchTxtBx.Text = "";
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            
            sfDataGrid1.SearchController.Search(SearchTxtBx.Text);
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            DisableButtons();
            if (!string.IsNullOrEmpty(textBox1.Text))
            { SetUse(int.Parse(textBox1.Text), EnvMethods.GetCurrentUser(), true); }
            var cmd = GetCommand();
            // Console.WriteLine(cmd.CommandText);
            FillDataGrid(cmd, sfDataGrid1, true);
            EnableButtons();
        }


        private bool IsFh()
        { 
            var selected = comboBox1.GetItemText(comboBox1.SelectedItem);
            switch (selected)
            {
                case "Newspaper":
                    return false;
                default:
                    return true;
            }
        }

        public SqlCommand GetCommand(string ssn = "")
        {
            var query = IsFh() ? $"SELECT * FROM v_DeathMGR_FH" : $"SELECT * FROM v_DeathMGR_NP";

            var cmd = new SqlCommand(cmdText:query);
            return cmd;
        }

        public void GetObitExtras(int id)
        {
            if (IsFh())
            {
                var obitTextQuery = "SELECT ObitText FROM tLegacyObitText WHERE ID = @VAL";
                var obitCmd = new SqlCommand(cmdText: obitTextQuery);
                obitCmd.Parameters.Add("@VAL", SqlDbType.BigInt).Value = id;
                using (var conn = new SqlConnection(DbInteractions.Utils.GetConn()))
                {
                    obitCmd.Connection = conn;
                    conn.Open();
                    using (var reader = obitCmd.ExecuteReader())
                    {
                        textBox4.Text = reader.Read() ? reader["ObitText"].ToString() : "No Obit Text found.";
                    }
                    conn.Close();
                }
            }
            else
            {
                var obitLinkQuery = "SELECT ObituaryLink FROM tLegacyDeaths WHERE ID = @VAL";
                var obitCmd = new SqlCommand(cmdText: obitLinkQuery);
                obitCmd.Parameters.Add("@VAL", SqlDbType.BigInt).Value = id;
                using (var conn = new SqlConnection(DbInteractions.Utils.GetConn()))
                {
                    obitCmd.Connection = conn;
                    conn.Open();
                    using (var reader = obitCmd.ExecuteReader())
                    {
                        textBox4.Text = reader.Read() ? reader["ObituaryLink"].ToString() : "No Link found";
                    }
                    conn.Close();
                }
            }

            var ageQuery =
                "SELECT ABS(DATEDIFF(YEAR, ISNULL(DOB, '01/01/1850'), ISNULL(DOD, GETDATE()))) AS 'Age' FROM tLegacyDeaths WHERE ID = @VAL";
            using (var conn = new SqlConnection(DbInteractions.Utils.GetConn()))
            {
                using (var cmd = new SqlCommand(cmdText:ageQuery, conn))
                {
                    cmd.Parameters.Add("@VAL", SqlDbType.BigInt).Value = id;
                    conn.Open();
                    var age = (int) cmd.ExecuteScalar();
                    textBox5.Text = age < 169 ? age.ToString() : "0";
                }
                conn.Close();
            }
            
        }

        private void SfButton4_Click(object sender, EventArgs e)
        {
            sfDataGrid1.SearchController.ClearSearch();
        }

        private void SfDataGrid1_CellClick(object sender, Syncfusion.WinForms.DataGrid.Events.CellClickEventArgs e)
        {
            if (e.DataRow.RowIndex >= 1)
            {
                DisableButtons();
                
                var rowData = sfDataGrid1.GetRecordAtRowIndex(e.DataRow.RowIndex);
                var propertyCollection = sfDataGrid1.View.GetPropertyAccessProvider();



                var id = propertyCollection.GetValue(rowData, "ID").ToString();
                var ssn = propertyCollection.GetValue(rowData, "SSN").ToString();
                var dod = propertyCollection.GetValue(rowData, "DOD").ToString();

                var intId = int.Parse(id);

                if (!string.IsNullOrEmpty(textBox1.Text) && textBox1.Text != id)
                {
                    var oldId = int.Parse(textBox1.Text);
                    SetUse(oldId, EnvMethods.GetCurrentUser(), true);
                }

                if (DbInteractions.CheckMethods.RecordInUseBy(intId, EnvMethods.GetCurrentUser()) == string.Empty)
                {
                    SetUse(intId, EnvMethods.GetCurrentUser(), false);
                    textBox1.Text = id;
                    textBox2.Text = ssn;
                    textBox3.Text = dod;

                    GetObitExtras(intId);

                    if (string.IsNullOrEmpty(dod)) { textBox3.ReadOnly = false; }
                    else { textBox3.ReadOnly = true; }

                    var pdQuery =
                        "SELECT AVSRecNo, DateRecd, DOB, PatFName as FName, PatMName as MName, PatLName as LName, ViatorState as 'State', " +
                        "PatientType as Diagnosis, Mortality as '%', PredictionMths AS 'LE', prediction as Comments FROM PersonalData WHERE SSN LIKE @VAL ORDER BY AVSRecNo DESC";
                    var cmd = new SqlCommand(cmdText: pdQuery);
                    cmd.Parameters.Add("@VAL", SqlDbType.VarChar).Value = ssn;
                    FillDataGrid(cmd, sfDataGrid2, false);
                    
                }

                else { CheckUseStatus(intId); }
                EnableButtons();
            }
        }

        private void SfDataGrid1_FilterPopupShowing(object sender, Syncfusion.WinForms.DataGrid.Events.FilterPopupShowingEventArgs e)
        {
            e.Control.CheckListBox.Style.CheckBoxStyle.CheckedBorderColor =
                e.Control.CheckListBox.Style.CheckBoxStyle.CheckedBackColor = ColorTranslator.FromHtml("#2f4388");
            e.Control.CheckListBox.Style.CheckBoxStyle.IndeterminateColor =
                e.Control.CheckListBox.Style.CheckBoxStyle.IndeterminateBorderColor = ColorTranslator.FromHtml("#2f4388");
            e.Control.OkButton.BackColor = e.Control.CancelButton.BackColor = ColorTranslator.FromHtml("#2f4388");

            e.Control.OkButton.ForeColor = e.Control.CancelButton.ForeColor = Color.White;
        }

        private void DisableButtons()
        {
            sfButton2.Enabled = false;
            sfButton3.Enabled = false;
            linkLabel1.Enabled = false;
            linkLabel2.Enabled = false;
        }

        private void EnableButtons()
        {
            sfButton2.Enabled = true;
            sfButton3.Enabled = true;
            linkLabel1.Enabled = true;
            linkLabel2.Enabled = true;
        }

        

        private void CheckUseStatus(int recId)
        {
            var currentUser = EnvMethods.GetCurrentUser();
            var inUseBy = DbInteractions.CheckMethods.RecordInUseBy(recId, currentUser);
            var msg = $"Record in use by {inUseBy}.\n\n Would you like to remove them from the record?";

            if (inUseBy != string.Empty)
            {
                var msgBox1Result = MessageBox.Show(msg, "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (msgBox1Result == DialogResult.Yes)
                {
                    var msgBox2Result = MessageBox.Show("Are you sure?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (msgBox2Result == DialogResult.Yes)
                    {
                        SetUse(recId, currentUser, false);
                    }
                }
            }
        }

        private void SetUse(int recId, string currentUser, bool removeUse)
        {
            using (var conn = new SqlConnection(DbInteractions.Utils.GetConn()))
            {
                var cmdText = "UPDATE tLegacyDeaths SET InUseBy = @currentUser WHERE id = @recID";
                using (var cmd = new SqlCommand(cmdText, conn))
                {
                    conn.Open();
                    cmd.Parameters.Add("@recID", SqlDbType.BigInt).Value = recId;
                    cmd.Parameters.Add("@currentUser", SqlDbType.VarChar).Value = removeUse ? "" : currentUser;
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        private void DeathManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                SetUse(int.Parse(textBox1.Text), EnvMethods.GetCurrentUser(), true);
            }
        }
    }
}
